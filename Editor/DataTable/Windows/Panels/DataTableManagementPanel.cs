using INLO.Core.DataTable;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.UIElements;

namespace INLO.Core.DataTable.Editor
{
    public sealed class DataTableManagementPanel : VisualElement
    {
        private readonly Action changed;
        private readonly Button refreshAllButton;
        private readonly Button updateDatabaseButton;
        private readonly DataTableImportResultView resultView;

        private DataTableAsset target;
        private bool running;

        public DataTableManagementPanel(Action changed)
        {
            this.changed = changed;
            AddToClassList("inlo-card--grow");

            VisualElement card = new();
            card.AddToClassList("inlo-card");

            Label title = new("Management");
            title.AddToClassList("inlo-card-title");
            card.Add(title);
            card.Add(new Label(
                "Refresh every configured DataTable atomically, or rebuild the runtime DataTableDatabase."));

            refreshAllButton = new(RefreshAll) { text = "Refresh All Tables" };
            refreshAllButton.AddToClassList("inlo-button");
            refreshAllButton.AddToClassList("inlo-button--accent");

            updateDatabaseButton = new(UpdateDatabase) { text = "Update Database" };
            updateDatabaseButton.AddToClassList("inlo-button");

            VisualElement actions = new();
            actions.AddToClassList("inlo-button-row");
            actions.Add(refreshAllButton);
            actions.Add(updateDatabaseButton);
            card.Add(actions);

            resultView = new DataTableImportResultView();
            Add(card);
            Add(resultView);
        }

        public void SetTarget(DataTableAsset value)
        {
            target = value;
        }

        public void Refresh()
        {
            refreshAllButton.SetEnabled(!running);
            updateDatabaseButton.SetEnabled(!running);
        }

        private async void RefreshAll()
        {
            running = true;
            Refresh();

            try
            {
                List<DataTableAsset> tables = DataTableAssetSearchService.FindAll();
                List<DataTableImportPlan> plans = new();
                List<DataTableValidationError> errors = new();
                int notConfigured = 0;

                for (int i = 0; i < tables.Count; i++)
                {
                    DataTableAsset table = tables[i];
                    if (string.IsNullOrWhiteSpace(table.EditorSourcePath))
                    {
                        notConfigured++;
                        continue;
                    }

                    DataTableImportPlan plan = await PrepareStoredSource(table);
                    if (plan == null || !plan.CanApply)
                    {
                        if (plan != null)
                        {
                            for (int errorIndex = 0; errorIndex < plan.Errors.Count; errorIndex++)
                            {
                                DataTableValidationError error = plan.Errors[errorIndex];
                                errors.Add(new DataTableValidationError(
                                    error.Type,
                                    error.RowIndex,
                                    table.name,
                                    error.Message));
                            }
                        }

                        continue;
                    }

                    plans.Add(plan);
                }

                if (errors.Count > 0)
                {
                    resultView.ShowErrors(
                        $"Refresh All stopped before apply. Failed tables: {errors.Count}, Not configured: {notConfigured}",
                        errors);
                    return;
                }

                if (plans.Count == 0)
                {
                    resultView.ShowError("No configured DataTable source was found.");
                    return;
                }

                if (!DataTableAssetGenerator.ApplyImportPlans(plans, out string status))
                {
                    resultView.ShowError(status);
                    return;
                }

                resultView.ShowSuccess(
                    $"{status} Not configured: {notConfigured}");
                changed?.Invoke();
            }
            catch (Exception exception)
            {
                resultView.ShowError(exception.Message);
            }
            finally
            {
                running = false;
                Refresh();
            }
        }

        private static async System.Threading.Tasks.Task<DataTableImportPlan> PrepareStoredSource(
            DataTableAsset table)
        {
            bool google =
                table.EditorSourceKind == "GooglePublishedCsv" ||
                GooglePublishedCsvGridReader.IsSupportedUrl(table.EditorSourcePath);

            if (!google)
            {
                return DataTableImportPlanService.PrepareXlsx(
                    table.EditorSourcePath,
                    table.EditorSheetName,
                    table);
            }

            string csv = await GooglePublishedCsvRequest.DownloadAsync(
                table.EditorSourcePath,
                CancellationToken.None);
            return DataTableImportPlanService.PrepareGrid(
                table.EditorSourcePath,
                string.Empty,
                table,
                GooglePublishedCsvGridReader.Parse(csv));
        }

        private void UpdateDatabase()
        {
            DataTableDatabase current = DataTableDatabaseSearchService.FindDefault();
            DataTableDatabasePlan plan = DataTableDatabaseGenerator.PreparePlan(current);

            if (!plan.CanApply)
            {
                resultView.ShowError(string.Join("\n", plan.Conflicts));
                return;
            }

            if (!DataTableDatabaseGenerator.ApplyPlan(plan, out _, out string status))
            {
                resultView.ShowError(status);
                return;
            }

            resultView.ShowSuccess(status);
            changed?.Invoke();
        }
    }
}
