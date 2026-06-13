using UnityEngine.UIElements;
using INLO.Core.Pooling.Editor;

namespace INLO.Core.EditorUI.Editor
{
    [InloControlCenterPanel("Pool Manager", "d_PreMatCube", "panel-pool", 2)]
    public sealed class PoolManagerPanel : IControlCenterPanel
    {
        private enum PoolTab { Browser, Validation, Debug }

        private InloControlCenterWindow _window;
        private PoolTab _activeTab = PoolTab.Browser;

        private Button _tabBrowserBtn;
        private Button _tabValidationBtn;
        private Button _tabDebugBtn;
        private VisualElement _panelSlot;

        private PoolBrowserPanel _browserPanel;
        private PoolValidationPanel _validationPanel;
        private PoolDebugPanel _debugPanel;

        public void Initialize(InloControlCenterWindow window, VisualElement root)
        {
            _window = window;

            _tabBrowserBtn = root.Q<Button>("pool-tab-browser");
            _tabValidationBtn = root.Q<Button>("pool-tab-validation");
            _tabDebugBtn = root.Q<Button>("pool-tab-debug");
            _panelSlot = root.Q<VisualElement>("pool-panel-slot");

            if (_tabBrowserBtn != null) _tabBrowserBtn.clicked += () => SwitchTab(PoolTab.Browser);
            if (_tabValidationBtn != null) _tabValidationBtn.clicked += () => SwitchTab(PoolTab.Validation);
            if (_tabDebugBtn != null) _tabDebugBtn.clicked += () => SwitchTab(PoolTab.Debug);

            _browserPanel ??= new PoolBrowserPanel(_window);
            _validationPanel ??= new PoolValidationPanel(_window);
            _debugPanel ??= new PoolDebugPanel(_window);

            // Restore state via SessionState
            _activeTab = (PoolTab)UnityEditor.SessionState.GetInt("INLO_CC_Pool_ActiveTab", (int)PoolTab.Browser);

            SwitchTab(_activeTab);
        }

        public void OnPanelEnabled()
        {
            if (_activeTab == PoolTab.Debug)
            {
                _debugPanel?.OnPanelEnabled();
            }
        }

        public void OnPanelDisabled()
        {
            _debugPanel?.OnPanelDisabled();
        }

        public void UpdateUI()
        {
            SetSubTabStyle(_tabBrowserBtn, _activeTab == PoolTab.Browser);
            SetSubTabStyle(_tabValidationBtn, _activeTab == PoolTab.Validation);
            SetSubTabStyle(_tabDebugBtn, _activeTab == PoolTab.Debug);
        }

        public void SelectBrowserTabWithSource(UnityEngine.Object source)
        {
            if (_browserPanel == null) _browserPanel = new PoolBrowserPanel(_window);
            _browserPanel.SetSource(source);
            SwitchTab(PoolTab.Browser);
        }

        private void SwitchTab(PoolTab tab)
        {
            _activeTab = tab;
            UnityEditor.SessionState.SetInt("INLO_CC_Pool_ActiveTab", (int)tab);
            if (_panelSlot == null) return;

            _panelSlot.Clear();
            if (_debugPanel != null) _debugPanel.OnPanelDisabled();

            switch (tab)
            {
                case PoolTab.Browser:
                    _panelSlot.Add(_browserPanel);
                    _browserPanel.RefreshSourceField();
                    _browserPanel.Refresh();
                    break;
                case PoolTab.Validation:
                    _panelSlot.Add(_validationPanel);
                    _validationPanel.RefreshReport();
                    break;
                case PoolTab.Debug:
                    _panelSlot.Add(_debugPanel);
                    _debugPanel.OnPanelEnabled();
                    _debugPanel.Refresh();
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
