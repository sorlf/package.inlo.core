using System.Collections.Generic;
using System.Text;
using INLO.Core.Events;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace INLO.Core.Editor.Events
{
    public class EventChannelBuildValidator : IPreprocessBuildWithReport
    {
        private const int MaxListedChannels = 20;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (IsDevelopmentBuild(report))
            {
                return;
            }

            List<DebugLogChannelInfo> debugLogChannels = FindDebugLogEnabledChannels();

            if (debugLogChannels.Count == 0)
            {
                return;
            }

            string message = BuildFailureMessage(debugLogChannels);
            throw new BuildFailedException(message);
        }

        [MenuItem("Tools/INLO/Events/Validation/Validate Release Build Rules")]
        private static void ValidateReleaseBuildRules()
        {
            List<DebugLogChannelInfo> debugLogChannels = FindDebugLogEnabledChannels();

            if (debugLogChannels.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "INLO Event Build Validation",
                    "검증 통과.\n\nDebug Log가 켜진 EventChannel이 없습니다.",
                    "OK"
                );
                return;
            }

            bool selectFirst = EditorUtility.DisplayDialog(
                "INLO Event Build Validation Failed",
                BuildFailureMessage(debugLogChannels) + "\n\n첫 번째 문제 Channel을 선택할까요?",
                "Select First",
                "Close"
            );

            if (selectFirst)
            {
                SelectAndPing(debugLogChannels[0].Asset);
            }
        }

        [MenuItem("Tools/INLO/Events/Validation/Select First Debug Log Channel")]
        private static void SelectFirstDebugLogChannel()
        {
            List<DebugLogChannelInfo> debugLogChannels = FindDebugLogEnabledChannels();

            if (debugLogChannels.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "INLO Event Channels",
                    "Debug Log가 켜진 EventChannel이 없습니다.",
                    "OK"
                );
                return;
            }

            SelectAndPing(debugLogChannels[0].Asset);
        }

        internal static List<DebugLogChannelInfo> FindDebugLogEnabledChannels()
        {
            List<DebugLogChannelInfo> result = new List<DebugLogChannelInfo>();
            string[] guids = AssetDatabase.FindAssets("t:EventChannelBaseSO");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EventChannelBaseSO asset = AssetDatabase.LoadAssetAtPath<EventChannelBaseSO>(path);

                if (asset == null)
                {
                    continue;
                }

                if (!asset.IsDebugLogEnabled)
                {
                    continue;
                }

                result.Add(new DebugLogChannelInfo(asset, path));
            }

            result.Sort((a, b) => string.Compare(a.Path, b.Path, System.StringComparison.Ordinal));
            return result;
        }

        private static bool IsDevelopmentBuild(BuildReport report)
        {
            return (report.summary.options & BuildOptions.Development) == BuildOptions.Development;
        }

        private static string BuildFailureMessage(List<DebugLogChannelInfo> debugLogChannels)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("Release Build 검증 실패");
            builder.AppendLine();
            builder.AppendLine("Debug Log가 켜진 EventChannel이 있습니다.");
            builder.AppendLine("Release 빌드에서는 EventChannel Debug Log를 끄고 빌드해야 합니다.");
            builder.AppendLine();
            builder.AppendLine($"Found: {debugLogChannels.Count}");
            builder.AppendLine();

            int listedCount = Mathf.Min(MaxListedChannels, debugLogChannels.Count);

            for (int i = 0; i < listedCount; i++)
            {
                DebugLogChannelInfo info = debugLogChannels[i];
                builder.AppendLine($"- {info.Asset.name}");
                builder.AppendLine($"  {info.Path}");
            }

            if (debugLogChannels.Count > MaxListedChannels)
            {
                builder.AppendLine($"...and {debugLogChannels.Count - MaxListedChannels} more.");
            }

            builder.AppendLine();
            builder.AppendLine("해결 방법:");
            builder.AppendLine("1. Tools > INLO > Events > Event Channel Browser를 엽니다.");
            builder.AppendLine("2. Debug Log On만 보기 필터를 켭니다.");
            builder.AppendLine("3. 표시된 EventChannel들의 Debug Log를 끕니다.");
            builder.AppendLine("4. 다시 빌드합니다.");

            return builder.ToString();
        }

        private static void SelectAndPing(Object asset)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        internal class DebugLogChannelInfo
        {
            public EventChannelBaseSO Asset { get; }
            public string Path { get; }

            public DebugLogChannelInfo(EventChannelBaseSO asset, string path)
            {
                Asset = asset;
                Path = path;
            }
        }
    }
}
