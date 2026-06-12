using INLO.Core.Pooling;
using UnityEditor;
using UnityEngine;

namespace INLO.Core.Pooling.Editor
{
    public static class PoolDatabaseUtilityMenu
    {
        [MenuItem("Tools/INLO/Pooling/Utilities/Fill Missing PoolKeys From Prefab Names")]
        public static void FillMissingPoolKeys()
        {
            Object[] selectedObjects = Selection.objects;

            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                Debug.Log("[PoolDatabaseUtility] No selected assets.");
                return;
            }

            int databaseCount = 0;
            int changedCount = 0;

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                if (selectedObjects[i] is not PoolDatabase database)
                {
                    continue;
                }

                databaseCount++;

                Undo.RecordObject(database, "Fill Missing PoolKeys");

                bool changed = false;

                foreach (PoolEntry entry in database.Entries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(entry.PoolKeyValue))
                    {
                        continue;
                    }

                    string generatedKey = PoolKeyEditorUtility.CreateKeyFromPrefab(entry.Prefab);

                    if (string.IsNullOrWhiteSpace(generatedKey))
                    {
                        continue;
                    }

                    entry.EditorSetPoolKey(generatedKey);
                    changed = true;
                    changedCount++;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(database);
                }
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"[PoolDatabaseUtility] Fill missing keys completed. Databases: {databaseCount}, Changed Entries: {changedCount}");
        }

        [MenuItem("Tools/INLO/Pooling/Utilities/Fill Missing PoolKeys From Prefab Names", true)]
        public static bool FillMissingPoolKeysEnabled()
        {
            return HasSelectedPoolDatabase();
        }

        [MenuItem("Tools/INLO/Pooling/Utilities/Normalize PoolDatabase Counts")]
        public static void NormalizeCounts()
        {
            Object[] selectedObjects = Selection.objects;

            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                Debug.Log("[PoolDatabaseUtility] No selected assets.");
                return;
            }

            int databaseCount = 0;
            int entryCount = 0;

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                if (selectedObjects[i] is not PoolDatabase database)
                {
                    continue;
                }

                databaseCount++;

                Undo.RecordObject(database, "Normalize PoolDatabase Counts");

                foreach (PoolEntry entry in database.Entries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    entry.EditorNormalizeCounts();
                    entryCount++;
                }

                EditorUtility.SetDirty(database);
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"[PoolDatabaseUtility] Normalize counts completed. Databases: {databaseCount}, Entries: {entryCount}");
        }

        [MenuItem("Tools/INLO/Pooling/Utilities/Normalize PoolDatabase Counts", true)]
        public static bool NormalizeCountsEnabled()
        {
            return HasSelectedPoolDatabase();
        }

        private static bool HasSelectedPoolDatabase()
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
    }
}
