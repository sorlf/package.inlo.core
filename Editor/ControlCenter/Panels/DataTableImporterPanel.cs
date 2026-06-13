using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using INLO.Core.DataTable;
using INLO.Core.DataTable.Editor;

namespace INLO.Core.EditorUI.Editor
{
    [InloControlCenterPanel("DataTable Importer", "d_Import", "panel-datatable", 4)]
    public sealed class DataTableImporterPanel : IControlCenterPanel
    {
        private enum DtTab { Xlsx, Google, Management }

        private InloControlCenterWindow _window;
        private DtTab _activeTab = DtTab.Xlsx;

        private Button _tabXlsxBtn;
        private Button _tabGoogleBtn;
        private Button _tabManagementBtn;
        private Label _selectedLabel;
        private VisualElement _tableListSlot;
        private VisualElement _contentSlot;

        private DataTableXlsxImportPanel _xlsxPanel;
        private DataTableGoogleImportPanel _googlePanel;
        private DataTableManagementPanel _managementPanel;
        private DataTableAsset _selectedTable;
        private readonly List<DataTableAsset> _tablesList = new();

        public void Initialize(InloControlCenterWindow window, VisualElement root)
        {
            _window = window;

            _tabXlsxBtn = root.Q<Button>("cc-dt-tab-xlsx");
            _tabGoogleBtn = root.Q<Button>("cc-dt-tab-google");
            _tabManagementBtn = root.Q<Button>("cc-dt-tab-management");
            _selectedLabel = root.Q<Label>("cc-dt-selected-table-label");
            _tableListSlot = root.Q<VisualElement>("cc-table-list-slot");
            _contentSlot = root.Q<VisualElement>("cc-dt-content-slot");

            if (_tabXlsxBtn != null) _tabXlsxBtn.clicked += () => SwitchTab(DtTab.Xlsx);
            if (_tabGoogleBtn != null) _tabGoogleBtn.clicked += () => SwitchTab(DtTab.Google);
            if (_tabManagementBtn != null) _tabManagementBtn.clicked += () => SwitchTab(DtTab.Management);

            _xlsxPanel ??= new DataTableXlsxImportPanel(OnImportApplied);
            _googlePanel ??= new DataTableGoogleImportPanel(OnImportApplied);
            _managementPanel ??= new DataTableManagementPanel(OnImportApplied);

            // Restore states via SessionState
            _activeTab = (DtTab)SessionState.GetInt("INLO_CC_DT_ActiveTab", (int)DtTab.Xlsx);
            string savedTableName = SessionState.GetString("INLO_CC_DT_SelectedTable", string.Empty);

            RefreshTables();

            if (!string.IsNullOrEmpty(savedTableName))
            {
                DataTableAsset match = _tablesList.Find(t => t.name == savedTableName);
                if (match != null)
                {
                    _selectedTable = match;
                }
            }

            if (_selectedTable != null)
            {
                _xlsxPanel.SetTarget(_selectedTable);
                _googlePanel.SetTarget(_selectedTable);
                _managementPanel.SetTarget(_selectedTable);
            }

            SwitchTab(_activeTab);
        }

        public void OnPanelEnabled() { }

        public void OnPanelDisabled()
        {
            _googlePanel?.CancelRequest();
        }

        public void UpdateUI()
        {
            if (_selectedLabel != null)
            {
                _selectedLabel.text = _selectedTable == null
                    ? "No DataTableAsset selected"
                    : $"Selected: {_selectedTable.name}";
            }

            RefreshTableButtons();
            UpdateTabStyles();
        }

        private void RefreshTables()
        {
            _tablesList.Clear();
            _tablesList.AddRange(DataTableAssetSearchService.FindAll());

            if (_selectedTable == null && Selection.activeObject is DataTableAsset active)
                _selectedTable = active;

            if (_selectedTable == null && _tablesList.Count > 0)
                _selectedTable = _tablesList[0];
        }

        private void RefreshTableButtons()
        {
            if (_tableListSlot == null) return;
            _tableListSlot.Clear();

            for (int i = 0; i < _tablesList.Count; i++)
            {
                DataTableAsset table = _tablesList[i];
                Button button = new(() => SelectTable(table))
                {
                    text = $"{table.name}\n{table.EditorSourceKind} | {table.EditorLastImportStatus}"
                };
                button.AddToClassList("inlo-list-card-button");
                button.EnableInClassList("inlo-list-card-button--selected", ReferenceEquals(_selectedTable, table));
                _tableListSlot.Add(button);
            }
        }

        private void SelectTable(DataTableAsset table)
        {
            _selectedTable = table;
            if (table != null)
            {
                SessionState.SetString("INLO_CC_DT_SelectedTable", table.name);
            }
            _xlsxPanel.SetTarget(table);
            _googlePanel.SetTarget(table);
            _managementPanel.SetTarget(table);
            UpdateUI();
            SwitchTab(_activeTab);
        }

        private void SwitchTab(DtTab tab)
        {
            _activeTab = tab;
            SessionState.SetInt("INLO_CC_DT_ActiveTab", (int)tab);
            if (_contentSlot == null) return;

            _contentSlot.Clear();

            switch (tab)
            {
                case DtTab.Xlsx:
                    _xlsxPanel.SetTarget(_selectedTable);
                    _contentSlot.Add(_xlsxPanel);
                    _xlsxPanel.Refresh();
                    break;
                case DtTab.Google:
                    _googlePanel.SetTarget(_selectedTable);
                    _contentSlot.Add(_googlePanel);
                    _googlePanel.Refresh();
                    break;
                case DtTab.Management:
                    _managementPanel.SetTarget(_selectedTable);
                    _contentSlot.Add(_managementPanel);
                    _managementPanel.Refresh();
                    break;
            }

            UpdateUI();
        }

        private void UpdateTabStyles()
        {
            SetSubTabStyle(_tabXlsxBtn, _activeTab == DtTab.Xlsx);
            SetSubTabStyle(_tabGoogleBtn, _activeTab == DtTab.Google);
            SetSubTabStyle(_tabManagementBtn, _activeTab == DtTab.Management);
        }

        private void OnImportApplied()
        {
            RefreshTables();
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
