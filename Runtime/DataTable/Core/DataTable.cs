using System;
using System.Collections.Generic;

namespace INLO.Core.DataTable
{
    public sealed class DataTable
    {
        private readonly List<DataTableRow> rows;
        private readonly Dictionary<long, DataTableRow> rowsById;
        private readonly Dictionary<string, DataTableRow> rowsByKey;

        public DataTable(DataTableSchema schema, IEnumerable<DataTableRow> rows)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));

            if (!schema.HasIdColumn)
                throw new DataTableException("DataTable schema error. Required column is missing: id.");

            this.rows = new List<DataTableRow>();
            rowsById = new Dictionary<long, DataTableRow>();
            rowsByKey = schema.HasColumn("key")
                ? new Dictionary<string, DataTableRow>(StringComparer.Ordinal)
                : null;

            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            foreach (DataTableRow row in rows)
            {
                AddRow(row);
            }
        }

        public DataTableSchema Schema { get; }
        public IReadOnlyList<DataTableRow> Rows => rows;

        public bool TryGetRow(int id, out DataTableRow row)
        {
            return TryGetRow((long)id, out row);
        }

        public bool TryGetRow(long id, out DataTableRow row)
        {
            return rowsById.TryGetValue(id, out row);
        }

        public DataTableRow GetRow(int id)
        {
            return GetRow((long)id);
        }

        public DataTableRow GetRow(long id)
        {
            if (TryGetRow(id, out DataTableRow row))
                return row;

            throw new DataTableException($"DataTable row not found. Id: {id}");
        }

        public bool TryGetRowByKey(string key, out DataTableRow row)
        {
            row = null;

            if (rowsByKey == null || string.IsNullOrWhiteSpace(key))
                return false;

            return rowsByKey.TryGetValue(key.Trim(), out row);
        }

        private void AddRow(DataTableRow row)
        {
            if (row == null)
                throw new DataTableException("DataTable row error. Row is null.");

            long id = row.LongId;

            if (rowsById.ContainsKey(id))
            {
                throw new DataTableException(
                    $"DataTable row error. Duplicate id found: {id}. Row {row.SourceRowNumber}.");
            }

            rows.Add(row);
            rowsById.Add(id, row);

            if (rowsByKey == null)
                return;

            string key = row.GetString("key");

            if (string.IsNullOrWhiteSpace(key))
                return;

            if (rowsByKey.ContainsKey(key))
            {
                throw new DataTableException(
                    $"DataTable row error. Duplicate key found: '{key}'. Row {row.SourceRowNumber}.");
            }

            rowsByKey.Add(key, row);
        }
    }
}
