using UnityEngine;

namespace INLO.Core.Events
{
    public abstract class EventChannelBaseSO : ScriptableObject
    {
        [SerializeField, TextArea]
        private string description;

        [SerializeField]
        private bool debugLog;

        public string Description => description;
        public bool IsDebugLogEnabled => debugLog;

        protected bool DebugLogEnabled => debugLog;

        protected void LogEventRaised(string message)
        {
            if (!debugLog)
            {
                return;
            }

            Debug.Log($"[{name}] {message}");
        }
    }
}