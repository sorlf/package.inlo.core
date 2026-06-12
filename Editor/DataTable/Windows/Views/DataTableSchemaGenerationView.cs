using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace INLO.Core.DataTable.Editor
{
    public sealed class DataTableSchemaGenerationView : VisualElement
    {
        private const string GuideText =
            "Create a new DataTable from this source with one action.\n" +
            "Example: Table Name 'Monster' creates MonsterRow.cs, MonsterTable.cs, and MonsterTable.asset.\n" +
            "Unity generates Row/Table C# files, resumes after compilation, creates the Table asset, " +
            "imports the data, and registers it in DataTableDatabase. Existing files are never overwritten.";

        private readonly Func<CancellationToken, Task<DataTableGrid>> prepareGrid;
        private readonly Func<string> getSource;
        private readonly Func<string> getSheet;
        private readonly TextField tableNameField;
        private readonly TextField namespaceField;
        private readonly TextField scriptOutputFolderField;
        private readonly TextField scriptableObjectOutputFolderField;
        private readonly Button createButton;
        private readonly Button cancelButton;
        private readonly Label summary;
        private readonly ScrollView details;

        private CancellationTokenSource cancellation;
        private bool running;

        public DataTableSchemaGenerationView(
            Func<CancellationToken, Task<DataTableGrid>> prepareGrid,
            Func<string> getSource,
            Func<string> getSheet)
        {
            this.prepareGrid = prepareGrid ?? throw new ArgumentNullException(nameof(prepareGrid));
            this.getSource = getSource ?? throw new ArgumentNullException(nameof(getSource));
            this.getSheet = getSheet ?? throw new ArgumentNullException(nameof(getSheet));

            AddToClassList("inlo-card");
            AddToClassList("inlo-card--accent");

            Label title = new("Create New DataTable");
            title.AddToClassList("inlo-card-title");
            Add(title);

            Label guide = new(GuideText);
            guide.AddToClassList("inlo-notice");
            guide.AddToClassList("inlo-wrap");
            Add(guide);

            tableNameField = new TextField("Table Name");
            tableNameField.RegisterValueChangedCallback(_ => Invalidate());
            Add(tableNameField);

            Foldout advanced = new()
            {
                text = "Advanced Settings",
                value = false
            };

            namespaceField = new TextField("Namespace")
            {
                value = DataTableCreationWorkflow.DefaultNamespace
            };
            namespaceField.RegisterValueChangedCallback(_ => Invalidate());
            advanced.Add(namespaceField);

            scriptOutputFolderField = new TextField("Script Folder")
            {
                value = DataTableCreationWorkflow.DefaultScriptOutputFolder
            };
            scriptOutputFolderField.RegisterValueChangedCallback(_ => Invalidate());
            advanced.Add(scriptOutputFolderField);

            Button selectScriptFolderButton = new(() => SelectOutputFolder(scriptOutputFolderField))
            {
                text = "Select Script Folder"
            };
            selectScriptFolderButton.AddToClassList("inlo-button");
            advanced.Add(selectScriptFolderButton);

            scriptableObjectOutputFolderField = new TextField("ScriptableObject Folder")
            {
                value = DataTableCreationWorkflow.DefaultScriptableObjectOutputFolder
            };
            scriptableObjectOutputFolderField.RegisterValueChangedCallback(_ => Invalidate());
            advanced.Add(scriptableObjectOutputFolderField);

            Button selectScriptableObjectFolderButton =
                new(() => SelectOutputFolder(scriptableObjectOutputFolderField))
                {
                    text = "Select ScriptableObject Folder"
                };
            selectScriptableObjectFolderButton.AddToClassList("inlo-button");
            advanced.Add(selectScriptableObjectFolderButton);
            Add(advanced);

            createButton = new(CreateNewDataTable) { text = "Create New DataTable" };
            createButton.AddToClassList("inlo-button");
            createButton.AddToClassList("inlo-button--accent");

            cancelButton = new(Cancel) { text = "Cancel" };
            cancelButton.AddToClassList("inlo-button");
            cancelButton.AddToClassList("inlo-button--danger");

            VisualElement actions = new();
            actions.AddToClassList("inlo-button-row");
            actions.Add(createButton);
            actions.Add(cancelButton);
            Add(actions);

            summary = new Label(
                string.IsNullOrWhiteSpace(DataTableCreationWorkflow.LastResult)
                    ? "Enter a Table Name to create a new DataTable."
                    : DataTableCreationWorkflow.LastResult);
            summary.AddToClassList("inlo-notice");
            Add(summary);

            details = new ScrollView();
            details.style.maxHeight = 220f;
            Add(details);

            Refresh();
        }

        public void InvalidateSource()
        {
            Invalidate();
        }

        public void Cancel()
        {
            cancellation?.Cancel();
        }

        private void Invalidate()
        {
            cancellation?.Cancel();
            details.Clear();
            summary.text = "Inputs changed. Click Create New DataTable when ready.";
            summary.EnableInClassList("inlo-notice--ok", false);
            summary.EnableInClassList("inlo-notice--error", false);
            Refresh();
        }

        private void Refresh()
        {
            createButton.SetEnabled(!running && !DataTableCreationWorkflow.HasPendingRequest);
            cancelButton.style.display = running ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void SelectOutputFolder(TextField target)
        {
            string selected = EditorUtility.OpenFolderPanel(
                "Select Existing Assets Folder",
                Application.dataPath,
                string.Empty);

            if (!string.IsNullOrWhiteSpace(selected))
                target.value = DataTablePathUtility.ToProjectRelativePath(selected);
        }

        private async void CreateNewDataTable()
        {
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = new CancellationTokenSource();
            running = true;
            details.Clear();
            ShowStatus("Reading source and validating the new DataTable...", false);
            Refresh();

            try
            {
                DataTableGrid grid = await prepareGrid(cancellation.Token);
                DataTableCreationWorkflowPlan plan = DataTableCreationWorkflow.Prepare(
                    tableNameField.value,
                    namespaceField.value,
                    scriptOutputFolderField.value,
                    scriptableObjectOutputFolderField.value,
                    getSource(),
                    getSheet(),
                    grid);

                if (!plan.CanApply)
                {
                    ShowErrors(plan);
                    return;
                }

                details.Add(CreateDetail($"Type: {plan.TableTypeName}"));
                details.Add(CreateDetail($"Asset: {plan.AssetPath}"));
                details.Add(CreateDetail($"Row: {plan.CodePlan.RowPath}"));
                details.Add(CreateDetail($"Table: {plan.CodePlan.TablePath}"));

                if (!DataTableCreationWorkflow.Start(plan, out string status))
                {
                    ShowStatus(status, true);
                    return;
                }

                ShowStatus(status, false);
            }
            catch (OperationCanceledException)
            {
                ShowStatus("DataTable creation cancelled.", true);
            }
            catch (Exception exception)
            {
                ShowStatus(exception.Message, true);
            }
            finally
            {
                running = false;
                cancellation?.Dispose();
                cancellation = null;
                Refresh();
            }
        }

        private void ShowErrors(DataTableCreationWorkflowPlan plan)
        {
            ShowStatus($"New DataTable validation failed with {plan.Errors.Count} error(s).", true);
            for (int i = 0; i < plan.Errors.Count; i++)
            {
                Label error = new(plan.Errors[i]);
                error.AddToClassList("inlo-error-item");
                details.Add(error);
            }
        }

        private void ShowStatus(string message, bool error)
        {
            summary.text = message;
            summary.EnableInClassList("inlo-notice--ok", !error);
            summary.EnableInClassList("inlo-notice--error", error);
        }

        private static Label CreateDetail(string text)
        {
            Label label = new(text);
            label.AddToClassList("inlo-wrap");
            return label;
        }
    }
}
