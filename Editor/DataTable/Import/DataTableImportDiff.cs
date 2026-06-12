using INLO.Core.DataTable;
using System;
using System.Collections;
using System.Collections.Generic;

namespace INLO.Core.DataTable.Editor
{
    public sealed class DataTableImportDiff
    {
        public int Added { get; private set; }
        public int Changed { get; private set; }
        public int Removed { get; private set; }
        public int Unchanged { get; private set; }

        public static DataTableImportDiff Build(
            DataTableAsset target,
            IList preparedRows,
            DataTableImportSchema schema)
        {
            DataTableImportDiff diff = new();
            Dictionary<string, object> currentRows = BuildRowMap(GetCurrentRows(target));
            Dictionary<string, object> nextRows = BuildRowMap(preparedRows);

            foreach (KeyValuePair<string, object> pair in nextRows)
            {
                if (!currentRows.TryGetValue(pair.Key, out object current))
                {
                    diff.Added++;
                    continue;
                }

                if (RowsEqual(current, pair.Value, schema))
                    diff.Unchanged++;
                else
                    diff.Changed++;
            }

            foreach (string id in currentRows.Keys)
            {
                if (!nextRows.ContainsKey(id))
                    diff.Removed++;
            }

            return diff;
        }

        private static IList GetCurrentRows(DataTableAsset target)
        {
            if (target == null)
                return Array.Empty<object>();

            var rowsProperty = target.GetType().GetProperty("Rows");
            return rowsProperty?.GetValue(target) as IList ?? Array.Empty<object>();
        }

        private static Dictionary<string, object> BuildRowMap(IList rows)
        {
            Dictionary<string, object> result = new(StringComparer.OrdinalIgnoreCase);

            if (rows == null)
                return result;

            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] is not IDataTableRow row || string.IsNullOrWhiteSpace(row.Id))
                    continue;

                result[row.Id.Trim()] = rows[i];
            }

            return result;
        }

        private static bool RowsEqual(
            object left,
            object right,
            DataTableImportSchema schema)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left == null || right == null || schema == null)
                return false;

            for (int i = 0; i < schema.Fields.Count; i++)
            {
                DataTableImportField field = schema.Fields[i];
                object leftValue = field.Field.GetValue(left);
                object rightValue = field.Field.GetValue(right);

                if (!Equals(leftValue, rightValue))
                    return false;
            }

            return true;
        }
    }
}
