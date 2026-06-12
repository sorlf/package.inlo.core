using INLO.Core.DataTable;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace INLO.Core.DataTable.Editor
{
    [InitializeOnLoad]
    public static class DataTableCreationWorkflow
    {
        public const string DefaultScriptOutputFolder = "Assets/INLO/DataTable/Scripts";
        public const string DefaultScriptableObjectOutputFolder = "Assets/INLO/DataTable/ScriptableObjects";
        public const string DefaultNamespace = "Game.DataTable";

        private const string PendingKey = "INLO.DataTable.PendingCreation";
        private const string ResultKey = "INLO.DataTable.LastCreationResult";

        static DataTableCreationWorkflow()
        {
            if (HasPendingRequest)
                EditorApplication.delayCall += ResumePending;
        }

        public static bool HasPendingRequest =>
            !string.IsNullOrWhiteSpace(SessionState.GetString(PendingKey, string.Empty));

        public static string LastResult =>
            SessionState.GetString(ResultKey, string.Empty);

        public static DataTableCreationWorkflowPlan Prepare(
            string tableName,
            string namespaceName,
            string scriptOutputFolder,
            string scriptableObjectOutputFolder,
            string source,
            string sheet,
            DataTableGrid grid)
        {
            DataTableCodeGenerationPlan codePlan = DataTableCodeGenerator.Prepare(
                tableName,
                namespaceName,
                scriptOutputFolder,
                grid);

            DataTableCreationWorkflowPlan plan = new()
            {
                CodePlan = codePlan,
                TableTypeName = $"{namespaceName?.Trim()}.{tableName?.Trim()}Table",
                AssetPath = $"{NormalizeFolder(scriptableObjectOutputFolder)}/{tableName?.Trim()}Table.asset",
                Source = DataTablePathUtility.ToProjectRelativePath(source),
                Sheet = sheet ?? string.Empty
            };

            if (codePlan.Errors.Count > 0)
                plan.Errors.AddRange(codePlan.Errors);

            if (string.IsNullOrWhiteSpace(plan.Source))
                plan.Errors.Add("Source path or URL is empty.");

            ValidateScriptableObjectOutputFolder(scriptableObjectOutputFolder, plan);

            if (FindTableType(plan.TableTypeName) != null)
                plan.Errors.Add($"A Table type already exists and will not be replaced: {plan.TableTypeName}");

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(plan.AssetPath) != null)
                plan.Errors.Add($"Existing Table asset will not be overwritten: {plan.AssetPath}");

            return plan;
        }

        private static void ValidateScriptableObjectOutputFolder(
            string outputFolder,
            DataTableCreationWorkflowPlan plan)
        {
            string normalized = NormalizeFolder(outputFolder);
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal) &&
                normalized != "Assets")
            {
                plan.Errors.Add(
                    "ScriptableObject output folder must be an existing repository-relative Assets/... folder.");
            }
            else if (!AssetDatabase.IsValidFolder(normalized))
            {
                plan.Errors.Add($"ScriptableObject output folder does not exist: {normalized}");
            }
        }

        public static bool Start(
            DataTableCreationWorkflowPlan plan,
            out string statusMessage)
        {
            statusMessage = string.Empty;

            if (plan == null || !plan.CanApply)
            {
                statusMessage = plan == null
                    ? "A valid DataTable creation plan is required."
                    : string.Join("\n", plan.Errors);
                return false;
            }

            if (HasPendingRequest)
            {
                statusMessage = "Another DataTable creation is already waiting for Unity compilation.";
                return false;
            }

            PendingCreation request = new()
            {
                TableTypeName = plan.TableTypeName,
                AssetPath = plan.AssetPath,
                Source = plan.Source,
                Sheet = plan.Sheet
            };

            SessionState.SetString(PendingKey, JsonUtility.ToJson(request));
            SessionState.SetString(ResultKey, "C# files created. Waiting for Unity compilation...");

            if (!DataTableCodeGenerator.Apply(plan.CodePlan, out statusMessage))
            {
                ClearPending();
                SessionState.SetString(ResultKey, statusMessage);
                return false;
            }

            statusMessage = "C# files created. Unity will resume asset creation and import after compilation.";
            return true;
        }

        private static async void ResumePending()
        {
            string json = SessionState.GetString(PendingKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
                return;

            try
            {
                PendingCreation request = JsonUtility.FromJson<PendingCreation>(json);
                await Complete(request);
            }
            catch (Exception exception)
            {
                ReportFailure($"DataTable automatic creation failed: {exception.Message}");
            }
            finally
            {
                ClearPending();
            }
        }

        private static async Task Complete(PendingCreation request)
        {
            Type tableType = FindTableType(request.TableTypeName);
            if (tableType == null)
            {
                throw new InvalidOperationException(
                    $"Generated Table type was not found after compilation: {request.TableTypeName}");
            }

            if (!typeof(DataTableAsset).IsAssignableFrom(tableType))
            {
                throw new InvalidOperationException(
                    $"Generated type is not a DataTableAsset: {request.TableTypeName}");
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(request.AssetPath) != null)
            {
                throw new InvalidOperationException(
                    $"Table asset already exists and will not be overwritten: {request.AssetPath}");
            }

            DataTableAsset table = ScriptableObject.CreateInstance(tableType) as DataTableAsset;
            if (table == null)
                throw new InvalidOperationException($"Could not create Table asset: {request.TableTypeName}");

            AssetDatabase.CreateAsset(table, request.AssetPath);
            AssetDatabase.SaveAssets();

            DataTableImportPlan importPlan = await PrepareImport(request, table);
            if (!importPlan.CanApply)
            {
                throw new InvalidOperationException(
                    $"Generated Table asset, but import validation failed:\n{FormatErrors(importPlan)}");
            }

            if (!DataTableImportPlanService.Apply(importPlan, out string importStatus))
                throw new InvalidOperationException(importStatus);

            DataTableDatabase current = DataTableDatabaseSearchService.FindDefault();
            DataTableDatabasePlan databasePlan = DataTableDatabaseGenerator.PreparePlan(current);
            if (!databasePlan.CanApply)
            {
                throw new InvalidOperationException(
                    $"Data imported, but Database registration has conflicts:\n{string.Join("\n", databasePlan.Conflicts)}");
            }

            if (!DataTableDatabaseGenerator.ApplyPlan(databasePlan, out _, out string databaseStatus))
                throw new InvalidOperationException(databaseStatus);

            string result =
                $"DataTable automatic creation completed: {request.AssetPath}\n{importStatus}\n{databaseStatus}";
            SessionState.SetString(ResultKey, result);
            Debug.Log(result);
        }

        private static async Task<DataTableImportPlan> PrepareImport(
            PendingCreation request,
            DataTableAsset table)
        {
            if (!GooglePublishedCsvGridReader.IsSupportedUrl(request.Source))
            {
                return DataTableImportPlanService.PrepareXlsx(
                    request.Source,
                    request.Sheet,
                    table);
            }

            string csv = await GooglePublishedCsvRequest.DownloadAsync(
                request.Source,
                CancellationToken.None);
            return DataTableImportPlanService.PrepareGrid(
                request.Source,
                string.Empty,
                table,
                GooglePublishedCsvGridReader.Parse(csv));
        }

        private static Type FindTableType(string fullName)
        {
            foreach (Type type in TypeCache.GetTypesDerivedFrom<DataTableAsset>())
            {
                if (string.Equals(type.FullName, fullName, StringComparison.Ordinal))
                    return type;
            }

            return null;
        }

        private static string FormatErrors(DataTableImportPlan plan)
        {
            if (plan?.Errors == null || plan.Errors.Count == 0)
                return "Unknown import error.";

            System.Text.StringBuilder builder = new();
            for (int i = 0; i < plan.Errors.Count; i++)
            {
                if (i > 0)
                    builder.Append('\n');

                builder.Append(plan.Errors[i].Message);
            }

            return builder.ToString();
        }

        private static void ReportFailure(string message)
        {
            SessionState.SetString(ResultKey, message);
            Debug.LogError(message);
        }

        private static void ClearPending()
        {
            SessionState.EraseString(PendingKey);
        }

        private static string NormalizeFolder(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace("\\", "/").TrimEnd('/');
        }

        [Serializable]
        private sealed class PendingCreation
        {
            public string TableTypeName;
            public string AssetPath;
            public string Source;
            public string Sheet;
        }
    }

    public sealed class DataTableCreationWorkflowPlan
    {
        public readonly System.Collections.Generic.List<string> Errors = new();

        public DataTableCodeGenerationPlan CodePlan { get; set; }
        public string TableTypeName { get; set; }
        public string AssetPath { get; set; }
        public string Source { get; set; }
        public string Sheet { get; set; }

        public bool CanApply => CodePlan != null && CodePlan.CanApply && Errors.Count == 0;
    }
}
