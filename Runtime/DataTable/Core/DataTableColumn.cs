using System;

namespace INLO.Core.DataTable
{
    public sealed class DataTableColumn
    {
        public string Name { get; }
        public DataTableValueType Type { get; }
        public int Index { get; }

        public DataTableColumn(string name, DataTableValueType type, int index)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("DataTable schema error. Column name is empty.", nameof(name));

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, "DataTable schema error. Column index must be 0 or greater.");

            Name = name.Trim();
            Type = type;
            Index = index;
        }
    }
}
