using System.Collections.Generic;

namespace INLO.Core.DataTable.Editor
{
    public sealed class DataTableGrid
    {
        public readonly List<string> Headers = new();
        public readonly List<DataTableGridRow> Rows = new();

        public int HeaderCount => Headers.Count;
        public int RowCount => Rows.Count;
    }

    public sealed class DataTableGridRow
    {
        public readonly int SourceRowNumber;
        public readonly List<string> Cells;

        public DataTableGridRow(int sourceRowNumber, List<string> cells)
        {
            SourceRowNumber = sourceRowNumber;
            Cells = cells;
        }

        public string GetCell(int index)
        {
            if (index < 0 || index >= Cells.Count)
                return string.Empty;

            return Cells[index] ?? string.Empty;
        }
    }
}