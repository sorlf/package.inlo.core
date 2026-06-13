using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using INLO.Core.EditorUI.Editor;

namespace INLO.Core.Events.Editor
{
    public sealed class EventSystemManagerWindow : InloBaseEditorWindow
    {
        protected override string UxmlPath => WindowUxmlPath;
        protected override string UssPath => CommonUssPath;
        protected override string MainScrollViewName => string.Empty; // 탭 스왑 방식

        private const string CommonUssPath =
            "Packages/com.inlo.core/Editor/EditorUI/USS/InloWindowCommon.uss";

        private const string WindowUxmlPath =
            "Packages/com.inlo.core/Editor/Events/Windows/UXML/EventSystemManagerWindow.uxml";

        private enum TabType
        {
            Browser,
            Audit,
            Graph
        }

        private Button _tabBrowserButton;
        private Button _tabAuditButton;
        private Button _tabGraphButton;
        private VisualElement _contentSlot;

        private EventBrowserPanel _browserPanel;
        private EventAuditPanel _auditPanel;
        private EventGraphPanel _graphPanel;

        private TabType _activeTab = TabType.Browser;

        [MenuItem("Tools/INLO/Events/Event System Manager")]
        public static void Open()
        {
            EventSystemManagerWindow window = GetWindow<EventSystemManagerWindow>("Event System Manager");
            window.minSize = new Vector2(1040, 680);
            window.Show();
        }

        protected override void OnBindElements()
        {
            _tabBrowserButton = rootVisualElement.Q<Button>("tab-browser");
            _tabAuditButton = rootVisualElement.Q<Button>("tab-audit");
            _tabGraphButton = rootVisualElement.Q<Button>("tab-graph");
            _contentSlot = rootVisualElement.Q<VisualElement>("manager-content-slot");

            if (_tabBrowserButton != null) _tabBrowserButton.clicked += () => SwitchTab(TabType.Browser);
            if (_tabAuditButton != null) _tabAuditButton.clicked += () => SwitchTab(TabType.Audit);
            if (_tabGraphButton != null) _tabGraphButton.clicked += () => SwitchTab(TabType.Graph);

            // Lazy loading 패널 인스턴스화
            if (_browserPanel == null) _browserPanel = new EventBrowserPanel();
            if (_auditPanel == null) _auditPanel = new EventAuditPanel();
            if (_graphPanel == null) _graphPanel = new EventGraphPanel();

            SwitchTab(_activeTab);
        }

        public override void UpdateUI()
        {
            RefreshActiveTab();
        }

        private void SwitchTab(TabType tab)
        {
            _activeTab = tab;
            if (_contentSlot == null) return;
            _contentSlot.Clear();

            UpdateTabStyles();

            switch (tab)
            {
                case TabType.Browser:
                    _contentSlot.Add(_browserPanel);
                    _browserPanel.RefreshUI();
                    break;

                case TabType.Audit:
                    _contentSlot.Add(_auditPanel);
                    _auditPanel.RefreshUI();
                    break;

                case TabType.Graph:
                    _contentSlot.Add(_graphPanel);
                    _graphPanel.RefreshUI();
                    break;
            }
        }

        private void UpdateTabStyles()
        {
            SetTabButtonStyle(_tabBrowserButton, _activeTab == TabType.Browser);
            SetTabButtonStyle(_tabAuditButton, _activeTab == TabType.Audit);
            SetTabButtonStyle(_tabGraphButton, _activeTab == TabType.Graph);
        }

        private static void SetTabButtonStyle(Button button, bool selected)
        {
            if (button == null) return;
            button.EnableInClassList("inlo-tab-button--selected", selected);
            button.EnableInClassList("inlo-tab-button--idle", !selected);
        }

        private void RefreshActiveTab()
        {
            switch (_activeTab)
            {
                case TabType.Browser:
                    if (_browserPanel != null) _browserPanel.RefreshUI();
                    break;
                case TabType.Audit:
                    if (_auditPanel != null) _auditPanel.RefreshUI();
                    break;
                case TabType.Graph:
                    if (_graphPanel != null) _graphPanel.RefreshUI();
                    break;
            }
        }
    }
}
