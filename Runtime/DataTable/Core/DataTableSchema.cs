using System;
using System.Collections.Generic;

namespace INLO.Core.DataTable
{
    public sealed class DataTableSchema
    {
        private readonly List<DataTableColumn> columns;
        private readonly Dictionary<string, DataTableColumn> columnsByName;

        public DataTableSchema(IEnumerable<DataTableColumn> columns)
        {
            if (columns == null)
                throw new ArgumentNullException(nameof(columns));

            this.columns = new List<DataTableColumn>();
            columnsByName = new Dictionary<string, DataTableColumn>(StringComparer.Ordinal);

            foreach (DataTableColumn column in columns)
            {
                if (column == null)
                    throw new DataTableException("DataTable schema error. Column is null.");

                if (columnsByName.ContainsKey(column.Name))
                {
                    throw new DataTableException(
                        $"DataTable schema error. Duplicate column name found: '{column.Name}'.");
                }

                this.columns.Add(column);
                columnsByName.Add(column.Name, column);
            }
        }

        public IReadOnlyList<DataTableColumn> Columns => columns;

        public bool HasIdColumn => HasColumn("id");

        public bool HasColumn(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return false;

            return columnsByName.ContainsKey(columnName.Trim());
        }

        public bool TryGetColumn(string columnName, out DataTableColumn column)
        {
            column = null;

            if (string.IsNullOrWhiteSpace(columnName))
                return false;

            return columnsByName.TryGetValue(columnName.Trim(), out column);
        }

        public DataTableColumn GetColumn(string columnName)
        {
            if (TryGetColumn(columnName, out DataTableColumn column))
                return column;

            throw new DataTableException(
                $"DataTable schema error. Column not found: '{columnName}'.");
        }
    }
}
