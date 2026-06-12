using System.Collections.Generic;
using UnityEngine.UIElements;

namespace INLO.Core.DataTable.Editor
{
    public sealed class DataTableImportResultView : VisualElement
    {
        private readonly Label summary;
        private readonly ScrollView details;

        public DataTableImportResultView()
        {
            AddToClassList("inlo-card");

            summary = new Label("Ready.");
            summary.AddToClassList("inlo-notice");
            Add(summary);

            details = new ScrollView();
            details.style.maxHeight = 260f;
            Add(details);
        }

        public void ShowSuccess(string message)
        {
            summary.text = message;
            summary.EnableInClassList("inlo-notice--ok", true);
            summary.EnableInClassList("inlo-notice--error", false);
            details.Clear();
        }

        public void ShowErrors(
            string message,
            IReadOnlyList<DataTableValidationError> errors)
        {
            summary.text = message;
            summary.EnableInClassList("inlo-notice--ok", false);
            summary.EnableInClassList("inlo-notice--error", true);
            details.Clear();

            if (errors == null)
                return;

            for (int i = 0; i < errors.Count; i++)
            {
                DataTableValidationError error = errors[i];
                Label label = new(
                    $"Row {error.RowIndex} | {error.ColumnName}\n{error.Message}");
                label.AddToClassList("inlo-error-item");
                details.Add(label);
            }
        }

        public void ShowError(string message)
        {
            summary.text = message;
            summary.EnableInClassList("inlo-notice--ok", false);
            summary.EnableInClassList("inlo-notice--error", true);
            details.Clear();
        }
    }
}
