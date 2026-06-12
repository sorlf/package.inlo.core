using INLO.Core.DataTable;
using System;
using System.Collections;
using System.Collections.Generic;

namespace INLO.Core.DataTable.Editor
{
    public sealed class DataTableImportPlan
    {
        public DataTableImportPlan(
            DataTableAsset target,
            string source,
            string sheet,
            Type rowType,
            DataTableGrid grid,
            IList rows,
            DataTableImportSchema schema,
            List<DataTableValidationError> errors)
        {
            Target = target;
            Source = source ?? string.Empty;
            Sheet = sheet ?? string.Empty;
            RowType = rowType;
            Grid = grid;
            Rows = rows;
            Schema = schema;
            Errors = errors ?? new List<DataTableValidationError>();
            Diff = Errors.Count == 0
                ? DataTableImportDiff.Build(target, rows, schema)
                : new DataTableImportDiff();
        }

        public DataTableAsset Target { get; }
        public string Source { get; }
        public string Sheet { get; }
        public Type RowType { get; }
        public DataTableGrid Grid { get; }
        public IList Rows { get; }
        public DataTableImportSchema Schema { get; }
        public IReadOnlyList<DataTableValidationError> Errors { get; }
        public DataTableImportDiff Diff { get; }
        public bool CanApply => Target != null && Rows != null && Errors.Count == 0;

        public bool Matches(DataTableAsset target, string source, string sheet)
        {
            return ReferenceEquals(Target, target) &&
                   string.Equals(Source, source ?? string.Empty, StringComparison.Ordinal) &&
                   string.Equals(Sheet, sheet ?? string.Empty, StringComparison.Ordinal);
        }
    }
}
