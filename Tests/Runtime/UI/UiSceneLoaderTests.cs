using INLO.Core.Bootstrap;
using INLO.Core.UI;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace INLO.Core.Tests
{
    public sealed class UiSceneLoaderTests
    {
        [UnityTest]
        public IEnumerator Execute_WithMissingBindingTable_FailsStep()
        {
            GameObject gameObject = new("UiSceneLoader");
            UiSceneLoader loader = gameObject.AddComponent<UiSceneLoader>();
            SceneTransitionContext context = new("BattleScene", LoadSceneMode.Single, true);

            yield return loader.Execute(context);

            Assert.That(context.Succeeded, Is.False);
            Object.DestroyImmediate(gameObject);
        }

        [UnityTest]
        public IEnumerator Execute_WithNoMatchingBinding_SucceedsWithoutUiWork()
        {
            GameObject gameObject = new("UiSceneLoader");
            SceneUiBindingTable table = ScriptableObject.CreateInstance<SceneUiBindingTable>();
            table.Configure(new[]
            {
                new SceneUiBindingEntry { GameSceneName = "LobbyScene", UiSceneName = "UIScene_Lobby" }
            });
            UiSceneLoader loader = gameObject.AddComponent<UiSceneLoader>();
            loader.Configure(table);
            SceneTransitionContext context = new("BattleScene", LoadSceneMode.Single, true);

            yield return loader.Execute(context);

            Assert.That(context.Succeeded, Is.True);
            Object.DestroyImmediate(table);
            Object.DestroyImmediate(gameObject);
        }

        [UnityTest]
        public IEnumerator Execute_WithNoUiPolicy_Succeeds()
        {
            GameObject gameObject = new("UiSceneLoader");
            SceneUiBindingTable table = ScriptableObject.CreateInstance<SceneUiBindingTable>();
            table.Configure(new[]
            {
                new SceneUiBindingEntry { GameSceneName = "CreditsScene", Policy = SceneUiPolicy.NoUi }
            });
            UiSceneLoader loader = gameObject.AddComponent<UiSceneLoader>();
            loader.Configure(table);
            SceneTransitionContext context = new("CreditsScene", LoadSceneMode.Single, true);

            yield return loader.Execute(context);

            Assert.That(context.Succeeded, Is.True);
            Object.DestroyImmediate(table);
            Object.DestroyImmediate(gameObject);
        }

        [UnityTest]
        public IEnumerator Execute_WithSingleUiLoadMode_FailsStep()
        {
            GameObject gameObject = new("UiSceneLoader");
            SceneUiBindingTable table = ScriptableObject.CreateInstance<SceneUiBindingTable>();
            table.Configure(new[]
            {
                new SceneUiBindingEntry
                {
                    GameSceneName = "BattleScene",
                    UiSceneName = "UIScene_Gameplay",
                    LoadMode = LoadSceneMode.Single
                }
            });
            UiSceneLoader loader = gameObject.AddComponent<UiSceneLoader>();
            loader.Configure(table);
            SceneTransitionContext context = new("BattleScene", LoadSceneMode.Single, true);

            yield return loader.Execute(context);

            Assert.That(context.Succeeded, Is.False);
            Object.DestroyImmediate(table);
            Object.DestroyImmediate(gameObject);
        }
    }
}
