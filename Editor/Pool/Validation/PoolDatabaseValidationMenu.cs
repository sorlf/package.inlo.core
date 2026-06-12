using INLO.Core.Pooling;
using UnityEditor;
using UnityEngine;

namespace INLO.Core.Pooling.Editor
{
    public static class PoolDatabaseValidationMenu
    {
        [MenuItem("Tools/INLO/Pooling/Validation/Validate Selected Pool Databases")]
        public static void ValidateSelected()
        {
            Object[] selectedObjects = Selection.objects;

            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                Debug.Log("[PoolDatabaseValidator] No selected assets.");
                return;
            }

            int databaseCount = 0;
            int errorCount = 0;
            int warningCount = 0;

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                if (selectedObjects[i] is not PoolDatabase database)
                {
                    continue;
                }

                databaseCount++;

                PoolDatabaseValidationResult result = PoolDatabaseValidator.Validate(database);

                if (result.HasError)
                {
                    errorCount++;
                }

                if (result.HasWarning)
                {
                    warningCount++;
                }

                LogResult(database, result);
            }

            if (databaseCount == 0)
            {
                Debug.Log("[PoolDatabaseValidator] No PoolDatabase assets selected.");
                return;
            }

            Debug.Log($"[PoolDatabaseValidator] Completed. Databases: {databaseCount}, Errors: {errorCount}, Warnings: {warningCount}");
        }

        [MenuItem("Tools/INLO/Pooling/Validation/Validate Selected Pool Databases", true)]
        public static bool ValidateSelectedMenuEnabled()
        {
            Object[] selectedObjects = Selection.objects;

            if (selectedObjects == null)
            {
                return false;
            }

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                if (selectedObjects[i] is PoolDatabase)
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogResult(PoolDatabase database, PoolDatabaseValidationResult result)
        {
            string path = AssetDatabase.GetAssetPath(database);

            for (int i = 0; i < result.Messages.Count; i++)
            {
                PoolDatabaseValidationMessage message = result.Messages[i];
                string prefix = message.EntryIndex >= 0 ? $"Entry {message.EntryIndex}: " : string.Empty;
                string log = $"[PoolDatabaseValidator] {path} / {prefix}{message.Message}";

                switch (message.Severity)
                {
                    case PoolDatabaseValidationSeverity.Error:
                        Debug.LogError(log, database);
                        break;

                    case PoolDatabaseValidationSeverity.Warning:
                        Debug.LogWarning(log, database);
                        break;

                    default:
                        Debug.Log(log, database);
                        break;
                }
            }
        }
    }
}
