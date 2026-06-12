using INLO.Core.Events;
using UnityEngine;
using UnityEngine.UI;

namespace INLO.Core.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UiButtonEventChannelPublisher : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private VoidEventChannelSO channel;
        [SerializeField] private bool warnIfChannelMissing = true;

        public VoidEventChannelSO Channel => channel;

        public void Configure(VoidEventChannelSO eventChannel, Button sourceButton = null)
        {
            channel = eventChannel;
            if (sourceButton != null)
            {
                button = sourceButton;
            }
        }

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.AddListener(Publish);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(Publish);
            }
        }

        public void Publish()
        {
            if (channel == null)
            {
                if (warnIfChannelMissing)
                {
                    Debug.LogWarning($"{nameof(UiButtonEventChannelPublisher)} on {gameObject.name} has no event channel assigned.", this);
                }

                return;
            }

            channel.RaiseEvent();
        }
    }
}
