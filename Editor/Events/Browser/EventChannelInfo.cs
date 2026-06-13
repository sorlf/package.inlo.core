using INLO.Core.Events;

namespace INLO.Core.Events.Editor
{
    public sealed class EventChannelInfo
    {
        public EventChannelBaseSO Asset { get; }
        public string Path { get; }
        public int UsageCount { get; set; } = -1;

        public EventChannelInfo(EventChannelBaseSO asset, string path)
        {
            Asset = asset;
            Path = path;
        }

        public EventChannelDescriptionQuality GetDescriptionQuality()
        {
            if (Asset == null || string.IsNullOrWhiteSpace(Asset.Description))
            {
                return EventChannelDescriptionQuality.Missing;
            }

            if (Asset.Description.Trim().Length < 20)
            {
                return EventChannelDescriptionQuality.TooShort;
            }

            return EventChannelDescriptionQuality.Ok;
        }

        public bool IsUnusedCandidate()
        {
            return UsageCount == 0;
        }
    }
}
