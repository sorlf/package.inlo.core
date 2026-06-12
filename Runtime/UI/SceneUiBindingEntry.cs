using System;
using UnityEngine.SceneManagement;

namespace INLO.Core.UI
{
    public enum SceneUiPolicy
    {
        LoadUiScene,
        NoUi
    }

    [Serializable]
    public sealed class SceneUiBindingEntry
    {
        public string GameSceneName;
        public SceneUiPolicy Policy = SceneUiPolicy.LoadUiScene;
        public string UiSceneName;
        public LoadSceneMode LoadMode = LoadSceneMode.Additive;
        public bool UnloadPreviousUi = true;
    }
}
