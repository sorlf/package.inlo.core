using INLO.Core.UI;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace INLO.Core.Editor.Bootstrap
{
    public static class SceneUiBindingTableValidationRule
    {
        public static IReadOnlyList<BootstrapUiValidationIssue> Validate(
            SceneUiBindingTable table,
            Func<string, bool> isSceneAvailable)
        {
            List<BootstrapUiValidationIssue> issues = new();
            if (table == null)
            {
                issues.Add(new BootstrapUiValidationIssue(
                    BootstrapUiValidationSeverity.Error,
                    "SceneUiBindingTable is missing."));
                return issues;
            }

            IReadOnlyList<SceneUiBindingEntry> entries = table.Entries;
            if (entries == null || entries.Count == 0)
            {
                issues.Add(new BootstrapUiValidationIssue(
                    BootstrapUiValidationSeverity.Error,
                    $"SceneUiBindingTable '{table.name}' has no bindings."));
                return issues;
            }

            HashSet<string> gameSceneNames = new();
            for (int i = 0; i < entries.Count; i++)
            {
                SceneUiBindingEntry entry = entries[i];
                if (entry == null)
                {
                    issues.Add(new BootstrapUiValidationIssue(
                        BootstrapUiValidationSeverity.Error,
                        $"SceneUiBindingTable '{table.name}' has a null entry at index {i}."));
                    continue;
                }

                ValidateEntry(table.name, i, entry, gameSceneNames, isSceneAvailable, issues);
            }

            return issues;
        }

        private static void ValidateEntry(
            string tableName,
            int index,
            SceneUiBindingEntry entry,
            HashSet<string> gameSceneNames,
            Func<string, bool> isSceneAvailable,
            List<BootstrapUiValidationIssue> issues)
        {
            bool hasGameSceneName = !string.IsNullOrWhiteSpace(entry.GameSceneName);
            bool hasUiSceneName = !string.IsNullOrWhiteSpace(entry.UiSceneName);

            if (!hasGameSceneName)
            {
                issues.Add(new BootstrapUiValidationIssue(
                    BootstrapUiValidationSeverity.Error,
                    $"SceneUiBindingTable '{tableName}' entry {index} has no GameSceneName."));
            }
            else if (!gameSceneNames.Add(entry.GameSceneName))
            {
                issues.Add(new BootstrapUiValidationIssue(
                    BootstrapUiValidationSeverity.Error,
                    $"SceneUiBindingTable '{tableName}' has duplicate GameSceneName: {entry.GameSceneName}"));
            }
            else if (isSceneAvailable != null && !isSceneAvailable(entry.GameSceneName))
            {
                issues.Add(new BootstrapUiValidationIssue(
                    BootstrapUiValidationSeverity.Warning,
                    $"GameScene '{entry.GameSceneName}' is not present in Build Settings."));
            }

            if (entry.Policy == SceneUiPolicy.LoadUiScene)
            {
                if (!hasUiSceneName)
                {
                    issues.Add(new BootstrapUiValidationIssue(
                        BootstrapUiValidationSeverity.Error,
                        $"SceneUiBindingTable '{tableName}' entry {index} loads UI but has no UiSceneName."));
                }
                else if (isSceneAvailable != null && !isSceneAvailable(entry.UiSceneName))
                {
                    issues.Add(new BootstrapUiValidationIssue(
                        BootstrapUiValidationSeverity.Warning,
                        $"UIScene '{entry.UiSceneName}' is not present in Build Settings."));
                }

                if (entry.LoadMode != LoadSceneMode.Additive)
                {
                    issues.Add(new BootstrapUiValidationIssue(
                        BootstrapUiValidationSeverity.Error,
                        $"UIScene binding for '{entry.GameSceneName}' must use Additive load mode."));
                }
            }
            else if (entry.Policy == SceneUiPolicy.NoUi)
            {
                if (hasUiSceneName)
                {
                    issues.Add(new BootstrapUiValidationIssue(
                        BootstrapUiValidationSeverity.Error,
                        $"SceneUiBindingTable '{tableName}' entry {index} uses NoUi but still has UiSceneName '{entry.UiSceneName}'."));
                }
            }
            else
            {
                issues.Add(new BootstrapUiValidationIssue(
                    BootstrapUiValidationSeverity.Error,
                    $"SceneUiBindingTable '{tableName}' entry {index} has an unsupported UI policy value: {entry.Policy}."));
            }
        }
    }
}
