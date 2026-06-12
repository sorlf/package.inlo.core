using INLO.Core.Bootstrap;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace INLO.Core.Tests
{
    public sealed class BootstrapRuntimeTests
    {
        [TearDown]
        public void TearDown()
        {
            if (AppBootstrap.Instance != null)
            {
                Object.DestroyImmediate(AppBootstrap.Instance.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator AppBootstrap_WithNoInitializers_BecomesReady()
        {
            GameObject gameObject = new("AppBootstrap");
            AppBootstrap bootstrap = gameObject.AddComponent<AppBootstrap>();
            int completionCount = 0;
            bootstrap.InitializationCompleted += _ => completionCount++;

            yield return null;

            Assert.That(bootstrap.InitializationState, Is.EqualTo(BootstrapInitializationState.Ready));
            Assert.That(completionCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AppBootstrap_RunsInitializersInOrder()
        {
            GameObject gameObject = new("AppBootstrap");
            gameObject.SetActive(false);
            List<int> order = new();
            TestInitializer first = gameObject.AddComponent<TestInitializer>();
            TestInitializer second = gameObject.AddComponent<TestInitializer>();
            first.Configure(1, order, false);
            second.Configure(2, order, false);
            AppBootstrap bootstrap = gameObject.AddComponent<AppBootstrap>();
            SetInitializers(bootstrap, first, second);

            gameObject.SetActive(true);
            yield return null;

            CollectionAssert.AreEqual(new[] { 1, 2 }, order);
            Assert.That(bootstrap.InitializationState, Is.EqualTo(BootstrapInitializationState.Ready));
        }

        [UnityTest]
        public IEnumerator AppBootstrap_WhenInitializerFails_BecomesFailed()
        {
            GameObject gameObject = new("AppBootstrap");
            gameObject.SetActive(false);
            TestInitializer initializer = gameObject.AddComponent<TestInitializer>();
            initializer.Configure(1, new List<int>(), true);
            AppBootstrap bootstrap = gameObject.AddComponent<AppBootstrap>();
            SetInitializers(bootstrap, initializer);
            LogAssert.Expect(LogType.Error, "[AppBootstrap] Initialization failed: Test failure");

            gameObject.SetActive(true);
            yield return null;

            Assert.That(bootstrap.InitializationState, Is.EqualTo(BootstrapInitializationState.Failed));
        }

        [Test]
        public void AppBootstrap_KeepsFirstInstance()
        {
            GameObject firstObject = new("FirstAppBootstrap");
            GameObject duplicateObject = new("DuplicateAppBootstrap");

            try
            {
                AppBootstrap first = firstObject.AddComponent<AppBootstrap>();
                LogAssert.Expect(LogType.Warning, "[AppBootstrap] Duplicate AppBootstrap was destroyed before initialization.");
                duplicateObject.AddComponent<AppBootstrap>();

                Assert.That(AppBootstrap.Instance, Is.SameAs(first));
            }
            finally
            {
                Object.DestroyImmediate(duplicateObject);
                Object.DestroyImmediate(firstObject);
            }
        }

        [UnityTest]
        public IEnumerator SceneLoader_PrepareLoadedScene_RunsStepsInOrder()
        {
            GameObject gameObject = new("SceneLoader");
            List<int> order = new();
            TestTransitionStep first = gameObject.AddComponent<TestTransitionStep>();
            TestTransitionStep second = gameObject.AddComponent<TestTransitionStep>();
            first.Configure(1, order, false);
            second.Configure(2, order, false);
            SceneLoader loader = gameObject.AddComponent<SceneLoader>();
            SceneTransitionResult? result = null;
            loader.TransitionFinished += value => result = value;

            bool accepted = loader.PrepareLoadedScene(SceneManager.GetActiveScene().name);
            while (loader.IsLoading)
            {
                yield return null;
            }

            Assert.That(accepted, Is.True);
            Assert.That(result?.Succeeded, Is.True);
            CollectionAssert.AreEqual(new[] { 1, 2 }, order);
            Object.DestroyImmediate(gameObject);
        }

        [UnityTest]
        public IEnumerator SceneLoader_WithoutSteps_CompletesLoadedScenePreparation()
        {
            GameObject gameObject = new("SceneLoader");
            SceneLoader loader = gameObject.AddComponent<SceneLoader>();
            SceneTransitionResult? result = null;
            loader.TransitionFinished += value => result = value;

            loader.PrepareLoadedScene(SceneManager.GetActiveScene().name);
            while (loader.IsLoading)
            {
                yield return null;
            }

            Assert.That(result?.Succeeded, Is.True);
            Object.DestroyImmediate(gameObject);
        }

        [UnityTest]
        public IEnumerator SceneLoader_WhenStepFails_ReportsFailure()
        {
            GameObject gameObject = new("SceneLoader");
            TestTransitionStep step = gameObject.AddComponent<TestTransitionStep>();
            step.Configure(1, new List<int>(), true);
            SceneLoader loader = gameObject.AddComponent<SceneLoader>();
            SceneTransitionResult? result = null;
            loader.TransitionFinished += value => result = value;
            LogAssert.Expect(LogType.Error, "[SceneLoader] Scene transition step failed: Test failure");

            loader.PrepareLoadedScene(SceneManager.GetActiveScene().name);
            while (loader.IsLoading)
            {
                yield return null;
            }

            Assert.That(result?.Succeeded, Is.False);
            Assert.That(result?.FailureReason, Is.EqualTo(SceneTransitionFailureReason.StepFailed));
            Object.DestroyImmediate(gameObject);
        }

        [UnityTest]
        public IEnumerator SceneLoader_RejectsRequestWhileBusy()
        {
            GameObject gameObject = new("SceneLoader");
            SceneLoader loader = gameObject.AddComponent<SceneLoader>();
            FieldInfo field = typeof(SceneLoader).GetField("_loadingRoutine", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(loader, loader.StartCoroutine(WaitOneFrame()));
            SceneTransitionResult? result = null;
            loader.TransitionFinished += value => result = value;
            LogAssert.Expect(LogType.Error, "[SceneLoader] Scene transition request was rejected because another transition is in progress: BusyScene");

            bool accepted = loader.LoadScene("BusyScene");

            Assert.That(accepted, Is.False);
            Assert.That(result?.FailureReason, Is.EqualTo(SceneTransitionFailureReason.Busy));
            yield return null;
            Object.DestroyImmediate(gameObject);
        }

        private static IEnumerator WaitOneFrame()
        {
            yield return null;
        }

        private static void SetInitializers(AppBootstrap bootstrap, params MonoBehaviour[] initializers)
        {
            FieldInfo field = typeof(AppBootstrap).GetField("initializers", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(bootstrap, new List<MonoBehaviour>(initializers));
        }

        private sealed class TestInitializer : MonoBehaviour, IBootstrapInitializer
        {
            private int _id;
            private List<int> _order;
            private bool _fail;

            public void Configure(int id, List<int> order, bool fail)
            {
                _id = id;
                _order = order;
                _fail = fail;
            }

            public IEnumerator Initialize(BootstrapInitializationContext context)
            {
                _order.Add(_id);
                if (_fail) context.Fail("Test failure");
                yield break;
            }
        }

        private sealed class TestTransitionStep : MonoBehaviour, ISceneTransitionStep
        {
            private int _id;
            private List<int> _order;
            private bool _fail;

            public void Configure(int id, List<int> order, bool fail)
            {
                _id = id;
                _order = order;
                _fail = fail;
            }

            public IEnumerator Execute(SceneTransitionContext context)
            {
                _order.Add(_id);
                if (_fail) context.Fail("Test failure");
                yield break;
            }
        }
    }
}
