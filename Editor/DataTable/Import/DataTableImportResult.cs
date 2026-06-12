using System.Collections.Generic;

namespace INLO.Core.DataTable.Editor
{
    public sealed class DataTableImportResult<TRow>
    {
        public readonly List<TRow> Rows = new();
        public readonly List<DataTableValidationError> Errors = new();

        public bool Success => Errors.Count == 0;

        public void AddError(DataTableValidationError error)
        {
            Errors.Add(error);
        }

        public void AddRow(TRow row)
        {
            Rows.Add(row);
        }
    }
}