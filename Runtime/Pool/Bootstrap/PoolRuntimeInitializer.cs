using UnityEngine;
using UnityEngine.SceneManagement;

namespace INLO.Core.Pooling
{
    internal static class PoolRuntimeInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPoolManagerStaticState()
        {
            PoolManager.ResetStaticState();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterSceneLoadedCleanup()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            PoolManager.CleanupInvalidReferences();
        }
    }
}
