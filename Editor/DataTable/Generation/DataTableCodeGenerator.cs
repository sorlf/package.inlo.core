using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;

namespace INLO.Core.DataTable.Editor
{
    public static class DataTableCodeGenerator
    {
        public static DataTableCodeGenerationPlan Prepare(
            string baseName,
            string namespaceName,
            string outputFolder,
            DataTableGrid grid)
        {
            DataTableCodeGenerationPlan plan = new()
            {
                BaseName = baseName?.Trim() ?? string.Empty,
                Namespace = namespaceName?.Trim() ?? string.Empty,
                OutputFolder = NormalizePath(outputFolder)
            };

            ValidatePlanInputs(plan, grid);

            if (plan.Errors.Count > 0)
                return plan;

            string rowClassName = plan.BaseName + "Row";
            string tableClassName = plan.BaseName + "Table";

            plan.RowPath = $"{plan.OutputFolder}/{rowClassName}.cs";
            plan.TablePath = $"{plan.OutputFolder}/{tableClassName}.cs";
            plan.AssemblyName = FindOwningAssembly(plan.OutputFolder);

            if (plan.AssemblyName == "Ambiguous asmdef ownership")
                plan.Errors.Add("Output folder contains multiple asmdef files and has ambiguous ownership.");

            if (File.Exists(plan.RowPath) || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(plan.RowPath) != null)
                plan.Errors.Add($"Existing Row script will not be overwritten: {plan.RowPath}");

            if (File.Exists(plan.TablePath) || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(plan.TablePath) != null)
                plan.Errors.Add($"Existing Table script will not be overwritten: {plan.TablePath}");

            if (plan.Errors.Count > 0)
                return plan;

            BuildCode(plan, grid, rowClassName, tableClassName);
            return plan;
        }

        public static bool Apply(
            DataTableCodeGenerationPlan plan,
            out string statusMessage)
        {
            statusMessage = string.Empty;

            if (plan == null || !plan.CanApply)
            {
                statusMessage = "A valid C# Schema generation plan is required.";
                return false;
            }

            if (!AssetDatabase.IsValidFolder(plan.OutputFolder))
            {
                statusMessage = $"Output folder no longer exists: {plan.OutputFolder}";
                return false;
            }

            if (File.Exists(plan.RowPath) || File.Exists(plan.TablePath))
            {
                statusMessage = "Generation cancelled because an output file already exists.";
                return false;
            }

            try
            {
                UTF8Encoding encoding = new(false);
                File.WriteAllText(plan.RowPath, plan.RowCode, encoding);
                File.WriteAllText(plan.TablePath, plan.TableCode, encoding);
                AssetDatabase.Refresh();

                statusMessage =
                    $"Created new C# Schema files in {plan.AssemblyName}: {plan.RowPath}, {plan.TablePath}";
                return true;
            }
            catch (Exception exception)
            {
                statusMessage = $"C# Schema generation failed: {exception.Message}";
                return false;
            }
        }

        private static void ValidatePlanInputs(
            DataTableCodeGenerationPlan plan,
            DataTableGrid grid)
        {
            if (!IsValidIdentifier(plan.BaseName))
                plan.Errors.Add("Base name must be a valid C# identifier.");

            if (!IsValidNamespace(plan.Namespace))
                plan.Errors.Add("Namespace must contain valid C# identifier segments.");

            if (!plan.OutputFolder.StartsWith("Assets/", StringComparison.Ordinal) &&
                plan.OutputFolder != "Assets")
            {
                plan.Errors.Add("Output folder must be an existing repository-relative Assets/... folder.");
            }
            else if (!AssetDatabase.IsValidFolder(plan.OutputFolder))
            {
                plan.Errors.Add($"Output folder does not exist: {plan.OutputFolder}");
            }

            if (grid == null || grid.HeaderCount == 0)
                plan.Errors.Add("Prepared source grid is required.");
        }

        private static void BuildCode(
            DataTableCodeGenerationPlan plan,
            DataTableGrid grid,
            string rowClassName,
            string tableClassName)
        {
            StringBuilder fields = new();
            string idField = null;

            for (int columnIndex = 0; columnIndex < grid.Headers.Count; columnIndex++)
            {
                string header = grid.Headers[columnIndex]?.Trim();

                if (string.IsNullOrWhiteSpace(header) ||
                    DataTableImportRules.IsIgnoredColumn(header))
                {
                    continue;
                }

                if (!IsValidIdentifier(header))
                {
                    plan.Errors.Add($"Header is not a valid C# field identifier: {header}");
                    continue;
                }

                string fieldType = string.Equals(header, "id", StringComparison.OrdinalIgnoreCase)
                    ? "string"
                    : InferColumnType(grid, columnIndex);

                fields.Append("        public ")
                    .Append(fieldType)
                    .Append(' ')
                    .Append(header)
                    .Append(";\n");

                if (string.Equals(header, "id", StringComparison.OrdinalIgnoreCase))
                    idField = header;
            }

            if (idField == null)
            {
                plan.Errors.Add("Source grid must contain an id column.");
                return;
            }

            plan.RowCode = NormalizeLf(
$@"using INLO.Core.DataTable;
using System;

namespace {plan.Namespace}
{{
    [Serializable]
    public sealed class {rowClassName} : IDataTableRow
    {{
{fields}
        public string Id => {idField};
    }}
}}
");

            plan.TableCode = NormalizeLf(
$@"using INLO.Core.DataTable;
using UnityEngine;

namespace {plan.Namespace}
{{
    [CreateAssetMenu(
        fileName = ""{tableClassName}"",
        menuName = ""Game/DataTable/{plan.BaseName} Table"")]
    public sealed class {tableClassName} : DataTableAsset<{rowClassName}>
    {{
    }}
}}
");
        }

        private static string InferColumnType(DataTableGrid grid, int columnIndex)
        {
            bool sawValue = false;
            bool allBool = true;
            bool allInt = true;
            bool allLong = true;
            bool allDouble = true;

            for (int rowIndex = 0; rowIndex < grid.Rows.Count; rowIndex++)
            {
                string value = grid.Rows[rowIndex].GetCell(columnIndex)?.Trim();

                if (string.IsNullOrEmpty(value))
                    continue;

                sawValue = true;
                allBool &= IsBool(value);
                allInt &= int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
                allLong &= long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
                allDouble &= double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
            }

            if (!sawValue)
                return "string";
            if (allBool)
                return "bool";
            if (allInt)
                return "int";
            if (allLong)
                return "long";
            if (allDouble)
                return "double";
            return "string";
        }

        private static bool IsBool(string value)
        {
            return bool.TryParse(value, out _) ||
                   value == "0" ||
                   value == "1" ||
                   value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("n", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("on", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("off", StringComparison.OrdinalIgnoreCase);
        }

        private static string FindOwningAssembly(string folder)
        {
            DirectoryInfo current = new(Path.GetFullPath(folder));
            DirectoryInfo assets = new(Path.GetFullPath("Assets"));

            while (current != null && current.FullName.StartsWith(assets.FullName, StringComparison.OrdinalIgnoreCase))
            {
                string[] asmdefs = Directory.GetFiles(current.FullName, "*.asmdef", SearchOption.TopDirectoryOnly);

                if (asmdefs.Length > 1)
                    return "Ambiguous asmdef ownership";

                if (asmdefs.Length == 1)
                    return Path.GetFileNameWithoutExtension(asmdefs[0]);

                current = current.Parent;
            }

            return "Assembly-CSharp";
        }

        private static bool IsValidNamespace(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string[] segments = value.Split('.');
            for (int i = 0; i < segments.Length; i++)
            {
                if (!IsValidIdentifier(segments[i]))
                    return false;
            }

            return true;
        }

        private static bool IsValidIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                (!char.IsLetter(value[0]) && value[0] != '_'))
            {
                return false;
            }

            for (int i = 1; i < value.Length; i++)
            {
                if (!char.IsLetterOrDigit(value[i]) && value[i] != '_')
                    return false;
            }

            return true;
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace("\\", "/").TrimEnd('/');
        }

        private static string NormalizeLf(string value)
        {
            return value.Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}
