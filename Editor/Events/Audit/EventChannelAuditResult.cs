using System.Collections.Generic;

namespace INLO.Core.Editor.Events
{
    public class EventChannelAuditResult
    {
        public List<EventChannelInfo> Channels { get; } = new List<EventChannelInfo>();
        public List<EventChannelAuditIssue> Issues { get; } = new List<EventChannelAuditIssue>();
        public Dictionary<string, EventChannelUsageScanResult> UsageResultsByChannelPath { get; } =
            new Dictionary<string, EventChannelUsageScanResult>();

        public int WarningCount
        {
            get
            {
                int count = 0;

                foreach (EventChannelAuditIssue issue in Issues)
                {
                    if (issue.Severity == EventChannelAuditIssueSeverity.Warning)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int ErrorCount
        {
            get
            {
                int count = 0;

                foreach (EventChannelAuditIssue issue in Issues)
                {
                    if (issue.Severity == EventChannelAuditIssueSeverity.Error)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int InfoCount
        {
            get
            {
                int count = 0;

                foreach (EventChannelAuditIssue issue in Issues)
                {
                    if (issue.Severity == EventChannelAuditIssueSeverity.Info)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }
}
