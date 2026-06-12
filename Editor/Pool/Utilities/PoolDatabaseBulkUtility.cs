using System.Collections.Generic;
using INLO.Core.Pooling;
using UnityEditor;
using UnityEngine;

namespace INLO.Core.Pooling.Editor
{
    public static class PoolDatabaseBulkUtility
    {
        private const int DefaultMaxCount = 100;
        private const int DefaultPreloadCount = 0;

        [MenuItem("Tools/INLO/Pooling/Utilities/Create PoolDatabase From Selected Prefabs")]
        public static void CreateDatabaseFromSelectedPrefabs()
        {
            List<GameObject> prefabs = GetSelectedPrefabAssets();

            if (prefabs.Count == 0)
            {
                Debug.LogWarning("[PoolDatabaseBulkUtility] No prefab assets selected.");
                return;
            }

            string path = GetCreateAssetPath("PoolDatabase_FromSelection.asset");

            PoolDatabase database = ScriptableObject.CreateInstance<PoolDatabase>();
            AssetDatabase.CreateAsset(database, path);

            SerializedObject serializedDatabase = new(database);
            SerializedProperty entries = serializedDatabase.FindProperty("entries");

            HashSet<string> usedKeys = new();

            for (int i = 0; i < prefabs.Count; i++)
            {
                AddEntry(entries, prefabs[i], usedKeys);
            }

            serializedDatabase.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = database;

            Debug.Log($"[PoolDatabaseBulkUtility] Created PoolDatabase: {path} / Entries: {prefabs.Count}");
        }

        [MenuItem("Tools/INLO/Pooling/Utilities/Create PoolDatabase From Selected Prefabs", true)]
        public static bool CreateDatabaseFromSelectedPrefabsEnabled()
        {
            return GetSelectedPrefabAssets().Count > 0;
        }

        [MenuItem("Tools/INLO/Pooling/Utilities/Add Selected Prefabs To Selected PoolDatabase")]
        public static void AddSelectedPrefabsToSelectedDatabase()
        {
            PoolDatabase database = GetSelectedPoolDatabase();
            List<GameObject> prefabs = GetSelectedPrefabAssets();

            if (database == null)
            {
                Debug.LogWarning("[PoolDatabaseBulkUtility] Select one PoolDatabase asset.");
                return;
            }

            if (prefabs.Count == 0)
            {
                Debug.LogWarning("[PoolDatabaseBulkUtility] Select one or more prefab assets.");
                return;
            }

            Undo.RecordObject(database, "Add Selected Prefabs To PoolDatabase");

            SerializedObject serializedDatabase = new(database);
            SerializedProperty entries = serializedDatabase.FindProperty("entries");

            HashSet<string> usedKeys = CollectExistingKeys(database);
            HashSet<GameObject> usedPrefabs = CollectExistingPrefabs(database);

            int addedCount = 0;
            int skippedCount = 0;

            for (int i = 0; i < prefabs.Count; i++)
            {
                GameObject prefab = prefabs[i];

                if (prefab == null || usedPrefabs.Contains(prefab))
                {
                    skippedCount++;
                    continue;
                }

                AddEntry(entries, prefab, usedKeys);
                usedPrefabs.Add(prefab);
                addedCount++;
            }

            serializedDatabase.ApplyModifiedProperties();

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            Debug.Log($"[PoolDatabaseBulkUtility] Added prefabs to PoolDatabase. Added: {addedCount}, Skipped: {skippedCount}");
        }

        [MenuItem("Tools/INLO/Pooling/Utilities/Add Selected Prefabs To Selected PoolDatabase", true)]
        public static bool AddSelectedPrefabsToSelectedDatabaseEnabled()
        {
            return GetSelectedPoolDatabase() != null && GetSelectedPrefabAssets().Count > 0;
        }

        private static void AddEntry(SerializedProperty entries, GameObject prefab, HashSet<string> usedKeys)
        {
            int index = entries.arraySize;
            entries.InsertArrayElementAtIndex(index);

            SerializedProperty entry = entries.GetArrayElementAtIndex(index);

            SerializedProperty poolKeyProperty = entry.FindPropertyRelative("poolKey");
            SerializedProperty prefabProperty = entry.FindPropertyRelative("prefab");
            SerializedProperty preloadCountProperty = entry.FindPropertyRelative("preloadCount");
            SerializedProperty maxCountProperty = entry.FindPropertyRelative("maxCount");
            SerializedProperty overflowPolicyProperty = entry.FindPropertyRelative("overflowPolicy");

            string key = CreateUniqueKey(prefab, usedKeys);

            poolKeyProperty.stringValue = key;
            prefabProperty.objectReferenceValue = prefab;
            preloadCountProperty.intValue = DefaultPreloadCount;
            maxCountProperty.intValue = DefaultMaxCount;
            overflowPolicyProperty.enumValueIndex = (int)PoolOverflowPolicy.Expand;
        }

        private static string CreateUniqueKey(GameObject prefab, HashSet<string> usedKeys)
        {
            string baseKey = PoolKeyEditorUtility.CreateKeyFromPrefab(prefab);

            if (string.IsNullOrWhiteSpace(baseKey))
            {
                baseKey = "PoolKey";
            }

            string key = baseKey;
            int suffix = 1;

            while (usedKeys.Contains(key))
            {
                key = $"{baseKey}_{suffix}";
                suffix++;
            }

            usedKeys.Add(key);
            return key;
        }

        private static HashSet<string> CollectExistingKeys(PoolDatabase database)
        {
            HashSet<string> keys = new();

            foreach (PoolEntry entry in database.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.PoolKeyValue))
                {
                    continue;
                }

                keys.Add(entry.PoolKeyValue);
            }

            return keys;
        }

        private static HashSet<GameObject> CollectExistingPrefabs(PoolDatabase database)
        {
            HashSet<GameObject> prefabs = new();

            foreach (PoolEntry entry in database.Entries)
            {
                if (entry == null || entry.Prefab == null)
                {
                    continue;
                }

                prefabs.Add(entry.Prefab);
            }

            return prefabs;
        }

        private static PoolDatabase GetSelectedPoolDatabase()
        {
            Object[] selectedObjects = Selection.objects;

            if (selectedObjects == null)
            {
                return null;
            }

            PoolDatabase found = null;

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                if (selectedObjects[i] is not PoolDatabase database)
                {
                    continue;
                }

                if (found != null)
                {
                    return null;
                }

                found = database;
            }

            return found;
        }

        private static List<GameObject> GetSelectedPrefabAssets()
        {
            List<GameObject> prefabs = new();
            Object[] selectedObjects = Selection.objects;

            if (selectedObjects == null)
            {
                return prefabs;
            }

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                if (selectedObjects[i] is not GameObject gameObject)
                {
                    continue;
                }

                string path = AssetDatabase.GetAssetPath(gameObject);

                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (PrefabUtility.GetPrefabAssetType(gameObject) == PrefabAssetType.NotAPrefab)
                {
                    continue;
                }

                prefabs.Add(gameObject);
            }

            return prefabs;
        }

        private static string GetCreateAssetPath(string fileName)
        {
            string selectedFolder = GetSelectedFolderPath();

            if (string.IsNullOrEmpty(selectedFolder))
            {
                selectedFolder = "Assets";
            }

            string path = $"{selectedFolder}/{fileName}";
            return AssetDatabase.GenerateUniqueAssetPath(path);
        }

        private static string GetSelectedFolderPath()
        {
            Object activeObject = Selection.activeObject;

            if (activeObject == null)
            {
                return "Assets";
            }

            string path = AssetDatabase.GetAssetPath(activeObject);

            if (string.IsNullOrEmpty(path))
            {
                return "Assets";
            }

            if (AssetDatabase.IsValidFolder(path))
            {
                return path;
            }

            int lastSlashIndex = path.LastIndexOf('/');

            if (lastSlashIndex <= 0)
            {
                return "Assets";
            }

            return path.Substring(0, lastSlashIndex);
        }
    }
}
