using INLO.Core.DataTable;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace INLO.Core.DataTable.Editor
{
    public sealed class DataTableXlsxImportPanel : ScrollView
    {
        private readonly Action importApplied;
        private readonly TextField pathField;
        private readonly DropdownField sheetField;
        private readonly Button importButton;
        private readonly Label targetLabel;
        private readonly DataTableImportResultView resultView;
        private readonly DataTableSchemaGenerationView schemaGenerationView;
        private readonly List<string> sheetNames = new();

        private DataTableAsset target;

        public DataTableXlsxImportPanel(Action importApplied)
        {
            this.importApplied = importApplied;
            AddToClassList("inlo-card--grow");

            VisualElement card = CreateCard("XLSX Import");
            card.Add(new Label("Select an XLSX file and import it into the selected DataTableAsset."));

            targetLabel = new Label();
            targetLabel.AddToClassList("inlo-notice");
            card.Add(targetLabel);

            pathField = new TextField("XLSX Path");
            pathField.RegisterValueChangedCallback(_ =>
            {
                schemaGenerationView?.InvalidateSource();
                Refresh();
            });
            pathField.RegisterCallback<FocusOutEvent>(_ => LoadSheets());
            card.Add(pathField);

            Button selectButton = new(SelectFile) { text = "Select XLSX" };
            selectButton.AddToClassList("inlo-button");

            sheetField = new DropdownField("Sheet");
            sheetField.choices = sheetNames;
            sheetField.RegisterValueChangedCallback(_ => schemaGenerationView?.InvalidateSource());
            card.Add(sheetField);

            importButton = new(Import) { text = "Import XLSX" };
            importButton.AddToClassList("inlo-button");
            importButton.AddToClassList("inlo-button--accent");

            VisualElement actions = new();
            actions.AddToClassList("inlo-button-row");
            actions.Add(selectButton);
            actions.Add(importButton);
            card.Add(actions);

            resultView = new DataTableImportResultView();
            schemaGenerationView = new DataTableSchemaGenerationView(
                PrepareSchemaGrid,
                () => pathField.value,
                () => sheetField.value);
            Add(card);
            Add(schemaGenerationView);
            Add(resultView);
        }

        public void SetTarget(DataTableAsset value)
        {
            if (ReferenceEquals(target, value))
                return;

            target = value;
            schemaGenerationView.InvalidateSource();
            if (target != null && target.EditorSourceKind == "Xlsx")
            {
                pathField.SetValueWithoutNotify(target.EditorSourcePath);
                LoadSheets(target.EditorSheetName);
            }
            else
            {
                pathField.SetValueWithoutNotify(string.Empty);
                SetSheets(new List<string>(), string.Empty);
            }

            Refresh();
        }

        private Task<DataTableGrid> PrepareSchemaGrid(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string path = DataTablePathUtility.ToAbsolutePath(pathField.value);
            DataTableGrid grid = XlsxTableGridReader.ReadSheet(path, sheetField.value);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(grid);
        }

        public void Refresh()
        {
            targetLabel.text = target == null
                ? "Select a DataTableAsset from the left."
                : $"Target: {target.name} | Rows: {target.Count}";

            importButton.SetEnabled(
                target != null &&
                DataTablePathUtility.Exists(pathField.value) &&
                !string.IsNullOrWhiteSpace(sheetField.value));
        }

        private void SelectFile()
        {
            string selected = EditorUtility.OpenFilePanel(
                "Select XLSX",
                Application.dataPath,
                "xlsx");

            if (string.IsNullOrWhiteSpace(selected))
                return;

            pathField.value = DataTablePathUtility.ToProjectRelativePath(selected);
            LoadSheets();
        }

        private void LoadSheets()
        {
            LoadSheets(string.Empty);
        }

        private void LoadSheets(string preferredSheet)
        {
            try
            {
                string path = DataTablePathUtility.ToAbsolutePath(pathField.value);
                SetSheets(XlsxTableGridReader.ReadSheetNames(path), preferredSheet);
                resultView.ShowSuccess($"Loaded {sheetNames.Count} sheet(s).");
            }
            catch (Exception exception)
            {
                SetSheets(new List<string>(), string.Empty);
                if (!string.IsNullOrWhiteSpace(pathField.value))
                    resultView.ShowError(exception.Message);
            }

            Refresh();
        }

        private void SetSheets(List<string> values, string preferredSheet)
        {
            schemaGenerationView?.InvalidateSource();
            sheetNames.Clear();
            sheetNames.AddRange(values);
            sheetField.choices = sheetNames;

            string selected = sheetNames.Contains(preferredSheet)
                ? preferredSheet
                : sheetNames.Count > 0 ? sheetNames[0] : string.Empty;

            sheetField.SetValueWithoutNotify(selected);
        }

        private void Import()
        {
            DataTableImportPlan plan = DataTableImportPlanService.PrepareXlsx(
                pathField.value,
                sheetField.value,
                target);

            if (!plan.CanApply)
            {
                resultView.ShowErrors("XLSX import failed.", plan.Errors);
                return;
            }

            if (!DataTableImportPlanService.Apply(plan, out string status))
            {
                resultView.ShowError(status);
                return;
            }

            resultView.ShowSuccess(status);
            importApplied?.Invoke();
            Refresh();
        }

        private static VisualElement CreateCard(string title)
        {
            VisualElement card = new();
            card.AddToClassList("inlo-card");
            Label label = new(title);
            label.AddToClassList("inlo-card-title");
            card.Add(label);
            return card;
        }
    }
}
