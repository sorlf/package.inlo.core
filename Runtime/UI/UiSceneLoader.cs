using System.Collections;
using INLO.Core.Bootstrap;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace INLO.Core.UI
{
    [DisallowMultipleComponent]
    public sealed class UiSceneLoader : MonoBehaviour, ISceneTransitionStep
    {
        [SerializeField] private SceneUiBindingTable bindingTable;

        private string _loadedUiSceneName;

        public string LoadedUiSceneName => _loadedUiSceneName;

        public void Configure(SceneUiBindingTable table)
        {
            bindingTable = table;
        }

        public IEnumerator Execute(SceneTransitionContext context)
        {
            if (bindingTable == null)
            {
                context.Fail("UiSceneLoader has no SceneUiBindingTable.");
                yield break;
            }

            if (!bindingTable.TryFind(context.SceneName, out SceneUiBindingEntry entry))
            {
                yield break;
            }

            if (entry.Policy == SceneUiPolicy.NoUi)
            {
                yield return UnloadPreviousUi();
                yield break;
            }

            if (string.IsNullOrWhiteSpace(entry.UiSceneName))
            {
                context.Fail($"UI scene name is empty for scene: {context.SceneName}");
                yield break;
            }

            if (entry.LoadMode != LoadSceneMode.Additive)
            {
                context.Fail($"UI scene must use Additive load mode: {entry.UiSceneName}");
                yield break;
            }

            if (entry.UnloadPreviousUi && !string.IsNullOrWhiteSpace(_loadedUiSceneName) && _loadedUiSceneName != entry.UiSceneName)
            {
                yield return UnloadPreviousUi();
            }

            Scene targetScene = SceneManager.GetSceneByName(entry.UiSceneName);
            if (targetScene.IsValid() && targetScene.isLoaded)
            {
                _loadedUiSceneName = entry.UiSceneName;
                yield break;
            }

            if (!Application.CanStreamedLevelBeLoaded(entry.UiSceneName))
            {
                context.Fail($"UI scene is not available in build settings: {entry.UiSceneName}");
                yield break;
            }

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(entry.UiSceneName, entry.LoadMode);
            if (loadOperation == null)
            {
                context.Fail($"Failed to load UI scene: {entry.UiSceneName}");
                yield break;
            }

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            _loadedUiSceneName = entry.UiSceneName;
        }

        private IEnumerator UnloadPreviousUi()
        {
            if (string.IsNullOrWhiteSpace(_loadedUiSceneName))
            {
                yield break;
            }

            Scene previousScene = SceneManager.GetSceneByName(_loadedUiSceneName);
            if (previousScene.IsValid() && previousScene.isLoaded)
            {
                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(previousScene);
                while (unloadOperation != null && !unloadOperation.isDone)
                {
                    yield return null;
                }
            }

            _loadedUiSceneName = null;
        }
    }
}
