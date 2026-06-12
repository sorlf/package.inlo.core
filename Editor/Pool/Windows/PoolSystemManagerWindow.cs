using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using INLO.Core.EditorUI.Editor;

namespace INLO.Core.Pooling.Editor
{
    public sealed class PoolSystemManagerWindow : InloBaseEditorWindow
    {
        protected override string UxmlPath => WindowUxmlPath;
        protected override string UssPath => CommonUssPath;
        protected override string MainScrollViewName => string.Empty; // 탭 스왑식이므로 고정 뷰 네임 없음

        private const string CommonUssPath =
            "Packages/com.inlo.core/Editor/EditorUI/USS/InloWindowCommon.uss";

        private const string WindowUxmlPath =
            "Packages/com.inlo.core/Editor/Pool/Windows/UXML/PoolSystemManagerWindow.uxml";

        private enum TabType
        {
            Browser,
            Validation,
            Debug
        }

        private Button _tabBrowserButton;
        private Button _tabValidationButton;
        private Button _tabDebugButton;
        private VisualElement _contentSlot;

        private PoolBrowserPanel _browserPanel;
        private PoolValidationPanel _validationPanel;
        private PoolDebugPanel _debugPanel;

        private TabType _activeTab = TabType.Browser;

        [MenuItem("Tools/INLO/Pooling/Pool System Manager")]
        public static void Open()
        {
            PoolSystemManagerWindow window = GetWindow<PoolSystemManagerWindow>("Pool System Manager");
            window.minSize = new Vector2(1040, 680);
            window.Show();
        }

        protected override void OnBindElements()
        {
            _tabBrowserButton = rootVisualElement.Q<Button>("tab-browser");
            _tabValidationButton = rootVisualElement.Q<Button>("tab-validation");
            _tabDebugButton = rootVisualElement.Q<Button>("tab-debug");
            _contentSlot = rootVisualElement.Q<VisualElement>("manager-content-slot");

            if (_tabBrowserButton != null) _tabBrowserButton.clicked += () => SwitchTab(TabType.Browser);
            if (_tabValidationButton != null) _tabValidationButton.clicked += () => SwitchTab(TabType.Validation);
            if (_tabDebugButton != null) _tabDebugButton.clicked += () => SwitchTab(TabType.Debug);

            // Lazy loading 패널 캐싱 초기화
            if (_browserPanel == null) _browserPanel = new PoolBrowserPanel(this);
            if (_validationPanel == null) _validationPanel = new PoolValidationPanel(this);
            if (_debugPanel == null) _debugPanel = new PoolDebugPanel(this);

            SwitchTab(_activeTab);
        }

        public override void UpdateUI()
        {
            RefreshActiveTab();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_debugPanel != null && _activeTab == TabType.Debug)
            {
                _debugPanel.OnPanelEnabled();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_debugPanel != null)
            {
                _debugPanel.OnPanelDisabled();
            }
        }

        private void SwitchTab(TabType tab)
        {
            _activeTab = tab;

            if (_contentSlot == null) return;
            _contentSlot.Clear();

            // 탭 버튼 스타일 전환
            UpdateTabStyles();

            // 라이프사이클 처리 (실시간 Debug update 해제)
            if (_debugPanel != null) _debugPanel.OnPanelDisabled();

            switch (tab)
            {
                case TabType.Browser:
                    _contentSlot.Add(_browserPanel);
                    _browserPanel.RefreshSourceField();
                    _browserPanel.Refresh();
                    break;

                case TabType.Validation:
                    _contentSlot.Add(_validationPanel);
                    _validationPanel.RefreshReport();
                    break;

                case TabType.Debug:
                    _contentSlot.Add(_debugPanel);
                    _debugPanel.OnPanelEnabled();
                    _debugPanel.Refresh();
                    break;
            }
        }

        private void UpdateTabStyles()
        {
            SetTabButtonStyle(_tabBrowserButton, _activeTab == TabType.Browser);
            SetTabButtonStyle(_tabValidationButton, _activeTab == TabType.Validation);
            SetTabButtonStyle(_tabDebugButton, _activeTab == TabType.Debug);
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
                    if (_browserPanel != null) _browserPanel.Refresh();
                    break;
                case TabType.Validation:
                    if (_validationPanel != null) _validationPanel.RefreshReport();
                    break;
                case TabType.Debug:
                    if (_debugPanel != null) _debugPanel.Refresh();
                    break;
            }
        }

        /// <summary>
        /// 외부 탭이나 검증 위반 리포트에서 특정 Source 그룹을 지목했을 때 브라우저 탭으로 즉시 타겟 전환해 주는 API
        /// </summary>
        public void SelectBrowserTabWithSource(Object source)
        {
            if (_browserPanel == null) _browserPanel = new PoolBrowserPanel(this);
            _browserPanel.SetSource(source);
            SwitchTab(TabType.Browser);
        }
    }
}
