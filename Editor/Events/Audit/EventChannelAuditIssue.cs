namespace INLO.Core.Editor.Events
{
    public sealed class EventChannelAuditIssue
    {
        public EventChannelAuditIssueSeverity Severity { get; }
        public EventChannelAuditIssueType Type { get; }

        // 기존 코드 호환용
        public EventChannelInfo Channel { get; }

        // CI / 로그 출력용
        public string ChannelName { get; }
        public string ChannelPath { get; }

        public string Message { get; }
        public string Recommendation { get; }

        public EventChannelAuditIssue(
            EventChannelAuditIssueSeverity severity,
            EventChannelAuditIssueType type,
            EventChannelInfo channel,
            string message,
            string recommendation
        )
        {
            Severity = severity;
            Type = type;
            Channel = channel;

            ChannelName = channel != null && channel.Asset != null
                ? channel.Asset.name
                : "(Unknown Channel)";

            ChannelPath = channel != null
                ? channel.Path
                : string.Empty;

            Message = message;
            Recommendation = recommendation;
        }

        public EventChannelAuditIssue(
            EventChannelAuditIssueSeverity severity,
            EventChannelAuditIssueType type,
            string channelName,
            string channelPath,
            string message,
            string recommendation
        )
        {
            Severity = severity;
            Type = type;
            Channel = null;

            ChannelName = string.IsNullOrWhiteSpace(channelName)
                ? "(Unknown Channel)"
                : channelName;

            ChannelPath = channelPath ?? string.Empty;

            Message = message;
            Recommendation = recommendation;
        }

        public EventChannelAuditIssue(
            EventChannelAuditIssueSeverity severity,
            EventChannelAuditIssueType type,
            string message,
            string recommendation
        )
        {
            Severity = severity;
            Type = type;
            Channel = null;
            ChannelName = "(Unknown Channel)";
            ChannelPath = string.Empty;
            Message = message;
            Recommendation = recommendation;
        }
    }
}