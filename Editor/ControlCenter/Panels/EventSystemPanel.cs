using UnityEngine.UIElements;
using INLO.Core.Events.Editor;

namespace INLO.Core.EditorUI.Editor
{
    [InloControlCenterPanel("Event System", "d_Profiler.Audio", "panel-events", 3)]
    public sealed class EventSystemPanel : IControlCenterPanel
    {
        private enum EventsTab { Browser, Audit, Graph }

        private InloControlCenterWindow _window;
        private EventsTab _activeTab = EventsTab.Browser;

        private Button _tabBrowserBtn;
        private Button _tabAuditBtn;
        private Button _tabGraphBtn;
        private VisualElement _panelSlot;

        private EventBrowserPanel _browserPanel;
        private EventAuditPanel _auditPanel;
        private EventGraphPanel _graphPanel;

        public void Initialize(InloControlCenterWindow window, VisualElement root)
        {
            _window = window;

            _tabBrowserBtn = root.Q<Button>("events-tab-browser");
            _tabAuditBtn = root.Q<Button>("events-tab-audit");
            _tabGraphBtn = root.Q<Button>("events-tab-graph");
            _panelSlot = root.Q<VisualElement>("events-panel-slot");

            if (_tabBrowserBtn != null) _tabBrowserBtn.clicked += () => SwitchTab(EventsTab.Browser);
            if (_tabAuditBtn != null) _tabAuditBtn.clicked += () => SwitchTab(EventsTab.Audit);
            if (_tabGraphBtn != null) _tabGraphBtn.clicked += () => SwitchTab(EventsTab.Graph);

            _browserPanel ??= new EventBrowserPanel();
            _auditPanel ??= new EventAuditPanel();
            _graphPanel ??= new EventGraphPanel();

            // Restore state via SessionState
            _activeTab = (EventsTab)UnityEditor.SessionState.GetInt("INLO_CC_Events_ActiveTab", (int)EventsTab.Browser);

            SwitchTab(_activeTab);
        }

        public void OnPanelEnabled() { }
        public void OnPanelDisabled() { }

        public void UpdateUI()
        {
            SetSubTabStyle(_tabBrowserBtn, _activeTab == EventsTab.Browser);
            SetSubTabStyle(_tabAuditBtn, _activeTab == EventsTab.Audit);
            SetSubTabStyle(_tabGraphBtn, _activeTab == EventsTab.Graph);
        }

        private void SwitchTab(EventsTab tab)
        {
            _activeTab = tab;
            UnityEditor.SessionState.SetInt("INLO_CC_Events_ActiveTab", (int)tab);
            if (_panelSlot == null) return;

            _panelSlot.Clear();

            switch (tab)
            {
                case EventsTab.Browser:
                    _panelSlot.Add(_browserPanel);
                    _browserPanel.RefreshUI();
                    break;
                case EventsTab.Audit:
                    _panelSlot.Add(_auditPanel);
                    _auditPanel.RefreshUI();
                    break;
                case EventsTab.Graph:
                    _panelSlot.Add(_graphPanel);
                    _graphPanel.RefreshUI();
                    break;
            }

            UpdateUI();
        }

        private static void SetSubTabStyle(Button button, bool selected)
        {
            if (button == null) return;
            button.EnableInClassList("inlo-tab-button--selected", selected);
            button.EnableInClassList("inlo-tab-button--idle", !selected);
        }
    }
}
