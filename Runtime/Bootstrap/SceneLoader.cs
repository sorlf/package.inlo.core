using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace INLO.Core.Bootstrap
{
    public enum SceneTransitionFailureReason
    {
        None,
        InvalidSceneName,
        Busy,
        SceneUnavailable,
        SceneNotLoaded,
        SceneLoadFailed,
        StepFailed
    }

    public readonly struct SceneTransitionResult
    {
        public SceneTransitionResult(string sceneName, bool succeeded, SceneTransitionFailureReason failureReason)
        {
            SceneName = sceneName;
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public string SceneName { get; }
        public bool Succeeded { get; }
        public SceneTransitionFailureReason FailureReason { get; }
    }

    [DisallowMultipleComponent]
    public sealed class SceneLoader : MonoBehaviour
    {
        private readonly List<ISceneTransitionStep> _steps = new();
        private Coroutine _loadingRoutine;

        public bool IsLoading => _loadingRoutine != null;

        public event Action<SceneTransitionResult> TransitionFinished;

        private void Awake()
        {
            RefreshSteps();
        }

        public void RefreshSteps()
        {
            _steps.Clear();
            MonoBehaviour[] components = GetComponents<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is ISceneTransitionStep step)
                {
                    _steps.Add(step);
                }
            }
        }

        public bool LoadScene(string sceneName, LoadSceneMode loadMode = LoadSceneMode.Single)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return Reject(sceneName, SceneTransitionFailureReason.InvalidSceneName, "[SceneLoader] Scene name is empty.");
            }

            if (_loadingRoutine != null)
            {
                return Reject(sceneName, SceneTransitionFailureReason.Busy, $"[SceneLoader] Scene transition request was rejected because another transition is in progress: {sceneName}");
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                return Reject(sceneName, SceneTransitionFailureReason.SceneUnavailable, $"[SceneLoader] Scene is not available in build settings: {sceneName}");
            }

            _loadingRoutine = StartCoroutine(LoadSceneRoutine(sceneName, loadMode));
            return true;
        }

        public bool PrepareLoadedScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return Reject(sceneName, SceneTransitionFailureReason.InvalidSceneName, "[SceneLoader] Scene name is empty.");
            }

            if (_loadingRoutine != null)
            {
                return Reject(sceneName, SceneTransitionFailureReason.Busy, $"[SceneLoader] Scene transition request was rejected because another transition is in progress: {sceneName}");
            }

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return Reject(sceneName, SceneTransitionFailureReason.SceneNotLoaded, $"[SceneLoader] Scene is not loaded: {sceneName}");
            }

            _loadingRoutine = StartCoroutine(RunStepsRoutine(sceneName, LoadSceneMode.Additive, false));
            return true;
        }

        private IEnumerator LoadSceneRoutine(string sceneName, LoadSceneMode loadMode)
        {
            yield return null;

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, loadMode);
            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] Failed to load scene: {sceneName}");
                Complete(sceneName, false, SceneTransitionFailureReason.SceneLoadFailed);
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }

            yield return RunStepsRoutine(sceneName, loadMode, true);
        }

        private IEnumerator RunStepsRoutine(string sceneName, LoadSceneMode loadMode, bool sceneWasLoaded)
        {
            yield return null;

            SceneTransitionContext context = new(sceneName, loadMode, sceneWasLoaded);
            for (int i = 0; i < _steps.Count; i++)
            {
                IEnumerator routine;
                try
                {
                    routine = _steps[i].Execute(context);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                    context.Fail(exception.Message);
                    routine = null;
                }

                while (routine != null)
                {
                    bool hasNext;
                    object current = null;
                    try
                    {
                        hasNext = routine.MoveNext();
                        if (hasNext)
                        {
                            current = routine.Current;
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, this);
                        context.Fail(exception.Message);
                        break;
                    }

                    if (!hasNext)
                    {
                        break;
                    }

                    yield return current;
                }

                if (!context.Succeeded)
                {
                    Debug.LogError($"[SceneLoader] Scene transition step failed: {context.FailureMessage}", this);
                    Complete(sceneName, false, SceneTransitionFailureReason.StepFailed);
                    yield break;
                }
            }

            Complete(sceneName, true, SceneTransitionFailureReason.None);
        }

        private bool Reject(string sceneName, SceneTransitionFailureReason failureReason, string message)
        {
            Debug.LogError(message);
            TransitionFinished?.Invoke(new SceneTransitionResult(sceneName, false, failureReason));
            return false;
        }

        private void Complete(string sceneName, bool succeeded, SceneTransitionFailureReason failureReason)
        {
            _loadingRoutine = null;
            TransitionFinished?.Invoke(new SceneTransitionResult(sceneName, succeeded, failureReason));
        }
    }
}
