using UnityEngine;
using UnityEngine.Events;

namespace INLO.Core.Events
{
    [CreateAssetMenu(menuName = "INLO/Core/Events/Void Event Channel")]
    public class VoidEventChannelSO : EventChannelBaseSO
    {
        public event UnityAction OnEventRaised;

        public void RaiseEvent()
        {
            LogEventRaised("Event raised.");

            OnEventRaised?.Invoke();
        }
    }
}