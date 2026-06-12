using UnityEngine;

namespace INLO.Core.Editor.Events
{
    /// <summary>
    /// Detailed usage information for an EventChannel reference.
    /// 
    /// This type intentionally exposes both UsageKind and Kind.
    /// Some editor tools may reference UsageKind, while newer/older files may reference Kind.
    /// Keeping both properties prevents Browser, Audit, Graph, and CI tools from drifting out of sync.
    /// </summary>
    public sealed class EventChannelUsageInfo
    {
        public string SourceType { get; }
        public string AssetPath { get; }
        public string GameObjectPath { get; }
        public string ComponentType { get; }
        public string PropertyPath { get; }
        public GameObject TargetObject { get; }

        public EventChannelUsageKind UsageKind { get; set; }

        public EventChannelUsageKind Kind
        {
            get => UsageKind;
            set => UsageKind = value;
        }

        public EventChannelUsageInfo(
            string sourceType,
            string assetPath,
            string gameObjectPath,
            string componentType,
            string propertyPath,
            GameObject targetObject
        )
            : this(
                sourceType,
                assetPath,
                gameObjectPath,
                componentType,
                propertyPath,
                targetObject,
                EventChannelUsageKind.Reference
            )
        {
        }

        public EventChannelUsageInfo(
            string sourceType,
            string assetPath,
            string gameObjectPath,
            string componentType,
            string propertyPath,
            GameObject targetObject,
            EventChannelUsageKind usageKind
        )
        {
            SourceType = sourceType ?? string.Empty;
            AssetPath = assetPath ?? string.Empty;
            GameObjectPath = gameObjectPath ?? string.Empty;
            ComponentType = componentType ?? string.Empty;
            PropertyPath = propertyPath ?? string.Empty;
            TargetObject = targetObject;
            UsageKind = usageKind;
        }

        public EventChannelUsageInfo(
            string sourceType,
            string assetPath,
            string gameObjectPath,
            string componentType,
            string propertyPath,
            EventChannelUsageKind usageKind,
            GameObject targetObject
        )
            : this(
                sourceType,
                assetPath,
                gameObjectPath,
                componentType,
                propertyPath,
                targetObject,
                usageKind
            )
        {
        }
    }
}
