using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using INLO.Core.EditorUI.Editor;

namespace INLO.Core.Editor.Events
{
    public sealed class EventAuditPanel : VisualElement
    {
        private EventChannelAuditResult auditResult;
        private string searchText = string.Empty;

        private bool scanUsages = true;
        private bool showInfo = true;
        private bool showWarnings = true;
        private bool showErrors = true;

        private Toggle scanUsagesToggle;
        private Toggle showErrorsToggle;
        private Toggle showWarningsToggle;
        private Toggle showInfoToggle;
        private TextField searchField;

        private VisualElement summaryContainer;
        private ScrollView issuesScrollView;
        private Label issueCountLabel;

        private VisualElement _totalChannelsCard;
        private VisualElement _issuesCard;
        private VisualElement _errorsCard;
        private VisualElement _warningsCard;
        private VisualElement _infoCard;

        public EventAuditPanel()
        {
            BuildUI();
            RefreshSummary();
            RefreshIssues();
        }

        public void RefreshUI()
        {
            RefreshSummary();
            RefreshIssues();
        }

        private void BuildUI()
        {
            style.flexGrow = 1;
            style.flexDirection = FlexDirection.Column;

            // F-패턴 가로 분할 스플릿
            VisualElement body = new();
            body.AddToClassList("inlo-split-container");
            body.style.flexGrow = 1;
            Add(body);

            // 1. 좌측 컨트롤 사이드바 (330px)
            VisualElement sidebar = new();
            sidebar.AddToClassList("inlo-card");
            sidebar.AddToClassList("inlo-sidebar-left");
            sidebar.style.width = 330;
            body.Add(sidebar);

            PopulateSidebar(sidebar);

            // 2. 우측 메인 워크스페이스
            VisualElement workspace = new();
            workspace.AddToClassList("inlo-workspace-right");
            body.Add(workspace);

            // 1단계 정보: 웅장한 가로 대시보드 행
            summaryContainer = new VisualElement();
            summaryContainer.AddToClassList("inlo-dashboard-row");
            workspace.Add(summaryContainer);

            _totalChannelsCard = CreateDashboardCard("Channels", "0", "inlo-card--accent");
            _issuesCard = CreateDashboardCard("Issues", "0", "inlo-card--warning");
            _errorsCard = CreateDashboardCard("Errors", "0", "inlo-card--error");
            _warningsCard = CreateDashboardCard("Warnings", "0", "inlo-card--warning");
            _infoCard = CreateDashboardCard("Info", "0", "inlo-card--info");

            summaryContainer.Add(_totalChannelsCard);
            summaryContainer.Add(_issuesCard);
            summaryContainer.Add(_errorsCard);
            summaryContainer.Add(_warningsCard);
            summaryContainer.Add(_infoCard);

            // 3단계 정보: 이슈 상세 리포트 카드
            VisualElement issuesCard = InloUIFactory.CreateCard();
            issuesCard.AddToClassList("inlo-card--grow");
            workspace.Add(issuesCard);

            VisualElement headerRow = new();
            headerRow.AddToClassList("inlo-row");

            Label title = InloUIFactory.CreateSectionLabel("Audited Diagnostics");
            title.style.flexGrow = 1;
            title.style.marginBottom = 0;
            headerRow.Add(title);

            issueCountLabel = new Label { name = "IssueCountLabel" };
            issueCountLabel.AddToClassList("inlo-muted");
            headerRow.Add(issueCountLabel);
            issuesCard.Add(headerRow);

            issuesScrollView = new ScrollView { name = "audit-scroll-view" };
            issuesScrollView.AddToClassList("inlo-list");
            issuesScrollView.style.marginTop = 6;
            issuesScrollView.style.flexGrow = 1;
            issuesCard.Add(issuesScrollView);
        }

        private VisualElement CreateDashboardCard(string label, string value, string statusClass)
        {
            VisualElement card = new();
            card.AddToClassList("inlo-card");
            card.AddToClassList("inlo-dashboard-card");
            if (!string.IsNullOrEmpty(statusClass))
            {
                card.AddToClassList(statusClass);
            }

            Label valLabel = new(value);
            valLabel.name = "value-label";
            valLabel.AddToClassList("inlo-metric-value");
            card.Add(valLabel);

            Label lblText = new(label);
            lblText.AddToClassList("inlo-muted");
            card.Add(lblText);

            return card;
        }

        private void SetDashboardValue(VisualElement card, string value)
        {
            Label val = card.Q<Label>("value-label");
            if (val != null) val.text = value;
        }

        private void PopulateSidebar(VisualElement sidebar)
        {
            sidebar.Add(InloUIFactory.CreateSectionLabel("Audit Runner"));

            scanUsagesToggle = new Toggle("Scan Scene/Prefab usages") { value = scanUsages };
            scanUsagesToggle.RegisterValueChangedCallback(evt => scanUsages = evt.newValue);
            sidebar.Add(scanUsagesToggle);

            VisualElement buttonRow = InloUIFactory.CreateButtonRow();
            buttonRow.style.marginTop = 6;

            Button runButton = InloUIFactory.CreateAccentButton("Run Diagnostics", RunAudit);
            runButton.style.flexGrow = 1;
            buttonRow.Add(runButton);

            Button clearButton = InloUIFactory.CreateDefaultButton("Reset", () =>
            {
                auditResult = null;
                RefreshSummary();
                RefreshIssues();
            });
            clearButton.style.flexGrow = 1;
            buttonRow.Add(clearButton);

            sidebar.Add(buttonRow);

            VisualElement divider = new();
            divider.style.height = 1;
            divider.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            divider.style.marginTop = 12;
            divider.style.marginBottom = 12;
            sidebar.Add(divider);

            sidebar.Add(InloUIFactory.CreateSectionLabel("Filters"));

            // 11px 작은 메타 안내 라벨 얹기
            Label searchDesc = new("Search Diagnostic Issue Key:") { style = { fontSize = 11, marginBottom = 4 } };
            searchDesc.AddToClassList("inlo-muted");
            sidebar.Add(searchDesc);

            // 라벨을 걷어내어 100% 가로폭 사용
            searchField = new TextField();
            searchField.AddToClassList("inlo-field");
            searchField.style.marginBottom = 8;
            searchField.RegisterCallback<FocusOutEvent>(_ =>
            {
                searchText = searchField.value ?? string.Empty;
                RefreshIssues();
            });
            sidebar.Add(searchField);

            Label filterTitle = InloUIFactory.CreateSectionLabel("Diagnostics Severity");
            filterTitle.style.marginTop = 6;
            sidebar.Add(filterTitle);

            showErrorsToggle = new Toggle("Errors Only") { value = showErrors };
            showErrorsToggle.RegisterValueChangedCallback(evt => { showErrors = evt.newValue; RefreshIssues(); });
            sidebar.Add(showErrorsToggle);

            showWarningsToggle = new Toggle("Warnings Only") { value = showWarnings };
            showWarningsToggle.RegisterValueChangedCallback(evt => { showWarnings = evt.newValue; RefreshIssues(); });
            sidebar.Add(showWarningsToggle);

            showInfoToggle = new Toggle("Info Messages") { value = showInfo };
            showInfoToggle.RegisterValueChangedCallback(evt => { showInfo = evt.newValue; RefreshIssues(); });
            sidebar.Add(showInfoToggle);
        }

        private void RefreshSummary()
        {
            if (auditResult == null)
            {
                SetDashboardValue(_totalChannelsCard, "0");
                SetDashboardValue(_issuesCard, "0");
                SetDashboardValue(_errorsCard, "0");
                SetDashboardValue(_warningsCard, "0");
                SetDashboardValue(_infoCard, "0");

                _issuesCard.EnableInClassList("inlo-card--warning", false);
                _errorsCard.EnableInClassList("inlo-card--error", false);
                _warningsCard.EnableInClassList("inlo-card--warning", false);
                return;
            }

            SetDashboardValue(_totalChannelsCard, auditResult.Channels.Count.ToString());
            SetDashboardValue(_issuesCard, auditResult.Issues.Count.ToString());
            SetDashboardValue(_errorsCard, auditResult.ErrorCount.ToString());
            SetDashboardValue(_warningsCard, auditResult.WarningCount.ToString());
            SetDashboardValue(_infoCard, auditResult.InfoCount.ToString());

            _issuesCard.EnableInClassList("inlo-card--warning", auditResult.Issues.Count > 0);
            _errorsCard.EnableInClassList("inlo-card--error", auditResult.ErrorCount > 0);
            _warningsCard.EnableInClassList("inlo-card--warning", auditResult.WarningCount > 0);
        }

        private void RefreshIssues()
        {
            if (issuesScrollView == null) return;
            issuesScrollView.Clear();

            if (auditResult == null)
            {
                if (issueCountLabel != null) issueCountLabel.text = "";
                issuesScrollView.Add(CreateMessageCard("No audit execution found.", "Click 'Run Diagnostics' to analyze current event channels and find validation issues."));
                return;
            }

            List<EventChannelAuditIssue> filteredIssues = GetFilteredIssues();
            if (issueCountLabel != null)
            {
                issueCountLabel.text = $"Visible: {filteredIssues.Count}  |  Total: {auditResult.Issues.Count}";
            }

            if (filteredIssues.Count == 0)
            {
                issuesScrollView.Add(CreateMessageCard("No issues match active filters.", "Use the severity checkbox toggles or adjust the search keyword in the Left Sidebar."));
                return;
            }

            foreach (EventChannelAuditIssue issue in filteredIssues)
            {
                issuesScrollView.Add(CreateIssueCard(issue));
            }
        }

        private List<EventChannelAuditIssue> GetFilteredIssues()
        {
            List<EventChannelAuditIssue> result = new();
            if (auditResult == null) return result;

            foreach (EventChannelAuditIssue issue in auditResult.Issues)
            {
                if (issue.Severity == EventChannelAuditIssueSeverity.Error && !showErrors) continue;
                if (issue.Severity == EventChannelAuditIssueSeverity.Warning && !showWarnings) continue;
                if (issue.Severity == EventChannelAuditIssueSeverity.Info && !showInfo) continue;

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    string lowerSearch = searchText.ToLowerInvariant();
                    bool matchesChannel = issue.Channel != null && issue.Channel.Asset != null && issue.Channel.Asset.name.ToLowerInvariant().Contains(lowerSearch);
                    bool matchesPath = issue.Channel != null && issue.Channel.Path.ToLowerInvariant().Contains(lowerSearch);
                    bool matchesMessage = !string.IsNullOrEmpty(issue.Message) && issue.Message.ToLowerInvariant().Contains(lowerSearch);
                    bool matchesType = issue.Type.ToString().ToLowerInvariant().Contains(lowerSearch);
                    bool matchesChannelName = !string.IsNullOrEmpty(issue.ChannelName) && issue.ChannelName.ToLowerInvariant().Contains(lowerSearch);

                    if (!matchesChannel && !matchesPath && !matchesMessage && !matchesType && !matchesChannelName) continue;
                }

                result.Add(issue);
            }

            return result;
        }

        private VisualElement CreateIssueCard(EventChannelAuditIssue issue)
        {
            VisualElement card = new();
            card.AddToClassList("inlo-card");
            card.AddToClassList(SeverityCardClass(issue.Severity));

            VisualElement topRow = new();
            topRow.AddToClassList("inlo-row");
            card.Add(topRow);

            topRow.Add(CreateBadge(issue.Severity.ToString(), SeverityBadgeClass(issue.Severity)));
            topRow.Add(CreateBadge(issue.Type.ToString(), "inlo-badge--info"));

            Label channelLabel = new(GetChannelDisplayName(issue));
            channelLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            channelLabel.style.marginLeft = 8;
            channelLabel.style.flexGrow = 1;
            topRow.Add(channelLabel);

            Button selectButton = InloUIFactory.CreateDefaultButton("Select", () =>
            {
                UnityEngine.Object target = GetChannelAsset(issue);
                if (target != null) Selection.activeObject = target;
            });
            Button pingButton = InloUIFactory.CreateDefaultButton("Ping", () =>
            {
                UnityEngine.Object target = GetChannelAsset(issue);
                if (target != null) EditorGUIUtility.PingObject(target);
            });

            topRow.Add(selectButton);
            topRow.Add(pingButton);

            Label pathLabel = new(GetChannelPath(issue));
            pathLabel.AddToClassList("inlo-muted");
            pathLabel.style.marginTop = 4;
            pathLabel.style.fontSize = 10;
            card.Add(pathLabel);

            HelpBox msg = new(issue.Message, ToHelpBoxMessageType(issue.Severity));
            msg.style.marginTop = 6;
            card.Add(msg);

            if (!string.IsNullOrWhiteSpace(issue.Recommendation))
            {
                VisualElement recCard = InloUIFactory.CreateCard();
                recCard.AddToClassList("inlo-card--info"); // 파란색 왼쪽 테두리 포인트 규격화
                recCard.style.marginTop = 6;
                card.Add(recCard);

                Label recTitle = new("Recommendation Advice");
                recTitle.AddToClassList("inlo-card-title");
                recCard.Add(recTitle);

                Label recTxt = new(issue.Recommendation);
                recTxt.AddToClassList("inlo-muted");
                recTxt.AddToClassList("inlo-wrap");
                recCard.Add(recTxt);
            }

            return card;
        }

        private static VisualElement CreateMessageCard(string titleText, string bodyText)
        {
            VisualElement card = InloUIFactory.CreateCard();
            Label t = new(titleText);
            t.AddToClassList("inlo-card-title");
            card.Add(t);

            Label b = new(bodyText);
            b.AddToClassList("inlo-muted");
            b.AddToClassList("inlo-wrap");
            card.Add(b);
            return card;
        }

        private string GetChannelDisplayName(EventChannelAuditIssue issue)
        {
            if (issue.Channel != null && issue.Channel.Asset != null) return issue.Channel.Asset.name;
            return !string.IsNullOrWhiteSpace(issue.ChannelName) ? issue.ChannelName : "(Unknown Channel)";
        }

        private string GetChannelPath(EventChannelAuditIssue issue)
        {
            if (issue.Channel != null && !string.IsNullOrWhiteSpace(issue.Channel.Path)) return issue.Channel.Path;
            return !string.IsNullOrWhiteSpace(issue.ChannelPath) ? issue.ChannelPath : "(No Asset Path)";
        }

        private UnityEngine.Object GetChannelAsset(EventChannelAuditIssue issue)
        {
            if (issue.Channel != null && issue.Channel.Asset != null) return issue.Channel.Asset;
            string path = GetChannelPath(issue);
            if (string.IsNullOrWhiteSpace(path) || path == "(No Asset Path)") return null;
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        }

        private static string SeverityBadgeClass(EventChannelAuditIssueSeverity severity)
        {
            return severity switch
            {
                EventChannelAuditIssueSeverity.Error => "inlo-badge--error",
                EventChannelAuditIssueSeverity.Warning => "inlo-badge--warning",
                _ => "inlo-badge--info"
            };
        }

        private static string SeverityCardClass(EventChannelAuditIssueSeverity severity)
        {
            return severity switch
            {
                EventChannelAuditIssueSeverity.Error => "inlo-card--error",
                EventChannelAuditIssueSeverity.Warning => "inlo-card--warning",
                _ => "inlo-card--info"
            };
        }

        private HelpBoxMessageType ToHelpBoxMessageType(EventChannelAuditIssueSeverity severity)
        {
            return severity switch
            {
                EventChannelAuditIssueSeverity.Error => HelpBoxMessageType.Error,
                EventChannelAuditIssueSeverity.Warning => HelpBoxMessageType.Warning,
                _ => HelpBoxMessageType.Info
            };
        }

        private void RunAudit()
        {
            auditResult = EventChannelAuditRunner.Run(scanUsages);
            Debug.Log($"Event System audit finished. Validated {auditResult.Channels.Count} channels. Flagged {auditResult.Issues.Count} issues.");
            RefreshSummary();
            RefreshIssues();
        }

        private static Label CreateBadge(string text, string modifier)
        {
            Label b = new(text);
            b.AddToClassList("inlo-badge");
            if (!string.IsNullOrEmpty(modifier))
            {
                b.AddToClassList(modifier);
            }
            return b;
        }
    }
}
