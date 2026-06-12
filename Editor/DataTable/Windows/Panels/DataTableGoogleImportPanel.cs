using INLO.Core.DataTable;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace INLO.Core.DataTable.Editor
{
    public sealed class DataTableGoogleImportPanel : ScrollView
    {
        private readonly Action importApplied;
        private readonly TextField urlField;
        private readonly Button importButton;
        private readonly Button cancelButton;
        private readonly Label targetLabel;
        private readonly DataTableImportResultView resultView;
        private readonly DataTableSchemaGenerationView schemaGenerationView;

        private CancellationTokenSource cancellation;
        private DataTableAsset target;
        private bool running;

        public DataTableGoogleImportPanel(Action importApplied)
        {
            this.importApplied = importApplied;
            AddToClassList("inlo-card--grow");

            VisualElement card = CreateCard("Google Sheets");
            card.Add(new Label(
                "Paste a Google Sheets Published CSV URL. Import downloads, validates, converts, and applies it."));

            targetLabel = new Label();
            targetLabel.AddToClassList("inlo-notice");
            card.Add(targetLabel);

            urlField = new TextField("Published CSV URL");
            urlField.RegisterValueChangedCallback(_ =>
            {
                schemaGenerationView?.InvalidateSource();
                Refresh();
            });
            card.Add(urlField);

            importButton = new(Import) { text = "Import Google Sheet" };
            importButton.AddToClassList("inlo-button");
            importButton.AddToClassList("inlo-button--accent");

            cancelButton = new(CancelRequest) { text = "Cancel" };
            cancelButton.AddToClassList("inlo-button");
            cancelButton.AddToClassList("inlo-button--danger");

            VisualElement actions = new();
            actions.AddToClassList("inlo-button-row");
            actions.Add(importButton);
            actions.Add(cancelButton);
            card.Add(actions);

            resultView = new DataTableImportResultView();
            schemaGenerationView = new DataTableSchemaGenerationView(
                PrepareSchemaGrid,
                () => urlField.value,
                () => string.Empty);
            Add(card);
            Add(schemaGenerationView);
            Add(resultView);
        }

        public void SetTarget(DataTableAsset value)
        {
            if (ReferenceEquals(target, value))
                return;

            CancelRequest();
            target = value;
            schemaGenerationView.InvalidateSource();
            urlField.SetValueWithoutNotify(
                target != null && target.EditorSourceKind == "GooglePublishedCsv"
                    ? target.EditorSourcePath
                    : string.Empty);
            Refresh();
        }

        public void Refresh()
        {
            targetLabel.text = target == null
                ? "Select a DataTableAsset from the left."
                : $"Target: {target.name} | Rows: {target.Count}";

            importButton.SetEnabled(
                !running &&
                target != null &&
                !string.IsNullOrWhiteSpace(urlField.value));
            cancelButton.style.display = running ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void CancelRequest()
        {
            cancellation?.Cancel();
            schemaGenerationView?.Cancel();
        }

        private async Task<DataTableGrid> PrepareSchemaGrid(CancellationToken cancellationToken)
        {
            string csv = await GooglePublishedCsvRequest.DownloadAsync(
                urlField.value,
                cancellationToken);
            return GooglePublishedCsvGridReader.Parse(csv);
        }

        private async void Import()
        {
            if (!GooglePublishedCsvGridReader.IsSupportedUrl(urlField.value))
            {
                resultView.ShowError(
                    "Use an HTTPS Google Sheets Published CSV URL ending with output=csv.");
                return;
            }

            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = new CancellationTokenSource();
            running = true;
            resultView.ShowSuccess("Downloading Google Published CSV...");
            Refresh();

            try
            {
                string csv = await GooglePublishedCsvRequest.DownloadAsync(
                    urlField.value,
                    cancellation.Token);
                DataTableGrid grid = GooglePublishedCsvGridReader.Parse(csv);
                DataTableImportPlan plan = DataTableImportPlanService.PrepareGrid(
                    urlField.value,
                    string.Empty,
                    target,
                    grid);

                if (!plan.CanApply)
                {
                    resultView.ShowErrors("Google Sheet import failed.", plan.Errors);
                    return;
                }

                if (!DataTableImportPlanService.Apply(plan, out string status))
                {
                    resultView.ShowError(status);
                    return;
                }

                resultView.ShowSuccess(status);
                importApplied?.Invoke();
            }
            catch (OperationCanceledException)
            {
                resultView.ShowError("Google Sheet import cancelled.");
            }
            catch (Exception exception)
            {
                resultView.ShowError(exception.Message);
            }
            finally
            {
                running = false;
                cancellation?.Dispose();
                cancellation = null;
                Refresh();
            }
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
