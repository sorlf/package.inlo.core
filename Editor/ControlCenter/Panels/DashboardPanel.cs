using UnityEditor;
using UnityEngine.UIElements;

namespace INLO.Core.EditorUI.Editor
{
    [InloControlCenterPanel("Dashboard", "d_PlayButton", "panel-dashboard", 1)]
    public sealed class DashboardPanel : IControlCenterPanel
    {
        private InloControlCenterWindow _window;
        private Button _dbBtnPool;
        private Button _dbBtnEvents;
        private Button _dbBtnDataTable;
        private Button _dbBtnAudit;

        private Label _poolCountLabel;
        private Label _poolGroupsLabel;
        private Label _eventsCountLabel;
        private Label _dtCountLabel;
        private Label _auditStatusLabel;

        public void Initialize(InloControlCenterWindow window, VisualElement root)
        {
            _window = window;

            _dbBtnPool = root.Q<Button>("db-btn-pool");
            _dbBtnEvents = root.Q<Button>("db-btn-events");
            _dbBtnDataTable = root.Q<Button>("db-btn-datatable");
            _dbBtnAudit = root.Q<Button>("db-btn-audit");

            _poolCountLabel = root.Q<Label>("db-kpi-pool-count");
            _poolGroupsLabel = root.Q<Label>("db-kpi-pool-groups");
            _eventsCountLabel = root.Q<Label>("db-kpi-events-count");
            _dtCountLabel = root.Q<Label>("db-kpi-dt-count");
            _auditStatusLabel = root.Q<Label>("db-kpi-audit-status");

            if (_dbBtnPool != null) _dbBtnPool.clicked += () => _window.SwitchToTab("panel-pool");
            if (_dbBtnEvents != null) _dbBtnEvents.clicked += () => _window.SwitchToTab("panel-events");
            if (_dbBtnDataTable != null) _dbBtnDataTable.clicked += () => _window.SwitchToTab("panel-datatable");
            if (_dbBtnAudit != null) _dbBtnAudit.clicked += () => 
            {
                _window.SwitchToTab("panel-control");
                _window.RunCodeConventionAudit();
            };

            UpdateUI();
        }

        public void OnPanelEnabled()
        {
            UpdateUI();
        }

        public void OnPanelDisabled() { }

        public void UpdateUI()
        {
            // Query counts dynamically across the project assets database
            int poolDatabases = AssetDatabase.FindAssets("t:PoolDatabase").Length;
            int poolGroups = AssetDatabase.FindAssets("t:PoolDatabaseGroup").Length;
            int eventChannels = AssetDatabase.FindAssets("t:EventChannelBaseSO").Length;
            int dataTables = AssetDatabase.FindAssets("t:DataTableAsset").Length;

            if (_poolCountLabel != null) _poolCountLabel.text = $"{poolDatabases} Databases";
            if (_poolGroupsLabel != null) _poolGroupsLabel.text = $"{poolGroups} Groups";
            if (_eventsCountLabel != null) _eventsCountLabel.text = $"{eventChannels} Channels";
            if (_dtCountLabel != null) _dtCountLabel.text = $"{dataTables} Sheets Loaded";

            // Status overview logic
            if (_auditStatusLabel != null)
            {
                _auditStatusLabel.text = "Systems Active";
            }
        }
    }
}
