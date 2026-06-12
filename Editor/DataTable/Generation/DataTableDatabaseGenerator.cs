using INLO.Core.DataTable;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

namespace INLO.Core.DataTable.Editor
{
    public static class DataTableDatabaseGenerator
    {
        public const string DefaultDatabasePath =
            "Assets/INLO/DataTable/DataTableDatabase.asset";

        public static DataTableDatabase CreateDatabaseAsset(
            string assetPath = DefaultDatabasePath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                Debug.LogError("Asset path is empty.");
                return null;
            }

            if (!assetPath.StartsWith("Assets/"))
            {
                Debug.LogError(
                    $"DataTableDatabase must be created under Assets. Current path: {assetPath}");

                return null;
            }

            DataTableDatabase existing =
                AssetDatabase.LoadAssetAtPath<DataTableDatabase>(assetPath);

            if (existing != null)
                return existing;

            EnsureAssetFolderExists(assetPath);

            DataTableDatabase database =
                ScriptableObject.CreateInstance<DataTableDatabase>();

            AssetDatabase.CreateAsset(database, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"DataTableDatabase created: {assetPath}");

            return database;
        }

        public static DataTableDatabasePlan PreparePlan(DataTableDatabase database)
        {
            DataTableDatabasePlan plan = new() { Target = database };
            List<DataTableAsset> tables = DataTableAssetSearchService.FindAll();
            Dictionary<Type, DataTableAsset> byType = new();
            HashSet<DataTableAsset> existing = new();

            if (database != null)
            {
                for (int i = 0; i < database.Tables.Count; i++)
                {
                    if (database.Tables[i] != null)
                        existing.Add(database.Tables[i]);
                }
            }

            for (int i = 0; i < tables.Count; i++)
            {
                DataTableAsset table = tables[i];

                if (table == null || table.EditorLastImportStatus != "Success")
                    continue;

                Type tableType = table.GetType();

                if (byType.TryGetValue(tableType, out DataTableAsset duplicate))
                {
                    plan.Conflicts.Add(
                        $"Duplicate table type '{tableType.Name}': {AssetDatabase.GetAssetPath(duplicate)} and {AssetDatabase.GetAssetPath(table)}");
                    continue;
                }

                byType.Add(tableType, table);
                plan.Candidates.Add(new DataTableDatabaseCandidate(table));

                if (existing.Contains(table))
                    plan.Unchanged++;
                else
                    plan.Added++;
            }

            foreach (DataTableAsset current in existing)
            {
                if (!byType.ContainsValue(current))
                    plan.Removed++;
            }

            plan.Recalculate(database);
            return plan;
        }

        public static bool ApplyPlan(
            DataTableDatabasePlan plan,
            out DataTableDatabase database,
            out string statusMessage)
        {
            database = plan?.Target;
            statusMessage = string.Empty;

            if (plan == null || !plan.CanApply)
            {
                statusMessage = "Database plan contains conflicts.";
                return false;
            }

            if (database == null)
                database = CreateDatabaseAsset();

            if (database == null)
            {
                statusMessage = "Failed to create or load DataTableDatabase.";
                return false;
            }

            Undo.RecordObject(database, "Apply DataTable Database Plan");

            List<DataTableAsset> selectedTables = new();
            for (int i = 0; i < plan.Candidates.Count; i++)
            {
                if (plan.Candidates[i].Selected)
                    selectedTables.Add(plan.Candidates[i].Table);
            }

            database.Editor_SetTables(selectedTables);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            statusMessage =
                $"Database plan applied. Added: {plan.Added}, Removed: {plan.Removed}, Unchanged: {plan.Unchanged}";
            return true;
        }

        private static void EnsureAssetFolderExists(string assetPath)
        {
            string folderPath = System.IO.Path.GetDirectoryName(assetPath);

            if (string.IsNullOrWhiteSpace(folderPath))
                return;

            folderPath = folderPath.Replace("\\", "/");

            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');

            if (parts.Length == 0 || parts[0] != "Assets")
                return;

            string currentPath = "Assets";

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = $"{currentPath}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }

                currentPath = nextPath;
            }
        }
    }
}
