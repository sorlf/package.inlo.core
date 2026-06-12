using System.Collections.Generic;
using INLO.Core.Pooling;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using INLO.Core.EditorUI.Editor;

namespace INLO.Core.Pooling.Editor
{
    public sealed class PoolValidationPanel : VisualElement
    {
        private sealed class GroupReport
        {
            public PoolDatabaseGroup Group;
            public string Path;
            public int DatabaseCount;
            public int MissingDatabaseCount;
            public int DuplicateDatabaseCount;
            public int DuplicateKeyCount;
            public int ErrorCount;
            public int WarningCount;
        }

        private readonly PoolSystemManagerWindow _window;
        private TextField _searchField;
        private Toggle _errorsOnlyToggle;
        private Toggle _warningsOnlyToggle;
        private ScrollView _list;

        private VisualElement _dashboardRow;
        private VisualElement _totalGroupsCard;
        private VisualElement _invalidGroupsCard;
        private VisualElement _errorsCard;
        private VisualElement _warningsCard;

        private readonly List<GroupReport> _reports = new();
        private string _searchText = string.Empty;

        public PoolValidationPanel(PoolSystemManagerWindow window)
        {
            _window = window;
            BuildUI();
            RefreshReport();
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

            PopulateControlSection(sidebar);

            // 2. 우측 메인 워크스페이스
            VisualElement workspace = new();
            workspace.AddToClassList("inlo-workspace-right");
            body.Add(workspace);

            // 1단계 정보: 웅장한 가로 대시보드 행
            _dashboardRow = new VisualElement();
            _dashboardRow.AddToClassList("inlo-dashboard-row");
            workspace.Add(_dashboardRow);

            _totalGroupsCard = CreateDashboardCard("Total Groups", "0", "inlo-card--accent");
            _invalidGroupsCard = CreateDashboardCard("Invalid Groups", "0", "inlo-card--warning");
            _errorsCard = CreateDashboardCard("Total Errors", "0", "inlo-card--error");
            _warningsCard = CreateDashboardCard("Total Warnings", "0", "inlo-card--warning");

            _dashboardRow.Add(_totalGroupsCard);
            _dashboardRow.Add(_invalidGroupsCard);
            _dashboardRow.Add(_errorsCard);
            _dashboardRow.Add(_warningsCard);

            // 3단계 정보: 상세 검증 결과 목록 리포트
            VisualElement listCard = InloUIFactory.CreateCard();
            listCard.AddToClassList("inlo-card--grow");
            workspace.Add(listCard);

            listCard.Add(InloUIFactory.CreateSectionLabel("Integrity Issues & Violations"));

            _list = new ScrollView { name = "pool-validation-scroll-view" };
            _list.AddToClassList("inlo-list");
            _list.style.marginTop = 6;
            _list.style.flexGrow = 1;
            listCard.Add(_list);
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

        private void PopulateControlSection(VisualElement sidebar)
        {
            sidebar.Add(InloUIFactory.CreateSectionLabel("Validation Controls"));

            VisualElement buttonRow = InloUIFactory.CreateButtonRow();

            Button refreshBtn = InloUIFactory.CreateAccentButton("Scan & Refresh", RefreshReport);
            refreshBtn.style.flexGrow = 1;
            buttonRow.Add(refreshBtn);

            Button pingBtn = InloUIFactory.CreateDefaultButton("Ping First Bad", PingFirstInvalid);
            pingBtn.style.flexGrow = 1;
            buttonRow.Add(pingBtn);

            sidebar.Add(buttonRow);

            VisualElement divider = new();
            divider.style.height = 1;
            divider.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            divider.style.marginTop = 12;
            divider.style.marginBottom = 12;
            sidebar.Add(divider);

            sidebar.Add(InloUIFactory.CreateSectionLabel("Filters"));

            _searchField = new TextField("Search Group Name");
            _searchField.AddToClassList("inlo-field");
            _searchField.style.marginBottom = 8;
            _searchField.RegisterCallback<FocusOutEvent>(_ => {
                _searchText = _searchField.value ?? string.Empty;
                RebuildList();
            });
            sidebar.Add(_searchField);

            _errorsOnlyToggle = new Toggle("Errors Only");
            _errorsOnlyToggle.AddToClassList("inlo-field");
            _errorsOnlyToggle.style.marginBottom = 4;
            _errorsOnlyToggle.RegisterValueChangedCallback(_ => RebuildList());
            sidebar.Add(_errorsOnlyToggle);

            _warningsOnlyToggle = new Toggle("Warnings Only");
            _warningsOnlyToggle.AddToClassList("inlo-field");
            _warningsOnlyToggle.RegisterValueChangedCallback(_ => RebuildList());
            sidebar.Add(_warningsOnlyToggle);
        }

        public void RefreshReport()
        {
            _reports.Clear();
            string[] guids = AssetDatabase.FindAssets("t:PoolDatabaseGroup");
            if (guids != null)
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    PoolDatabaseGroup group = AssetDatabase.LoadAssetAtPath<PoolDatabaseGroup>(path);
                    if (group == null) continue;

                    _reports.Add(BuildReport(group, path));
                }
            }

            _reports.Sort(CompareReports);
            RebuildList();
        }

        private void RebuildList()
        {
            if (_list == null) return;
            _list.Clear();

            int totalErrors = 0;
            int totalWarnings = 0;
            int invalidGroups = 0;
            int visibleCount = 0;

            for (int i = 0; i < _reports.Count; i++)
            {
                totalErrors += _reports[i].ErrorCount;
                totalWarnings += _reports[i].WarningCount;

                if (_reports[i].ErrorCount > 0 || _reports[i].WarningCount > 0)
                {
                    invalidGroups++;
                }
            }

            // 대시보드 실시간 업데이트
            SetDashboardValue(_totalGroupsCard, _reports.Count.ToString());
            SetDashboardValue(_invalidGroupsCard, invalidGroups.ToString());
            SetDashboardValue(_errorsCard, totalErrors.ToString());
            SetDashboardValue(_warningsCard, totalWarnings.ToString());

            // 대시보드 카드 테두리 색상 분기
            _invalidGroupsCard.EnableInClassList("inlo-card--ok", invalidGroups == 0);
            _invalidGroupsCard.EnableInClassList("inlo-card--warning", invalidGroups > 0);
            _errorsCard.EnableInClassList("inlo-card--ok", totalErrors == 0);
            _errorsCard.EnableInClassList("inlo-card--error", totalErrors > 0);
            _warningsCard.EnableInClassList("inlo-card--ok", totalWarnings == 0);
            _warningsCard.EnableInClassList("inlo-card--warning", totalWarnings > 0);

            _list.Add(CreateHeader());

            for (int i = 0; i < _reports.Count; i++)
            {
                GroupReport report = _reports[i];
                if (!ShouldShow(report)) continue;

                _list.Add(CreateRow(report));
                visibleCount++;
            }

            if (visibleCount == 0)
            {
                _list.Add(CreateEmptyState("No pool validation records match the active filters."));
            }
        }

        private static GroupReport BuildReport(PoolDatabaseGroup group, string path)
        {
            GroupReport report = new()
            {
                Group = group,
                Path = path
            };

            IReadOnlyList<PoolDatabase> databases = group.Databases;
            if (databases == null || databases.Count == 0)
            {
                report.WarningCount++;
                return report;
            }

            HashSet<PoolDatabase> databaseSet = new();
            Dictionary<string, PoolDatabase> keyOwners = new();

            for (int i = 0; i < databases.Count; i++)
            {
                PoolDatabase database = databases[i];
                if (database == null)
                {
                    report.MissingDatabaseCount++;
                    report.ErrorCount++;
                    continue;
                }

                if (!databaseSet.Add(database))
                {
                    report.DuplicateDatabaseCount++;
                    report.WarningCount++;
                    continue;
                }

                report.DatabaseCount++;
                PoolDatabaseValidationResult databaseResult = PoolDatabaseValidator.Validate(database);

                for (int m = 0; m < databaseResult.Messages.Count; m++)
                {
                    PoolDatabaseValidationMessage message = databaseResult.Messages[m];
                    if (message.Severity == PoolDatabaseValidationSeverity.Error)
                        report.ErrorCount++;
                    else if (message.Severity == PoolDatabaseValidationSeverity.Warning)
                        report.WarningCount++;
                }

                foreach (PoolEntry entry in database.Entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.PoolKeyValue)) continue;

                    if (keyOwners.TryGetValue(entry.PoolKeyValue, out PoolDatabase owner))
                    {
                        if (owner != database)
                        {
                            report.DuplicateKeyCount++;
                            report.ErrorCount++;
                        }
                        continue;
                    }
                    keyOwners.Add(entry.PoolKeyValue, database);
                }
            }

            return report;
        }

        private bool ShouldShow(GroupReport report)
        {
            if (_errorsOnlyToggle != null && _errorsOnlyToggle.value && report.ErrorCount <= 0) return false;
            if (_warningsOnlyToggle != null && _warningsOnlyToggle.value && report.WarningCount <= 0) return false;

            if (string.IsNullOrWhiteSpace(_searchText)) return true;

            string search = _searchText.Trim().ToLowerInvariant();
            return (report.Group != null && report.Group.name.ToLowerInvariant().Contains(search)) ||
                   (!string.IsNullOrEmpty(report.Path) && report.Path.ToLowerInvariant().Contains(search));
        }

        private static VisualElement CreateHeader()
        {
            VisualElement row = CreateRowContainer(true);
            row.Add(CreateCell("Status", 75, true));
            row.Add(CreateCell("Pool Group Name", 200, true));
            row.Add(CreateCell("DBs", 45, true));
            row.Add(CreateCell("Missing", 65, true));
            row.Add(CreateCell("Dup DB", 65, true));
            row.Add(CreateCell("Dup Key", 65, true));
            row.Add(CreateCell("Errors", 65, true));
            row.Add(CreateCell("Warnings", 75, true));
            row.Add(CreateCell("Database Asset Path", 380, true));
            row.Add(CreateCell("Actions", 130, true));
            return row;
        }

        private VisualElement CreateRow(GroupReport report)
        {
            VisualElement row = CreateRowContainer(false);

            bool hasError = report.ErrorCount > 0;
            bool hasWarning = !hasError && report.WarningCount > 0;
            bool isOk = !hasError && !hasWarning;

            row.EnableInClassList("inlo-list-item--error", hasError);
            row.EnableInClassList("inlo-list-item--warning", hasWarning);
            row.EnableInClassList("inlo-list-item--ok", isOk);

            VisualElement statusCell = new();
            statusCell.AddToClassList("inlo-cell");
            statusCell.style.width = 75;

            string statusText = hasError ? "ERROR" : (hasWarning ? "WARN" : "OK");
            Label statusBadge = new(statusText);
            statusBadge.AddToClassList("inlo-badge");
            statusBadge.EnableInClassList("inlo-badge--error", hasError);
            statusBadge.EnableInClassList("inlo-badge--warning", hasWarning);
            statusBadge.EnableInClassList("inlo-badge--ok", isOk);
            statusCell.Add(statusBadge);
            row.Add(statusCell);

            row.Add(CreateCell(report.Group != null ? report.Group.name : "(Missing)", 200));
            row.Add(CreateCell(report.DatabaseCount.ToString(), 45));
            row.Add(CreateCell(report.MissingDatabaseCount.ToString(), 65));
            row.Add(CreateCell(report.DuplicateDatabaseCount.ToString(), 65));
            row.Add(CreateCell(report.DuplicateKeyCount.ToString(), 65));
            row.Add(CreateCell(report.ErrorCount.ToString(), 65));
            row.Add(CreateCell(report.WarningCount.ToString(), 75));
            row.Add(CreateCell(report.Path, 380));

            VisualElement actions = new();
            actions.AddToClassList("inlo-cell-actions");
            actions.style.width = 130;

            Button pingButton = new(() =>
            {
                if (report.Group == null) return;
                Selection.activeObject = report.Group;
                EditorGUIUtility.PingObject(report.Group);
            })
            {
                text = "Ping"
            };
            pingButton.AddToClassList("inlo-button");

            Button openButton = new(() =>
            {
                if (report.Group == null) return;
                // 메인 매니저 창에서 Browser 탭으로 탭 전환하고 선택 소스를 강제 주입해 줍니다.
                _window.SelectBrowserTabWithSource(report.Group);
            })
            {
                text = "Open"
            };
            openButton.AddToClassList("inlo-button");

            actions.Add(pingButton);
            actions.Add(openButton);
            row.Add(actions);

            return row;
        }

        private static VisualElement CreateRowContainer(bool isHeader)
        {
            VisualElement row = new();
            row.AddToClassList("inlo-list-item");
            if (isHeader) row.AddToClassList("inlo-list-item--header");
            return row;
        }

        private static Label CreateCell(string text, float width, bool bold = false)
        {
            Label label = new(text);
            label.AddToClassList("inlo-cell");
            label.style.width = width;
            if (bold) label.AddToClassList("inlo-cell--header");
            return label;
        }

        private static Label CreateEmptyState(string text)
        {
            Label label = new(text);
            label.AddToClassList("inlo-empty");
            return label;
        }

        private static int CompareReports(GroupReport a, GroupReport b)
        {
            int errorCompare = b.ErrorCount.CompareTo(a.ErrorCount);
            if (errorCompare != 0) return errorCompare;

            int warningCompare = b.WarningCount.CompareTo(a.WarningCount);
            if (warningCompare != 0) return warningCompare;

            return string.Compare(a.Path, b.Path, System.StringComparison.Ordinal);
        }

        private void PingFirstInvalid()
        {
            for (int i = 0; i < _reports.Count; i++)
            {
                GroupReport report = _reports[i];
                if (report.ErrorCount <= 0 && report.WarningCount <= 0) continue;
                if (report.Group == null) continue;

                Selection.activeObject = report.Group;
                EditorGUIUtility.PingObject(report.Group);
                return;
            }
        }
    }
}
