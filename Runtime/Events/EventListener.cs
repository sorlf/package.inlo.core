using UnityEngine;

namespace INLO.Core.Events
{
    public abstract class EventListener<TEventData, TChannel> : MonoBehaviour
        where TChannel : EventChannelSO<TEventData>
    {
        [SerializeField] private TChannel channel;
        [SerializeField] private bool warnIfChannelMissing = true;

        protected TChannel Channel => channel;

        protected virtual void OnEnable()
        {
            if (channel == null)
            {
                if (warnIfChannelMissing)
                {
                    Debug.LogWarning($"{GetType().Name} on {gameObject.name} has no event channel assigned.", this);
                }

                return;
            }

            channel.OnEventRaised += OnEventRaised;
        }

        protected virtual void OnDisable()
        {
            if (channel == null)
            {
                return;
            }

            channel.OnEventRaised -= OnEventRaised;
        }

        protected abstract void OnEventRaised(TEventData data);
    }
}