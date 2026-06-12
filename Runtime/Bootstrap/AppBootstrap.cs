using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace INLO.Core.Bootstrap
{
    public enum BootstrapInitializationState
    {
        NotStarted,
        Initializing,
        Ready,
        Failed
    }

    [DisallowMultipleComponent]
    public sealed class AppBootstrap : MonoBehaviour
    {
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private List<MonoBehaviour> initializers = new();

        private bool _initializationCompleted;

        public static AppBootstrap Instance { get; private set; }

        public event Action<BootstrapInitializationState> InitializationCompleted;

        public bool DontDestroyOnLoadEnabled => dontDestroyOnLoad;
        public BootstrapInitializationState InitializationState { get; private set; }
        public IReadOnlyList<MonoBehaviour> Initializers => initializers;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (initializers == null) return;
            for (int i = 0; i < initializers.Count; i++)
            {
                if (initializers[i] != null && initializers[i] is not IBootstrapInitializer)
                {
                    Debug.LogWarning($"[AppBootstrap] Component '{initializers[i].GetType().Name}' at index {i} does not implement IBootstrapInitializer and will fail at runtime. Please remove it.", this);
                }
            }
        }
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[AppBootstrap] Duplicate AppBootstrap was destroyed before initialization.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializationState = BootstrapInitializationState.Initializing;

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private IEnumerator Start()
        {
            BootstrapInitializationContext context = new();

            for (int i = 0; i < initializers.Count; i++)
            {
                MonoBehaviour component = initializers[i];
                if (component == null)
                {
                    Debug.LogError($"[AppBootstrap] Initializer at index {i} is missing.", this);
                    context.Fail($"Initializer at index {i} is missing.");
                    break;
                }

                if (component is not IBootstrapInitializer initializer)
                {
                    Debug.LogError($"[AppBootstrap] Component does not implement IBootstrapInitializer: {component.GetType().Name}", component);
                    context.Fail($"Component does not implement IBootstrapInitializer: {component.GetType().Name}");
                    break;
                }

                IEnumerator routine;
                try
                {
                    routine = initializer.Initialize(context);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, component);
                    context.Fail(exception.Message);
                    break;
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
                        Debug.LogException(exception, component);
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
                    Debug.LogError($"[AppBootstrap] Initialization failed: {context.FailureMessage}", component);
                    break;
                }
            }

            CompleteInitialization(context.Succeeded
                ? BootstrapInitializationState.Ready
                : BootstrapInitializationState.Failed);
        }

        private void CompleteInitialization(BootstrapInitializationState state)
        {
            if (_initializationCompleted)
            {
                return;
            }

            _initializationCompleted = true;
            InitializationState = state;
            InitializationCompleted?.Invoke(state);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
