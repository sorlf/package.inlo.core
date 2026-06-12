using INLO.Core.Editor.Bootstrap;
using INLO.Core.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace INLO.Core.Tests.Editor
{
    public sealed class SceneUiBindingTableValidationRuleTests
    {
        [Test]
        public void Validate_ReportsMissingTable()
        {
            var issues = SceneUiBindingTableValidationRule.Validate(null, _ => true);

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Severity, Is.EqualTo(BootstrapUiValidationSeverity.Error));
        }

        [Test]
        public void Validate_ReportsInvalidEntries()
        {
            SceneUiBindingTable table = ScriptableObject.CreateInstance<SceneUiBindingTable>();
            table.Configure(new[]
            {
                new SceneUiBindingEntry { GameSceneName = "", UiSceneName = "UIScene_Gameplay" },
                new SceneUiBindingEntry { GameSceneName = "BattleScene", UiSceneName = "UIScene_Gameplay" },
                new SceneUiBindingEntry { GameSceneName = "BattleScene", UiSceneName = "" }
            });

            try
            {
                var issues = SceneUiBindingTableValidationRule.Validate(table, _ => true);

                Assert.That(issues, Has.Count.EqualTo(3));
                Assert.That(issues[0].Severity, Is.EqualTo(BootstrapUiValidationSeverity.Error));
                Assert.That(issues[1].Severity, Is.EqualTo(BootstrapUiValidationSeverity.Error));
                Assert.That(issues[2].Severity, Is.EqualTo(BootstrapUiValidationSeverity.Error));
            }
            finally
            {
                Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void Validate_AllowsExplicitNoUi()
        {
            SceneUiBindingTable table = ScriptableObject.CreateInstance<SceneUiBindingTable>();
            table.Configure(new[]
            {
                new SceneUiBindingEntry { GameSceneName = "CreditsScene", Policy = SceneUiPolicy.NoUi }
            });

            try
            {
                var issues = SceneUiBindingTableValidationRule.Validate(table, _ => true);

                Assert.That(issues, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void Validate_DoesNotRequireBindingsForUnlistedScenes()
        {
            SceneUiBindingTable table = ScriptableObject.CreateInstance<SceneUiBindingTable>();
            table.Configure(new[]
            {
                new SceneUiBindingEntry { GameSceneName = "LoginScene", UiSceneName = "UIScene_Login" }
            });

            try
            {
                var issues = SceneUiBindingTableValidationRule.Validate(table, _ => true);

                Assert.That(issues, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void Validate_ReportsNoUiWithUiSceneAndNonAdditiveLoad()
        {
            SceneUiBindingTable table = ScriptableObject.CreateInstance<SceneUiBindingTable>();
            table.Configure(new[]
            {
                new SceneUiBindingEntry
                {
                    GameSceneName = "CreditsScene",
                    Policy = SceneUiPolicy.NoUi,
                    UiSceneName = "UIScene_Credits"
                },
                new SceneUiBindingEntry
                {
                    GameSceneName = "BattleScene",
                    UiSceneName = "UIScene_Gameplay",
                    LoadMode = LoadSceneMode.Single
                }
            });

            try
            {
                var issues = SceneUiBindingTableValidationRule.Validate(table, _ => true);

                Assert.That(issues, Has.Count.EqualTo(2));
                Assert.That(issues[0].Severity, Is.EqualTo(BootstrapUiValidationSeverity.Error));
                Assert.That(issues[1].Severity, Is.EqualTo(BootstrapUiValidationSeverity.Error));
            }
            finally
            {
                Object.DestroyImmediate(table);
            }
        }
    }
}
