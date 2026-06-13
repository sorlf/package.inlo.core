using System.Collections.Generic;
using INLO.Core.Events;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace INLO.Core.Events.Editor
{
    public static class EventChannelUsageScanner
    {
        public static EventChannelUsageScanResult Scan(EventChannelInfo channelInfo)
        {
            EventChannelUsageScanResult result = new EventChannelUsageScanResult(channelInfo);

            if (channelInfo == null || channelInfo.Asset == null)
            {
                return result;
            }

            FindAssetDependencyUsages(channelInfo, result);
            FindOpenSceneDetailedUsages(channelInfo.Asset, result);
            FindPrefabDetailedUsages(channelInfo.Asset, result);

            channelInfo.UsageCount = result.TotalUsageCount;

            return result;
        }

        public static List<EventChannelUsageScanResult> ScanAll(List<EventChannelInfo> channels)
        {
            List<EventChannelUsageScanResult> results = new List<EventChannelUsageScanResult>();

            if (channels == null)
            {
                return results;
            }

            foreach (EventChannelInfo channel in channels)
            {
                results.Add(Scan(channel));
            }

            return results;
        }

        private static void FindAssetDependencyUsages(
            EventChannelInfo channelInfo,
            EventChannelUsageScanResult result
        )
        {
            string targetPath = channelInfo.Path;
            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();

            foreach (string assetPath in allAssetPaths)
            {
                if (!assetPath.StartsWith("Assets/"))
                {
                    continue;
                }

                if (assetPath == targetPath)
                {
                    continue;
                }

                string[] dependencies = AssetDatabase.GetDependencies(assetPath, true);

                foreach (string dependency in dependencies)
                {
                    if (dependency == targetPath)
                    {
                        result.AssetDependencyPaths.Add(assetPath);
                        break;
                    }
                }
            }

            result.AssetDependencyPaths.Sort();
        }

        private static void FindOpenSceneDetailedUsages(
            EventChannelBaseSO target,
            EventChannelUsageScanResult result
        )
        {
            int sceneCount = SceneManager.sceneCount;

            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (!scene.isLoaded)
                {
                    continue;
                }

                string scenePath = string.IsNullOrEmpty(scene.path)
                    ? scene.name
                    : scene.path;

                GameObject[] rootObjects = scene.GetRootGameObjects();

                foreach (GameObject rootObject in rootObjects)
                {
                    ScanGameObjectHierarchyForUsages(
                        rootObject,
                        target,
                        "Open Scene",
                        scenePath,
                        result
                    );
                }
            }
        }

        private static void FindPrefabDetailedUsages(
            EventChannelBaseSO target,
            EventChannelUsageScanResult result
        )
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

            foreach (string guid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);

                if (string.IsNullOrEmpty(prefabPath))
                {
                    continue;
                }

                GameObject prefabRoot = null;

                try
                {
                    prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

                    if (prefabRoot == null)
                    {
                        continue;
                    }

                    ScanGameObjectHierarchyForUsages(
                        prefabRoot,
                        target,
                        "Prefab",
                        prefabPath,
                        result
                    );
                }
                finally
                {
                    if (prefabRoot != null)
                    {
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                    }
                }
            }
        }

        private static void ScanGameObjectHierarchyForUsages(
            GameObject root,
            EventChannelBaseSO target,
            string sourceType,
            string assetPath,
            EventChannelUsageScanResult result
        )
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

            foreach (Transform transform in transforms)
            {
                GameObject gameObject = transform.gameObject;
                Component[] components = gameObject.GetComponents<Component>();

                foreach (Component component in components)
                {
                    if (component == null)
                    {
                        continue;
                    }

                    ScanComponentForUsage(
                        component,
                        target,
                        sourceType,
                        assetPath,
                        GetGameObjectPath(gameObject),
                        result
                    );
                }
            }
        }

        private static void ScanComponentForUsage(
            Component component,
            EventChannelBaseSO target,
            string sourceType,
            string assetPath,
            string gameObjectPath,
            EventChannelUsageScanResult result
        )
        {
            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty iterator = serializedObject.GetIterator();

            while (iterator.NextVisible(true))
            {

                if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (iterator.objectReferenceValue != target)
                {
                    continue;
                }

                EventChannelUsageKind kind = DetermineUsageKind(
                    component,
                    iterator.propertyPath
                );

                result.DetailedUsageInfos.Add(
                    new EventChannelUsageInfo(
                        sourceType,
                        assetPath,
                        gameObjectPath,
                        component.GetType().Name,
                        iterator.propertyPath,
                        component.gameObject,
                        kind
                    )
                );
            }
        }

        private static EventChannelUsageKind DetermineUsageKind(Component component, string propertyPath)
        {
            if (IsEventListenerComponent(component))
            {
                return EventChannelUsageKind.Listener;
            }

            string lowerProperty = propertyPath.ToLowerInvariant();
            string lowerComponent = component.GetType().Name.ToLowerInvariant();

            bool propertyLooksLikePublisher =
                lowerProperty.Contains("channel") ||
                lowerProperty.Contains("event");

            bool componentLooksLikePublisher =
                lowerComponent.Contains("tester") ||
                lowerComponent.Contains("controller") ||
                lowerComponent.Contains("service") ||
                lowerComponent.Contains("manager") ||
                lowerComponent.Contains("emitter") ||
                lowerComponent.Contains("publisher") ||
                lowerComponent.Contains("sender") ||
                lowerComponent.Contains("requester") ||
                lowerComponent.Contains("system");

            if (propertyLooksLikePublisher || componentLooksLikePublisher)
            {
                return EventChannelUsageKind.PublisherCandidate;
            }

            return EventChannelUsageKind.Reference;
        }

        private static bool IsEventListenerComponent(Component component)
        {
            System.Type type = component.GetType();

            while (type != null)
            {
                if (type.IsGenericType &&
                    type.GetGenericTypeDefinition().FullName == "INLO.Core.Events.EventListener`2")
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private static string GetGameObjectPath(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return "(None)";
            }

            string path = gameObject.name;
            Transform current = gameObject.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
