using INLO.Core.DataTable;
using INLO.Core.EditorUI.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace INLO.Core.DataTable.Editor
{
    public sealed class DataTableImporterWindow : InloBaseEditorWindow
    {
        private const string CommonUssPath =
            "Packages/com.inlo.core/Editor/EditorUI/USS/InloWindowCommon.uss";

        private const string WindowUxmlPath =
            "Packages/com.inlo.core/Editor/DataTable/Windows/UXML/DataTableImporterWindow.uxml";

        private enum TabType
        {
            Xlsx,
            Google,
            Management
        }

        protected override string UxmlPath => WindowUxmlPath;
        protected override string UssPath => CommonUssPath;
        protected override string MainScrollViewName => string.Empty;

        private readonly List<DataTableAsset> tables = new();

        private Button tabXlsx;
        private Button tabGoogle;
        private Button tabManagement;
        private Label selectedLabel;
        private VisualElement tableList;
        private VisualElement contentSlot;

        private DataTableXlsxImportPanel xlsxPanel;
        private DataTableGoogleImportPanel googlePanel;
        private DataTableManagementPanel managementPanel;
        private DataTableAsset selectedTable;
        private TabType activeTab = TabType.Xlsx;

        [MenuItem("Tools/INLO/DataTable/Importer")]
        public static void OpenWindow()
        {
            DataTableImporterWindow window = GetWindow<DataTableImporterWindow>("DataTable Importer");
            window.minSize = new Vector2(980f, 640f);
            window.Show();
        }

        protected override void OnDisable()
        {
            googlePanel?.CancelRequest();
            base.OnDisable();
        }

        protected override void OnBindElements()
        {
            tabXlsx = Require<Button>("tab-xlsx");
            tabGoogle = Require<Button>("tab-google");
            tabManagement = Require<Button>("tab-management");
            selectedLabel = Require<Label>("selected-table-label");
            tableList = Require<VisualElement>("table-list-slot");
            contentSlot = Require<VisualElement>("manager-content-slot");

            tabXlsx.clicked += () => SwitchTab(TabType.Xlsx);
            tabGoogle.clicked += () => SwitchTab(TabType.Google);
            tabManagement.clicked += () => SwitchTab(TabType.Management);

            xlsxPanel ??= new DataTableXlsxImportPanel(OnImportApplied);
            googlePanel ??= new DataTableGoogleImportPanel(OnImportApplied);
            managementPanel ??= new DataTableManagementPanel(OnImportApplied);

            RefreshTables();
            SwitchTab(activeTab);
        }

        public override void UpdateUI()
        {
            selectedLabel.text = selectedTable == null
                ? "No DataTableAsset selected"
                : $"Selected: {selectedTable.name}";

            RefreshTableButtons();
            UpdateTabStyles();
            ActivePanelRefresh();
        }

        private T Require<T>(string elementName)
            where T : VisualElement
        {
            T element = rootVisualElement.Q<T>(elementName);
            if (element == null)
                throw new MissingReferenceException($"DataTable Importer UXML element is missing: {elementName}");

            return element;
        }

        private void RefreshTables()
        {
            tables.Clear();
            tables.AddRange(DataTableAssetSearchService.FindAll());

            if (selectedTable == null && Selection.activeObject is DataTableAsset active)
                selectedTable = active;

            if (selectedTable == null && tables.Count > 0)
                selectedTable = tables[0];
        }

        private void RefreshTableButtons()
        {
            tableList.Clear();

            for (int i = 0; i < tables.Count; i++)
            {
                DataTableAsset table = tables[i];
                Button button = new(() => SelectTable(table))
                {
                    text = $"{table.name}\n{SourceDescription(table)}"
                };
                button.AddToClassList("inlo-list-card-button");
                button.EnableInClassList(
                    "inlo-list-card-button--selected",
                    ReferenceEquals(selectedTable, table));
                tableList.Add(button);
            }
        }

        private static string SourceDescription(DataTableAsset table)
        {
            if (string.IsNullOrWhiteSpace(table.EditorSourcePath))
                return "No source";

            return $"{table.EditorSourceKind} | {table.EditorLastImportStatus}";
        }

        private void SelectTable(DataTableAsset table)
        {
            selectedTable = table;
            xlsxPanel.SetTarget(table);
            googlePanel.SetTarget(table);
            managementPanel.SetTarget(table);
            UpdateUI();
        }

        private void SwitchTab(TabType tab)
        {
            activeTab = tab;
            contentSlot.Clear();

            switch (tab)
            {
                case TabType.Xlsx:
                    xlsxPanel.SetTarget(selectedTable);
                    contentSlot.Add(xlsxPanel);
                    break;
                case TabType.Google:
                    googlePanel.SetTarget(selectedTable);
                    contentSlot.Add(googlePanel);
                    break;
                case TabType.Management:
                    managementPanel.SetTarget(selectedTable);
                    contentSlot.Add(managementPanel);
                    break;
            }

            UpdateUI();
        }

        private void UpdateTabStyles()
        {
            SetTabStyle(tabXlsx, activeTab == TabType.Xlsx);
            SetTabStyle(tabGoogle, activeTab == TabType.Google);
            SetTabStyle(tabManagement, activeTab == TabType.Management);
        }

        private static void SetTabStyle(Button button, bool selected)
        {
            button.EnableInClassList("inlo-tab-button--selected", selected);
            button.EnableInClassList("inlo-tab-button--idle", !selected);
        }

        private void ActivePanelRefresh()
        {
            switch (activeTab)
            {
                case TabType.Xlsx:
                    xlsxPanel.Refresh();
                    break;
                case TabType.Google:
                    googlePanel.Refresh();
                    break;
                case TabType.Management:
                    managementPanel.Refresh();
                    break;
            }
        }

        private void OnImportApplied()
        {
            RefreshTables();
            UpdateUI();
        }
    }
}
