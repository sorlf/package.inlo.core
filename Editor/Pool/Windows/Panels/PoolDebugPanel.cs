using System.Collections.Generic;
using INLO.Core.Pooling;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using INLO.Core.EditorUI.Editor;

namespace INLO.Core.Pooling.Editor
{
    public sealed class PoolDebugPanel : VisualElement
    {
        private enum SortMode
        {
            PoolKey,
            ActiveDescending,
            PeakDescending,
            TotalDescending,
            UsageDescending,
            Policy
        }

        private const float RefreshInterval = 0.5f;

        private readonly IPoolWindow _window;
        private Toggle _autoRefreshToggle;
        private Toggle _showOnlyActiveToggle;
        private Toggle _showOnlyFullToggle;
        private EnumField _sortField;
        private TextField _searchField;
        private Label _summaryLabel;
        private ScrollView _table;

        private double _nextRefreshTime;
        private bool _autoRefresh = true;
        private bool _showOnlyActive;
        private bool _showOnlyFull;
        private SortMode _sortMode = SortMode.PeakDescending;
        private string _searchText = string.Empty;
        private bool _isListening;

        public PoolDebugPanel(IPoolWindow window)
        {
            _window = window;
            BuildUI();
            Refresh();
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

            // 1. 좌측 사이드바 (330px)
            VisualElement sidebar = new();
            sidebar.AddToClassList("inlo-card");
            sidebar.AddToClassList("inlo-sidebar-left");
            sidebar.style.width = 330;
            body.Add(sidebar);

            PopulateControls(sidebar);

            // 2. 우측 메인 워크스페이스
            VisualElement workspace = new();
            workspace.AddToClassList("inlo-workspace-right");
            body.Add(workspace);

            // 우측 상단 실시간 요약 통계 카드
            VisualElement summaryCard = InloUIFactory.CreateCard();
            summaryCard.name = "pool-debug-summary";
            workspace.Add(summaryCard);
            PopulateSummary(summaryCard);

            // 우측 하단 라이브 인스턴스 모니터 테이블
            VisualElement tableCard = InloUIFactory.CreateCard();
            tableCard.AddToClassList("inlo-card--grow");
            workspace.Add(tableCard);

            tableCard.Add(InloUIFactory.CreateSectionLabel("Live Pool Allocation Matrix"));

            _table = new ScrollView { name = "pool-debug-table" };
            _table.AddToClassList("inlo-list");
            _table.style.marginTop = 6;
            _table.style.flexGrow = 1;
            tableCard.Add(_table);
        }

        private void PopulateControls(VisualElement sidebar)
        {
            sidebar.Add(InloUIFactory.CreateSectionLabel("Live Debug controls"));

            _autoRefreshToggle = new Toggle("Auto Live updates") { value = _autoRefresh };
            _autoRefreshToggle.AddToClassList("inlo-field");
            _autoRefreshToggle.style.marginBottom = 6;
            _autoRefreshToggle.RegisterValueChangedCallback(evt => _autoRefresh = evt.newValue);
            sidebar.Add(_autoRefreshToggle);

            VisualElement buttonRow1 = InloUIFactory.CreateButtonRow();
            Button manualRefreshBtn = InloUIFactory.CreateAccentButton("Manual refresh", Refresh);
            manualRefreshBtn.style.flexGrow = 1;
            buttonRow1.Add(manualRefreshBtn);
            sidebar.Add(buttonRow1);

            VisualElement buttonRow2 = InloUIFactory.CreateButtonRow();
            Button resetBtn = InloUIFactory.CreateDefaultButton("Reset Peaks", () =>
            {
                if (!EditorApplication.isPlaying) return;
                PoolManager.ResetAllPeaks();
                Refresh();
            });
            resetBtn.style.flexGrow = 1;

            Button cleanupBtn = InloUIFactory.CreateDefaultButton("GC Cleanup", () =>
            {
                if (!EditorApplication.isPlaying) return;
                PoolManager.CleanupInvalidReferences();
                Refresh();
            });
            cleanupBtn.style.flexGrow = 1;

            buttonRow2.Add(resetBtn);
            buttonRow2.Add(cleanupBtn);
            sidebar.Add(buttonRow2);

            VisualElement divider = new();
            divider.style.height = 1;
            divider.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            divider.style.marginTop = 12;
            divider.style.marginBottom = 12;
            sidebar.Add(divider);

            sidebar.Add(InloUIFactory.CreateSectionLabel("Filters & Sort"));

            _searchField = new TextField("Search Key/Prefab");
            _searchField.AddToClassList("inlo-field");
            _searchField.style.marginBottom = 8;
            _searchField.RegisterCallback<FocusOutEvent>(_ => {
                _searchText = _searchField.value ?? string.Empty;
                Refresh();
            });
            sidebar.Add(_searchField);

            _sortField = new EnumField("Sort Mode", _sortMode);
            _sortField.AddToClassList("inlo-field");
            _sortField.style.marginBottom = 8;
            _sortField.RegisterValueChangedCallback(evt =>
            {
                _sortMode = (SortMode)evt.newValue;
                Refresh();
            });
            sidebar.Add(_sortField);

            _showOnlyActiveToggle = new Toggle("Active Pools Only") { value = _showOnlyActive };
            _showOnlyActiveToggle.AddToClassList("inlo-field");
            _showOnlyActiveToggle.style.marginBottom = 4;
            _showOnlyActiveToggle.RegisterValueChangedCallback(evt =>
            {
                _showOnlyActive = evt.newValue;
                Refresh();
            });
            sidebar.Add(_showOnlyActiveToggle);

            _showOnlyFullToggle = new Toggle("Full Capacity Only") { value = _showOnlyFull };
            _showOnlyFullToggle.AddToClassList("inlo-field");
            _showOnlyFullToggle.RegisterValueChangedCallback(evt =>
            {
                _showOnlyFull = evt.newValue;
                Refresh();
            });
            sidebar.Add(_showOnlyFullToggle);
        }

        private void PopulateSummary(VisualElement summarySlot)
        {
            summarySlot.Add(InloUIFactory.CreateSectionLabel("Runtime Monitor Stats"));
            _summaryLabel = new Label("Waiting for Play Mode...");
            _summaryLabel.AddToClassList("inlo-summary-label");
            summarySlot.Add(_summaryLabel);
        }

        public void OnPanelEnabled()
        {
            if (!_isListening)
            {
                EditorApplication.update += OnEditorUpdate;
                _isListening = true;
            }
        }

        public void OnPanelDisabled()
        {
            if (_isListening)
            {
                EditorApplication.update -= OnEditorUpdate;
                _isListening = false;
            }
        }

        private void OnEditorUpdate()
        {
            if (!_autoRefresh) return;
            if (EditorApplication.timeSinceStartup < _nextRefreshTime) return;

            _nextRefreshTime = EditorApplication.timeSinceStartup + RefreshInterval;
            Refresh();
        }

        public void Refresh()
        {
            if (_summaryLabel == null || _table == null) return;

            IReadOnlyList<PoolStats> rawStats = PoolManager.GetAllStats();
            List<PoolStats> stats = FilterAndSort(rawStats);

            int poolCount = rawStats.Count;
            int visibleCount = stats.Count;
            int activeTotal = 0;
            int inactiveTotal = 0;
            int createdTotal = 0;
            int peakTotal = 0;
            int fullCount = 0;

            for (int i = 0; i < rawStats.Count; i++)
            {
                activeTotal += rawStats[i].ActiveCount;
                inactiveTotal += rawStats[i].InactiveCount;
                createdTotal += rawStats[i].TotalCount;
                peakTotal += rawStats[i].PeakActiveCount;

                if (rawStats[i].IsAtMax && rawStats[i].OverflowPolicy != PoolOverflowPolicy.Expand)
                {
                    fullCount++;
                }
            }

            _summaryLabel.text = EditorApplication.isPlaying
                ? $"Active Pools: {poolCount}  |  Visible: {visibleCount}  |  Active Instances: {activeTotal}  |  Inactive: {inactiveTotal}  |  Peak Active Sum: {peakTotal}  |  Saturated (Full): {fullCount}"
                : "플레이 모드 실행 후, 풀이 등록되거나 가동되면 실시간 사용량 메트릭이 여기에 표시됩니다.";

            RebuildTable(stats);
        }

        private List<PoolStats> FilterAndSort(IReadOnlyList<PoolStats> rawStats)
        {
            List<PoolStats> stats = new();
            for (int i = 0; i < rawStats.Count; i++)
            {
                PoolStats stat = rawStats[i];
                if (!ShouldShow(stat)) continue;
                stats.Add(stat);
            }
            stats.Sort(CompareStats);
            return stats;
        }

        private bool ShouldShow(PoolStats stat)
        {
            if (_showOnlyActive && stat.ActiveCount <= 0) return false;
            if (_showOnlyFull && !(stat.IsAtMax && stat.OverflowPolicy != PoolOverflowPolicy.Expand)) return false;

            if (string.IsNullOrWhiteSpace(_searchText)) return true;

            string search = _searchText.Trim().ToLowerInvariant();
            string key = stat.PoolKey.IsValid ? stat.PoolKey.ToString() : string.Empty;
            string prefab = stat.PrefabName ?? string.Empty;

            return key.ToLowerInvariant().Contains(search) ||
                   prefab.ToLowerInvariant().Contains(search) ||
                   stat.OverflowPolicy.ToString().ToLowerInvariant().Contains(search);
        }

        private int CompareStats(PoolStats a, PoolStats b)
        {
            switch (_sortMode)
            {
                case SortMode.ActiveDescending:
                    return b.ActiveCount.CompareTo(a.ActiveCount);
                case SortMode.PeakDescending:
                    return b.PeakActiveCount.CompareTo(a.PeakActiveCount);
                case SortMode.TotalDescending:
                    return b.TotalCount.CompareTo(a.TotalCount);
                case SortMode.UsageDescending:
                    return b.PeakUsageRatio.CompareTo(a.PeakUsageRatio);
                case SortMode.Policy:
                    return string.Compare(a.OverflowPolicy.ToString(), b.OverflowPolicy.ToString(), System.StringComparison.Ordinal);
                case SortMode.PoolKey:
                default:
                    return string.Compare(GetKeyText(a), GetKeyText(b), System.StringComparison.Ordinal);
            }
        }

        private void RebuildTable(IReadOnlyList<PoolStats> stats)
        {
            if (stats == null || stats.Count == 0)
            {
                _table.Clear();
                _table.Add(CreateEmptyState("실시간 가동 중인 오브젝트 풀이 없거나 조건과 매칭되지 않습니다."));
                return;
            }

            VisualElement firstChild = _table.childCount > 0 ? _table[0] : null;
            if (firstChild != null && firstChild.ClassListContains("inlo-empty"))
            {
                _table.Clear();
            }

            if (_table.childCount == 0)
            {
                _table.Add(CreateHeader());
            }

            // 행 재사용 풀링 기법: GC 가비지 유발 최소화
            int currentRowsCount = _table.childCount - 1;
            int neededRowsCount = stats.Count;

            for (int i = currentRowsCount; i < neededRowsCount; i++)
            {
                _table.Add(CreateRow(stats[i]));
            }

            for (int i = currentRowsCount - 1; i >= neededRowsCount; i--)
            {
                _table.RemoveAt(i + 1);
            }

            for (int i = 0; i < neededRowsCount; i++)
            {
                VisualElement row = _table[i + 1];
                UpdateRow(row, stats[i]);
            }
        }

        private static void UpdateRow(VisualElement row, PoolStats stat)
        {
            string keyText = GetKeyText(stat);
            string refCount = stat.PoolKey.IsValid ? PoolManager.GetRegistrationCount(stat.PoolKey).ToString() : "-";
            string status = GetLiveStatusText(stat);
            bool isOver = status == "OVER";
            bool isFull = status == "FULL";
            bool isActive = status == "ACTIVE";

            row.EnableInClassList("inlo-list-item--error", isOver);
            row.EnableInClassList("inlo-list-item--warning", isFull);
            row.EnableInClassList("inlo-list-item--ok", isActive);

            VisualElement statusCell = row[0];
            Label statusBadge = (Label)statusCell[0];
            statusBadge.text = status;
            statusBadge.EnableInClassList("inlo-badge--error", isOver);
            statusBadge.EnableInClassList("inlo-badge--warning", isFull);
            statusBadge.EnableInClassList("inlo-badge--ok", isActive);
            statusBadge.EnableInClassList("inlo-badge--info", !isOver && !isFull && !isActive);

            if (stat.IsAtMax)
            {
                if (statusCell.childCount < 2)
                {
                    Label maxedBadge = new("MAXED");
                    maxedBadge.AddToClassList("inlo-badge");
                    maxedBadge.AddToClassList("inlo-badge--info");
                    statusCell.Add(maxedBadge);
                }
            }
            else
            {
                if (statusCell.childCount >= 2)
                {
                    statusCell.RemoveAt(1);
                }
            }

            ((Label)row[1]).text = keyText;
            ((Label)row[2]).text = stat.PrefabName;
            ((Label)row[3]).text = stat.ActiveCount.ToString();
            ((Label)row[4]).text = stat.InactiveCount.ToString();
            ((Label)row[5]).text = stat.TotalCount.ToString();
            ((Label)row[6]).text = stat.MaxCount.ToString();
            ((Label)row[7]).text = stat.PeakActiveCount.ToString();
            ((Label)row[8]).text = GetPercentText(stat.PeakUsageRatio);
            ((Label)row[9]).text = stat.OverflowPolicy.ToString();
            ((Label)row[10]).text = refCount;
        }

        private static VisualElement CreateHeader()
        {
            VisualElement row = CreateRowContainer(true);
            row.Add(CreateCell("Live Status", 110, true));
            row.Add(CreateCell("PoolKey Value", 180, true));
            row.Add(CreateCell("Target Prefab", 160, true));
            row.Add(CreateCell("Active", 60, true));
            row.Add(CreateCell("Inactive", 70, true));
            row.Add(CreateCell("Total", 60, true));
            row.Add(CreateCell("Max", 60, true));
            row.Add(CreateCell("Peak", 60, true));
            row.Add(CreateCell("Peak %", 70, true));
            row.Add(CreateCell("Overflow Policy", 120, true));
            row.Add(CreateCell("Refs", 50, true));
            return row;
        }

        private static VisualElement CreateRow(PoolStats stat)
        {
            VisualElement row = CreateRowContainer(false);
            string keyText = GetKeyText(stat);
            string refCount = stat.PoolKey.IsValid ? PoolManager.GetRegistrationCount(stat.PoolKey).ToString() : "-";

            string status = GetLiveStatusText(stat);
            bool isOver = status == "OVER";
            bool isFull = status == "FULL";
            bool isActive = status == "ACTIVE";

            row.EnableInClassList("inlo-list-item--error", isOver);
            row.EnableInClassList("inlo-list-item--warning", isFull);
            row.EnableInClassList("inlo-list-item--ok", isActive);

            VisualElement statusCell = new();
            statusCell.AddToClassList("inlo-cell");
            statusCell.AddToClassList("inlo-cell--badges");
            statusCell.style.width = 110;

            Label statusBadge = new(status);
            statusBadge.AddToClassList("inlo-badge");
            statusBadge.EnableInClassList("inlo-badge--error", isOver);
            statusBadge.EnableInClassList("inlo-badge--warning", isFull);
            statusBadge.EnableInClassList("inlo-badge--ok", isActive);
            statusBadge.EnableInClassList("inlo-badge--info", !isOver && !isFull && !isActive);
            statusCell.Add(statusBadge);

            if (stat.IsAtMax)
            {
                Label maxedBadge = new("MAXED");
                maxedBadge.AddToClassList("inlo-badge");
                maxedBadge.AddToClassList("inlo-badge--info");
                statusCell.Add(maxedBadge);
            }
            row.Add(statusCell);

            row.Add(CreateCell(keyText, 180));
            row.Add(CreateCell(stat.PrefabName, 160));
            row.Add(CreateCell(stat.ActiveCount.ToString(), 60));
            row.Add(CreateCell(stat.InactiveCount.ToString(), 70));
            row.Add(CreateCell(stat.TotalCount.ToString(), 60));
            row.Add(CreateCell(stat.MaxCount.ToString(), 60));
            row.Add(CreateCell(stat.PeakActiveCount.ToString(), 60));
            row.Add(CreateCell(GetPercentText(stat.PeakUsageRatio), 70));
            row.Add(CreateCell(stat.OverflowPolicy.ToString(), 120));
            row.Add(CreateCell(refCount, 50));

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

        private static string GetKeyText(PoolStats stat)
        {
            return stat.PoolKey.IsValid ? stat.PoolKey.ToString() : "(Prefab Direct)";
        }

        private static string GetLiveStatusText(PoolStats stat)
        {
            if (stat.MaxCount > 0 && stat.ActiveCount > stat.MaxCount) return "OVER";
            if (stat.MaxCount > 0 && stat.ActiveCount == stat.MaxCount) return "FULL";
            if (stat.ActiveCount > 0) return "ACTIVE";
            return "READY";
        }

        private static string GetPercentText(float ratio)
        {
            return $"{Mathf.RoundToInt(ratio * 100f)}%";
        }

        private static Label CreateEmptyState(string text)
        {
            Label label = new(text);
            label.AddToClassList("inlo-empty");
            return label;
        }
    }
}
