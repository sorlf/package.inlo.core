using System.Collections.Generic;
using INLO.Core.Pooling;
using UnityEditor;
using UnityEngine;

namespace INLO.Core.Pooling.Editor
{
    public static class PoolDatabaseProjectValidationMenu
    {
        [MenuItem("Tools/INLO/Pooling/Validation/Validate All Pool Databases In Project")]
        public static void ValidateAllInProject()
        {
            string[] guids = AssetDatabase.FindAssets("t:PoolDatabase");

            if (guids == null || guids.Length == 0)
            {
                Debug.Log("[PoolDatabaseProjectValidator] No PoolDatabase assets found in project.");
                return;
            }

            int databaseCount = 0;
            int databaseWithErrorsCount = 0;
            int databaseWithWarningsCount = 0;
            int totalErrorCount = 0;
            int totalWarningCount = 0;

            List<string> errorPaths = new();
            List<string> warningPaths = new();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                PoolDatabase database = AssetDatabase.LoadAssetAtPath<PoolDatabase>(path);

                if (database == null)
                {
                    continue;
                }

                databaseCount++;

                PoolDatabaseValidationResult result = PoolDatabaseValidator.Validate(database);

                int errorCount = CountSeverity(result, PoolDatabaseValidationSeverity.Error);
                int warningCount = CountSeverity(result, PoolDatabaseValidationSeverity.Warning);

                totalErrorCount += errorCount;
                totalWarningCount += warningCount;

                if (errorCount > 0)
                {
                    databaseWithErrorsCount++;
                    errorPaths.Add(path);
                }

                if (warningCount > 0)
                {
                    databaseWithWarningsCount++;
                    warningPaths.Add(path);
                }

                LogResult(path, database, result);
            }

            string summary =
                $"[PoolDatabaseProjectValidator] Completed. " +
                $"Databases: {databaseCount}, " +
                $"Databases With Errors: {databaseWithErrorsCount}, " +
                $"Databases With Warnings: {databaseWithWarningsCount}, " +
                $"Total Errors: {totalErrorCount}, " +
                $"Total Warnings: {totalWarningCount}";

            if (totalErrorCount > 0)
            {
                Debug.LogError(summary);
            }
            else if (totalWarningCount > 0)
            {
                Debug.LogWarning(summary);
            }
            else
            {
                Debug.Log(summary);
            }

            LogPathList("Error Databases", errorPaths);
            LogPathList("Warning Databases", warningPaths);
        }

        [MenuItem("Tools/INLO/Pooling/Validation/Ping First Invalid Pool Database")]
        public static void PingFirstInvalidDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:PoolDatabase");

            if (guids == null || guids.Length == 0)
            {
                Debug.Log("[PoolDatabaseProjectValidator] No PoolDatabase assets found in project.");
                return;
            }

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                PoolDatabase database = AssetDatabase.LoadAssetAtPath<PoolDatabase>(path);

                if (database == null)
                {
                    continue;
                }

                PoolDatabaseValidationResult result = PoolDatabaseValidator.Validate(database);

                if (!result.HasError && !result.HasWarning)
                {
                    continue;
                }

                Selection.activeObject = database;
                EditorGUIUtility.PingObject(database);

                Debug.Log($"[PoolDatabaseProjectValidator] First invalid PoolDatabase: {path}", database);
                return;
            }

            Debug.Log("[PoolDatabaseProjectValidator] No invalid PoolDatabase found.");
        }

        private static int CountSeverity(PoolDatabaseValidationResult result, PoolDatabaseValidationSeverity severity)
        {
            int count = 0;

            for (int i = 0; i < result.Messages.Count; i++)
            {
                if (result.Messages[i].Severity == severity)
                {
                    count++;
                }
            }

            return count;
        }

        private static void LogResult(string path, PoolDatabase database, PoolDatabaseValidationResult result)
        {
            for (int i = 0; i < result.Messages.Count; i++)
            {
                PoolDatabaseValidationMessage message = result.Messages[i];

                if (message.Severity == PoolDatabaseValidationSeverity.Info)
                {
                    continue;
                }

                string prefix = message.EntryIndex >= 0 ? $"Entry {message.EntryIndex}: " : string.Empty;
                string log = $"[PoolDatabaseProjectValidator] {path} / {prefix}{message.Message}";

                switch (message.Severity)
                {
                    case PoolDatabaseValidationSeverity.Error:
                        Debug.LogError(log, database);
                        break;

                    case PoolDatabaseValidationSeverity.Warning:
                        Debug.LogWarning(log, database);
                        break;
                }
            }
        }

        private static void LogPathList(string title, List<string> paths)
        {
            if (paths == null || paths.Count == 0)
            {
                return;
            }

            Debug.Log($"[PoolDatabaseProjectValidator] {title}: {string.Join(", ", paths)}");
        }
    }
}
