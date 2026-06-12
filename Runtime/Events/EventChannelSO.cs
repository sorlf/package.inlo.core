using UnityEngine.Events;

namespace INLO.Core.Events
{
    public abstract class EventChannelSO<T> : EventChannelBaseSO
    {
        public event UnityAction<T> OnEventRaised;

        public void RaiseEvent(T value)
        {
            LogEventRaised($"Event raised with value: {value}");

            OnEventRaised?.Invoke(value);
        }
    }
}