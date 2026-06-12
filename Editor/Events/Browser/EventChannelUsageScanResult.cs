using System.Collections.Generic;

namespace INLO.Core.Editor.Events
{
    public sealed class EventChannelUsageScanResult
    {
        public EventChannelInfo Channel { get; }
        public List<string> AssetDependencyPaths { get; } = new List<string>();
        public List<EventChannelUsageInfo> DetailedUsageInfos { get; } = new List<EventChannelUsageInfo>();

        // Compatibility aliases for earlier tool code.
        public List<string> AssetUsagePaths => AssetDependencyPaths;
        public List<EventChannelUsageInfo> DetailedUsages => DetailedUsageInfos;

        public int TotalUsageCount
        {
            get
            {
                return AssetDependencyPaths.Count + DetailedUsageInfos.Count;
            }
        }

        public EventChannelUsageScanResult(EventChannelInfo channel)
        {
            Channel = channel;
        }
    }
}
