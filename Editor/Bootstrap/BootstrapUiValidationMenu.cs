using INLO.Core.UI;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace INLO.Core.Editor.Bootstrap
{
    public static class BootstrapUiValidationMenu
    {
        [MenuItem("Tools/INLO/UI Scene Binding/Validate Tables")]
        public static void ValidateSceneUiBindingTablesInProject()
        {
            string[] guids = AssetDatabase.FindAssets("t:SceneUiBindingTable");
            if (guids.Length == 0)
            {
                Debug.Log("[UiSceneBindingValidator] No SceneUiBindingTable assets found. UI Scene binding is optional.");
                return;
            }

            int errorCount = 0;
            int warningCount = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                SceneUiBindingTable table = AssetDatabase.LoadAssetAtPath<SceneUiBindingTable>(path);
                IReadOnlyList<BootstrapUiValidationIssue> issues =
                    SceneUiBindingTableValidationRule.Validate(table, IsSceneInBuildSettings);

                for (int j = 0; j < issues.Count; j++)
                {
                    BootstrapUiValidationIssue issue = issues[j];
                    if (issue.Severity == BootstrapUiValidationSeverity.Error)
                    {
                        errorCount++;
                        Debug.LogError($"[UiSceneBindingValidator] {issue.Message}", table);
                    }
                    else if (issue.Severity == BootstrapUiValidationSeverity.Warning)
                    {
                        warningCount++;
                        Debug.LogWarning($"[UiSceneBindingValidator] {issue.Message}", table);
                    }
                }
            }

            if (errorCount > 0)
            {
                Debug.LogError($"[UiSceneBindingValidator] Completed with errors. Errors: {errorCount}, Warnings: {warningCount}");
            }
            else if (warningCount > 0)
            {
                Debug.LogWarning($"[UiSceneBindingValidator] Completed with warnings. Errors: {errorCount}, Warnings: {warningCount}");
            }
            else
            {
                Debug.Log("[UiSceneBindingValidator] Completed. No issues found.");
            }
        }

        private static bool IsSceneInBuildSettings(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (Path.GetFileNameWithoutExtension(scenes[i].path) == sceneName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
