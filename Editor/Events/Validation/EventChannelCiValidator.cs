using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace INLO.Core.Events.Editor
{
    /// <summary>
    /// Command-line and menu validation entry point for INLO EventChannel rules.
    ///
    /// Unity batch mode example:
    /// Unity.exe -batchmode -quit -projectPath "<PROJECT_PATH>" -executeMethod INLO.Core.Events.Editor.EventChannelCiValidator.Run
    ///
    /// 이 검증기는 CI나 빌드 파이프라인에서 EventChannel 규칙 위반을 자동으로 잡기 위한 진입점입니다.
    /// </summary>
    public static class EventChannelCiValidator
    {
        private const bool DefaultScanUsages = true;
        private const bool DefaultFailOnWarnings = true;
        private const int MaxListedIssues = 50;

        [MenuItem("Tools/INLO/Events/Validation/Run CI Validation")]
        public static void RunFromMenu()
        {
            EventChannelCiValidationResult result = RunValidation(
                DefaultScanUsages,
                DefaultFailOnWarnings
            );

            EditorUtility.DisplayDialog(
                result.Passed ? "INLO Event CI Validation Passed" : "INLO Event CI Validation Failed",
                result.Message,
                "OK"
            );
        }

        /// <summary>
        /// CI entry point.
        /// Use this with Unity -executeMethod.
        /// </summary>
        public static void Run()
        {
            bool scanUsages = ReadBoolCommandLineArg("-inloEventScanUsages", DefaultScanUsages);
            bool failOnWarnings = ReadBoolCommandLineArg("-inloEventFailOnWarnings", DefaultFailOnWarnings);

            EventChannelCiValidationResult result = RunValidation(scanUsages, failOnWarnings);

            if (result.Passed)
            {
                Debug.Log(result.Message);

                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }

                return;
            }

            Debug.LogError(result.Message);

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        private static EventChannelCiValidationResult RunValidation(bool scanUsages, bool failOnWarnings)
        {
            EventChannelAuditResult auditResult = EventChannelAuditRunner.Run(scanUsages);

            bool hasErrors = auditResult.ErrorCount > 0;
            bool hasWarnings = auditResult.WarningCount > 0;
            bool passed = hasErrors == false && (failOnWarnings == false || hasWarnings == false);

            string message = BuildReportMessage(auditResult, scanUsages, failOnWarnings, passed);

            return new EventChannelCiValidationResult(passed, message);
        }

        private static string BuildReportMessage(
            EventChannelAuditResult auditResult,
            bool scanUsages,
            bool failOnWarnings,
            bool passed
        )
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine(passed
                ? "INLO Event CI Validation Passed"
                : "INLO Event CI Validation Failed");

            builder.AppendLine();
            builder.AppendLine("Options");
            builder.AppendLine($"- Scan Usages: {scanUsages}");
            builder.AppendLine($"- Fail On Warnings: {failOnWarnings}");
            builder.AppendLine();

            builder.AppendLine("Summary");
            builder.AppendLine($"- Channels: {auditResult.Channels.Count}");
            builder.AppendLine($"- Errors: {auditResult.ErrorCount}");
            builder.AppendLine($"- Warnings: {auditResult.WarningCount}");
            builder.AppendLine($"- Info: {auditResult.InfoCount}");
            builder.AppendLine();

            if (auditResult.Issues.Count == 0)
            {
                builder.AppendLine("No issues found.");
                return builder.ToString();
            }

            builder.AppendLine("Issues");

            int listedCount = Mathf.Min(MaxListedIssues, auditResult.Issues.Count);

            for (int i = 0; i < listedCount; i++)
            {
                EventChannelAuditIssue issue = auditResult.Issues[i];

                builder.AppendLine(
                    $"- [{issue.Severity}] {issue.Type} | {GetChannelDisplayName(issue.ChannelPath)}"
                );

                builder.AppendLine($"  Path: {issue.ChannelPath}");
                builder.AppendLine($"  Message: {issue.Message}");

                if (!string.IsNullOrWhiteSpace(issue.Recommendation))
                {
                    builder.AppendLine($"  Recommendation: {issue.Recommendation}");
                }
            }

            if (auditResult.Issues.Count > MaxListedIssues)
            {
                builder.AppendLine($"...and {auditResult.Issues.Count - MaxListedIssues} more issue(s).");
            }

            builder.AppendLine();
            builder.AppendLine("해결 방법");
            builder.AppendLine("1. Tools > INLO > Events > Event Audit Report를 엽니다.");
            builder.AppendLine("2. Run Audit을 실행합니다.");
            builder.AppendLine("3. Missing Description, Debug Log, Unused 후보 등을 확인합니다.");
            builder.AppendLine("4. 필요한 Channel asset을 수정한 뒤 다시 CI Validation을 실행합니다.");

            return builder.ToString();
        }

        private static string GetChannelDisplayName(string channelPath)
        {
            if (string.IsNullOrWhiteSpace(channelPath))
            {
                return "(Unknown Channel)";
            }

            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(channelPath);

            if (asset != null)
            {
                return asset.name;
            }

            return Path.GetFileNameWithoutExtension(channelPath);
        }

        private static bool ReadBoolCommandLineArg(string argName, bool defaultValue)
        {
            string[] args = System.Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] != argName)
                {
                    continue;
                }

                if (i + 1 >= args.Length)
                {
                    return defaultValue;
                }

                string value = args[i + 1].ToLowerInvariant();

                if (value == "true" || value == "1" || value == "yes")
                {
                    return true;
                }

                if (value == "false" || value == "0" || value == "no")
                {
                    return false;
                }

                return defaultValue;
            }

            return defaultValue;
        }

        private readonly struct EventChannelCiValidationResult
        {
            public bool Passed { get; }
            public string Message { get; }

            public EventChannelCiValidationResult(bool passed, string message)
            {
                Passed = passed;
                Message = message;
            }
        }
    }
}
