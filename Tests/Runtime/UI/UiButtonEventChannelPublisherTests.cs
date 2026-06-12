using INLO.Core.Events;
using INLO.Core.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace INLO.Core.Tests
{
    public sealed class UiButtonEventChannelPublisherTests
    {
        [Test]
        public void ButtonClick_RaisesAssignedVoidEventChannel()
        {
            GameObject gameObject = new("TestButton");

            try
            {
                Button button = gameObject.AddComponent<Button>();
                UiButtonEventChannelPublisher publisher = gameObject.AddComponent<UiButtonEventChannelPublisher>();
                VoidEventChannelSO channel = ScriptableObject.CreateInstance<VoidEventChannelSO>();
                bool raised = false;
                channel.OnEventRaised += () => raised = true;

                publisher.Configure(channel, button);
                publisher.Publish();

                Assert.That(raised, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Publish_WithMissingChannel_DoesNotThrow()
        {
            GameObject gameObject = new("TestButton");

            try
            {
                gameObject.AddComponent<Button>();
                UiButtonEventChannelPublisher publisher = gameObject.AddComponent<UiButtonEventChannelPublisher>();

                Assert.DoesNotThrow(() => publisher.Publish());
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
