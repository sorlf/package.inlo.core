using UnityEngine;
using UnityEngine.Events;

namespace INLO.Core.Events
{
    public class VoidEventListener : MonoBehaviour
    {
        [SerializeField] private VoidEventChannelSO channel;
        [SerializeField] private UnityEvent response;

        private void OnEnable()
        {
            if (channel != null)
                channel.OnEventRaised += Respond;
        }

        private void OnDisable()
        {
            if (channel != null)
                channel.OnEventRaised -= Respond;
        }

        private void Respond()
        {
            response?.Invoke();
        }
    }
}