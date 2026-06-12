using System.Collections;
using UnityEngine.SceneManagement;

namespace INLO.Core.Bootstrap
{
    public interface ISceneTransitionStep
    {
        IEnumerator Execute(SceneTransitionContext context);
    }

    public sealed class SceneTransitionContext
    {
        public SceneTransitionContext(string sceneName, LoadSceneMode loadMode, bool sceneWasLoaded)
        {
            SceneName = sceneName;
            LoadMode = loadMode;
            SceneWasLoaded = sceneWasLoaded;
        }

        public string SceneName { get; }
        public LoadSceneMode LoadMode { get; }
        public bool SceneWasLoaded { get; }
        public bool Succeeded { get; private set; } = true;
        public string FailureMessage { get; private set; }

        public void Fail(string message)
        {
            Succeeded = false;
            FailureMessage = message;
        }
    }
}
