using INLO.Core.Events;
using UnityEditor;

namespace INLO.Core.Events.Editor
{
    public static class EventChannelAuditRunner
    {
        public static EventChannelAuditResult Run(bool scanUsages)
        {
            EventChannelAuditResult result = new EventChannelAuditResult();

            string[] guids = AssetDatabase.FindAssets("t:EventChannelBaseSO");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EventChannelBaseSO asset = AssetDatabase.LoadAssetAtPath<EventChannelBaseSO>(path);

                if (asset == null)
                {
                    continue;
                }

                EventChannelInfo channelInfo = new EventChannelInfo(asset, path);
                result.Channels.Add(channelInfo);

                AuditDescription(channelInfo, result);
                AuditDebugLog(channelInfo, result);

                if (scanUsages)
                {
                    AuditUsages(channelInfo, result);
                }
            }

            result.Channels.Sort((a, b) =>
                string.Compare(a.Asset.name, b.Asset.name, System.StringComparison.Ordinal)
            );

            return result;
        }

        private static void AuditDescription(EventChannelInfo channelInfo, EventChannelAuditResult result)
        {
            EventChannelDescriptionQuality quality = channelInfo.GetDescriptionQuality();

            if (quality == EventChannelDescriptionQuality.Missing)
            {
                result.Issues.Add(
                    new EventChannelAuditIssue(
                        EventChannelAuditIssueSeverity.Warning,
                        EventChannelAuditIssueType.MissingDescription,
                        channelInfo,
                        "Description이 비어 있습니다.",
                        "이 채널이 언제 발생하고 어떤 시스템이 들어야 하는지 Description에 작성하세요."
                    )
                );
            }
            else if (quality == EventChannelDescriptionQuality.TooShort)
            {
                result.Issues.Add(
                    new EventChannelAuditIssue(
                        EventChannelAuditIssueSeverity.Info,
                        EventChannelAuditIssueType.TooShortDescription,
                        channelInfo,
                        "Description이 너무 짧습니다.",
                        "최소한 발생 조건, 상태 저장 금지 여부, 대표 수신자를 적는 것을 권장합니다."
                    )
                );
            }
        }

        private static void AuditDebugLog(EventChannelInfo channelInfo, EventChannelAuditResult result)
        {
            if (!channelInfo.Asset.IsDebugLogEnabled)
            {
                return;
            }

            result.Issues.Add(
                new EventChannelAuditIssue(
                    EventChannelAuditIssueSeverity.Warning,
                    EventChannelAuditIssueType.DebugLogEnabled,
                    channelInfo,
                    "Debug Log가 켜져 있습니다.",
                    "개발 중에는 유용하지만 Release 빌드 전에는 꺼야 합니다."
                )
            );
        }

        private static void AuditUsages(EventChannelInfo channelInfo, EventChannelAuditResult result)
        {
            EventChannelUsageScanResult usageResult = EventChannelUsageScanner.Scan(channelInfo);
            result.UsageResultsByChannelPath[channelInfo.Path] = usageResult;
            channelInfo.UsageCount = usageResult.TotalUsageCount;

            if (usageResult.TotalUsageCount == 0)
            {
                result.Issues.Add(
                    new EventChannelAuditIssue(
                        EventChannelAuditIssueSeverity.Warning,
                        EventChannelAuditIssueType.UnusedCandidate,
                        channelInfo,
                        "사용처를 찾지 못했습니다.",
                        "삭제 후보일 수 있습니다. 단, Addressables key, Resources.Load, 런타임 동적 연결은 스캔에 잡히지 않을 수 있습니다."
                    )
                );

                return;
            }

            int listenerCount = 0;

            foreach (EventChannelUsageInfo usageInfo in usageResult.DetailedUsages)
            {
                if (usageInfo.UsageKind == EventChannelUsageKind.Listener)
                {
                    listenerCount++;
                }
            }

            if (listenerCount == 0)
            {
                result.Issues.Add(
                    new EventChannelAuditIssue(
                        EventChannelAuditIssueSeverity.Info,
                        EventChannelAuditIssueType.NoListener,
                        channelInfo,
                        "상세 스캔에서 Listener를 찾지 못했습니다.",
                        "이벤트가 발행만 되고 수신되지 않는 구조인지 확인하세요. 닫혀 있는 Scene 내부 Listener는 상세 스캔에 잡히지 않을 수 있습니다."
                    )
                );
            }

            if (usageResult.DetailedUsages.Count == 0 && usageResult.AssetDependencyPaths.Count > 0)
            {
                result.Issues.Add(
                    new EventChannelAuditIssue(
                        EventChannelAuditIssueSeverity.Info,
                        EventChannelAuditIssueType.NoDetailedUsage,
                        channelInfo,
                        "Asset dependency는 있지만 상세 GameObject 사용처를 찾지 못했습니다.",
                        "닫혀 있는 Scene 내부 참조일 가능성이 있습니다. 필요한 경우 해당 Scene을 열고 다시 스캔하세요."
                    )
                );
            }
        }
    }
}
