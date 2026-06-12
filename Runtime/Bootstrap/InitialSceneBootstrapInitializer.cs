using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace INLO.Core.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class InitialSceneBootstrapInitializer : MonoBehaviour, IBootstrapInitializer
    {
        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private string initialSceneName;
        [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;

        public IEnumerator Initialize(BootstrapInitializationContext context)
        {
            if (sceneLoader == null)
            {
                context.Fail("Initial Scene initializer has no SceneLoader.");
                yield break;
            }

            SceneTransitionResult? result = null;
            void OnTransitionFinished(SceneTransitionResult value) => result = value;

            sceneLoader.TransitionFinished += OnTransitionFinished;
            bool accepted = sceneLoader.LoadScene(initialSceneName, loadMode);

            while (accepted && !result.HasValue)
            {
                yield return null;
            }

            sceneLoader.TransitionFinished -= OnTransitionFinished;

            if (!result.HasValue || !result.Value.Succeeded)
            {
                string reason = result.HasValue ? result.Value.FailureReason.ToString() : "RequestRejected";
                context.Fail($"Initial scene transition failed: {reason}");
            }
        }
    }
}
