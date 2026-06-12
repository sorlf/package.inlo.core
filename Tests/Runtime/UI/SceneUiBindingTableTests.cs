using INLO.Core.UI;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace INLO.Core.Tests
{
    public sealed class SceneUiBindingTableTests
    {
        [Test]
        public void TryFind_ReturnsMatchingBinding()
        {
            SceneUiBindingTable table = UnityEngine.ScriptableObject.CreateInstance<SceneUiBindingTable>();
            SceneUiBindingEntry battleBinding = new()
            {
                GameSceneName = "BattleScene",
                UiSceneName = "UIScene_Gameplay",
                LoadMode = LoadSceneMode.Additive,
                UnloadPreviousUi = true
            };

            table.Configure(new[]
            {
                new SceneUiBindingEntry { GameSceneName = "LobbyScene", UiSceneName = "UIScene_Lobby" },
                battleBinding
            });

            bool found = table.TryFind("BattleScene", out SceneUiBindingEntry entry);

            Assert.That(found, Is.True);
            Assert.That(entry, Is.SameAs(battleBinding));
        }

        [Test]
        public void TryFind_RejectsEmptyAndUnknownSceneNames()
        {
            SceneUiBindingTable table = UnityEngine.ScriptableObject.CreateInstance<SceneUiBindingTable>();
            table.Configure(new[]
            {
                new SceneUiBindingEntry { GameSceneName = "BattleScene", UiSceneName = "UIScene_Gameplay" }
            });

            Assert.That(table.TryFind("", out SceneUiBindingEntry emptyEntry), Is.False);
            Assert.That(emptyEntry, Is.Null);
            Assert.That(table.TryFind("MissingScene", out SceneUiBindingEntry missingEntry), Is.False);
            Assert.That(missingEntry, Is.Null);
        }
    }
}
