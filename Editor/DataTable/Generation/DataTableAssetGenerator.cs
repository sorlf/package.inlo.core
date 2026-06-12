using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using INLO.Core.DataTable;
using UnityEditor;
using UnityEngine;

namespace INLO.Core.DataTable.Editor
{
    public static class DataTableAssetGenerator
    {
        public static void ApplyRows<TRow>(
            DataTableAsset<TRow> tableAsset,
            List<TRow> rows)
            where TRow : class, IDataTableRow
        {
            if (tableAsset == null)
            {
                Debug.LogError("DataTableAsset is null.");
                return;
            }

            tableAsset.Editor_SetRows(rows);

            EditorUtility.SetDirty(tableAsset);
            AssetDatabase.SaveAssets();

            Debug.Log($"DataTable updated: {tableAsset.name}, Row Count: {rows.Count}");
        }

        public static bool ApplyRowsUntyped(
            DataTableAsset tableAsset,
            IList rows,
            System.Type rowType,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (tableAsset == null)
            {
                errorMessage = "DataTableAsset is null.";
                return false;
            }

            if (rows == null)
            {
                errorMessage = "Rows is null.";
                return false;
            }

            if (rowType == null)
            {
                errorMessage = "Row type is null.";
                return false;
            }

            MethodInfo method = typeof(DataTableAssetGenerator)
                .GetMethod(nameof(ApplyRows), BindingFlags.Public | BindingFlags.Static);

            if (method == null)
            {
                errorMessage = "ApplyRows method not found.";
                return false;
            }

            MethodInfo genericMethod = method.MakeGenericMethod(rowType);

            genericMethod.Invoke(
                null,
                new object[]
                {
                    tableAsset,
                    rows
                });

            return true;
        }

        public static void ApplySourceConfig(
            DataTableAsset tableAsset,
            string sourcePath,
            string sheetName)
        {
            if (tableAsset == null)
                return;

            string storedPath = DataTablePathUtility.ToProjectRelativePath(sourcePath);

            tableAsset.Editor_SetSourcePath(storedPath);
            tableAsset.Editor_SetSheetName(sheetName ?? string.Empty);
            tableAsset.Editor_SetSourceKind(
                GooglePublishedCsvGridReader.IsSupportedUrl(storedPath)
                    ? "GooglePublishedCsv"
                    : "Xlsx");

            EditorUtility.SetDirty(tableAsset);
            AssetDatabase.SaveAssets();
        }

        public static void ApplySourcePath(
            DataTableAsset tableAsset,
            string sourcePath)
        {
            ApplySourceConfig(tableAsset, sourcePath, tableAsset != null ? tableAsset.EditorSheetName : string.Empty);
        }

        public static void ApplyImportResult(
            DataTableAsset tableAsset,
            bool success,
            string message)
        {
            if (tableAsset == null)
                return;

            string status = success ? "Success" : "Failed";
            string utcTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

            tableAsset.Editor_SetLastImportResult(
                utcTime,
                status,
                message ?? string.Empty);

            EditorUtility.SetDirty(tableAsset);
            AssetDatabase.SaveAssets();
        }

        public static bool ApplyImportPlan(
            DataTableImportPlan plan,
            out string statusMessage)
        {
            return ApplyImportPlans(
                new List<DataTableImportPlan> { plan },
                out statusMessage);
        }

        public static bool ApplyImportPlans(
            IReadOnlyList<DataTableImportPlan> plans,
            out string statusMessage)
        {
            statusMessage = string.Empty;

            if (plans == null || plans.Count == 0)
            {
                statusMessage = "At least one prepared import plan is required.";
                return false;
            }

            List<ImportSnapshot> snapshots = new();
            UnityEngine.Object[] targets = new UnityEngine.Object[plans.Count];

            for (int i = 0; i < plans.Count; i++)
            {
                DataTableImportPlan plan = plans[i];

                if (plan == null || !plan.CanApply)
                {
                    statusMessage = $"Plan at index {i} is not ready to apply.";
                    return false;
                }

                targets[i] = plan.Target;
                snapshots.Add(ImportSnapshot.Capture(plan.Target));
            }

            Undo.RecordObjects(targets, "Apply DataTable Import Plans");

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < plans.Count; i++)
                    ApplyWithoutSaving(plans[i]);
            }
            catch (Exception exception)
            {
                for (int i = 0; i < snapshots.Count; i++)
                    snapshots[i].Restore();

                statusMessage = $"Apply failed and all targets were restored: {exception.Message}";
                return false;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            statusMessage = $"Applied {plans.Count} prepared DataTable plan(s).";
            return true;
        }

        private static void ApplyWithoutSaving(DataTableImportPlan plan)
        {
            MethodInfo setRowsMethod = plan.Target
                .GetType()
                .GetMethod("Editor_SetRows", BindingFlags.Public | BindingFlags.Instance);

            if (setRowsMethod == null)
                throw new MissingMethodException("Target DataTable asset does not expose Editor_SetRows.");

            setRowsMethod.Invoke(plan.Target, new object[] { plan.Rows });
            plan.Target.Editor_SetSourcePath(plan.Source);
            plan.Target.Editor_SetSheetName(plan.Sheet);
            plan.Target.Editor_SetSourceKind(
                GooglePublishedCsvGridReader.IsSupportedUrl(plan.Source)
                    ? "GooglePublishedCsv"
                    : "Xlsx");

            string message =
                $"Applied prepared import. Rows: {plan.Rows.Count}, " +
                $"Added: {plan.Diff.Added}, Changed: {plan.Diff.Changed}, " +
                $"Removed: {plan.Diff.Removed}, Unchanged: {plan.Diff.Unchanged}";

            plan.Target.Editor_SetLastImportResult(
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"),
                "Success",
                message);

            EditorUtility.SetDirty(plan.Target);
        }

        private sealed class ImportSnapshot
        {
            private readonly DataTableAsset target;
            private readonly IList rows;
            private readonly string source;
            private readonly string sheet;
            private readonly string sourceKind;
            private readonly string importUtc;
            private readonly string importStatus;
            private readonly string importMessage;

            private ImportSnapshot(
                DataTableAsset target,
                IList rows,
                string source,
                string sheet,
                string sourceKind,
                string importUtc,
                string importStatus,
                string importMessage)
            {
                this.target = target;
                this.rows = rows;
                this.source = source;
                this.sheet = sheet;
                this.sourceKind = sourceKind;
                this.importUtc = importUtc;
                this.importStatus = importStatus;
                this.importMessage = importMessage;
            }

            public static ImportSnapshot Capture(DataTableAsset target)
            {
                Type rowType = null;
                DataTableRowTypeUtility.TryGetRowType(target, out rowType);
                Type listType = typeof(List<>).MakeGenericType(rowType);
                IList copy = (IList)Activator.CreateInstance(listType);

                var rowsProperty = target.GetType().GetProperty("Rows");
                if (rowsProperty?.GetValue(target) is IEnumerable currentRows)
                {
                    foreach (object row in currentRows)
                        copy.Add(row);
                }

                return new ImportSnapshot(
                    target,
                    copy,
                    target.EditorSourcePath,
                    target.EditorSheetName,
                    target.EditorSourceKind,
                    target.EditorLastImportUtc,
                    target.EditorLastImportStatus,
                    target.EditorLastImportMessage);
            }

            public void Restore()
            {
                MethodInfo setRowsMethod = target
                    .GetType()
                    .GetMethod("Editor_SetRows", BindingFlags.Public | BindingFlags.Instance);

                setRowsMethod?.Invoke(target, new object[] { rows });
                target.Editor_SetSourcePath(source);
                target.Editor_SetSheetName(sheet);
                target.Editor_SetSourceKind(sourceKind);
                target.Editor_SetLastImportResult(importUtc, importStatus, importMessage);
                EditorUtility.SetDirty(target);
            }
        }
    }
}
