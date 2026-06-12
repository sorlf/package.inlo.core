using System.Collections.Generic;
using INLO.Core.Pooling;
using UnityEditor;
using UnityEngine;

namespace INLO.Core.Pooling.Editor
{
    public static class PoolDatabaseGroupValidationMenu
    {
        [MenuItem("Tools/INLO/Pooling/Validation/Validate Selected Pool Database Groups")]
        public static void ValidateSelectedGroups()
        {
            Object[] selectedObjects = Selection.objects;

            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                Debug.Log("[PoolDatabaseGroupValidator] No selected assets.");
                return;
            }

            int groupCount = 0;
            int errorCount = 0;
            int warningCount = 0;

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                if (selectedObjects[i] is not PoolDatabaseGroup group)
                {
                    continue;
                }

                groupCount++;
                ValidateGroup(group, ref errorCount, ref warningCount);
            }

            if (groupCount == 0)
            {
                Debug.Log("[PoolDatabaseGroupValidator] No PoolDatabaseGroup assets selected.");
                return;
            }

            Debug.Log($"[PoolDatabaseGroupValidator] Completed. Groups: {groupCount}, Errors: {errorCount}, Warnings: {warningCount}");
        }

        [MenuItem("Tools/INLO/Pooling/Validation/Validate Selected Pool Database Groups", true)]
        public static bool ValidateSelectedGroupsEnabled()
        {
            Object[] selectedObjects = Selection.objects;

            if (selectedObjects == null)
            {
                return false;
            }

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                if (selectedObjects[i] is PoolDatabaseGroup)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateGroup(PoolDatabaseGroup group, ref int errorCount, ref int warningCount)
        {
            string groupPath = AssetDatabase.GetAssetPath(group);

            if (group.Databases == null || group.Databases.Count == 0)
            {
                warningCount++;
                Debug.LogWarning($"[PoolDatabaseGroupValidator] {groupPath} has no databases.", group);
                return;
            }

            HashSet<PoolDatabase> databaseSet = new();
            Dictionary<string, PoolDatabase> keyOwners = new();

            IReadOnlyList<PoolDatabase> databases = group.Databases;

            for (int i = 0; i < databases.Count; i++)
            {
                PoolDatabase database = databases[i];

                if (database == null)
                {
                    errorCount++;
                    Debug.LogError($"[PoolDatabaseGroupValidator] {groupPath} has missing database reference at index {i}.", group);
                    continue;
                }

                if (!databaseSet.Add(database))
                {
                    warningCount++;
                    Debug.LogWarning($"[PoolDatabaseGroupValidator] {groupPath} contains duplicated database: {database.name}.", group);
                    continue;
                }

                PoolDatabaseValidationResult result = PoolDatabaseValidator.Validate(database);

                for (int m = 0; m < result.Messages.Count; m++)
                {
                    PoolDatabaseValidationMessage message = result.Messages[m];

                    if (message.Severity == PoolDatabaseValidationSeverity.Error)
                    {
                        errorCount++;
                    }
                    else if (message.Severity == PoolDatabaseValidationSeverity.Warning)
                    {
                        warningCount++;
                    }
                }

                foreach (PoolEntry entry in database.Entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.PoolKeyValue))
                    {
                        continue;
                    }

                    if (keyOwners.TryGetValue(entry.PoolKeyValue, out PoolDatabase owner))
                    {
                        errorCount++;
                        Debug.LogError(
                            $"[PoolDatabaseGroupValidator] Duplicate PoolKey '{entry.PoolKeyValue}' across databases. " +
                            $"Owner: {owner.name}, Duplicate: {database.name}.",
                            group);
                        continue;
                    }

                    keyOwners.Add(entry.PoolKeyValue, database);
                }
            }
        }
    }
}
