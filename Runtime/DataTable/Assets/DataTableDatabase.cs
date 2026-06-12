using System.Collections.Generic;
using UnityEngine;

namespace INLO.Core.DataTable
{
    [CreateAssetMenu(
        fileName = "DataTableDatabase",
        menuName = "INLO/DataTable/DataTable Database")]
    public sealed class DataTableDatabase : ScriptableObject
    {
        [SerializeField]
        private List<DataTableAsset> tables = new();

        public IReadOnlyList<DataTableAsset> Tables => tables;

        public int Count => tables.Count;

        public void BuildAllCaches()
        {
            for (int i = 0; i < tables.Count; i++)
            {
                DataTableAsset table = tables[i];

                if (table == null)
                    continue;

                table.BuildCache();
            }
        }

        public void ClearAllCaches()
        {
            for (int i = 0; i < tables.Count; i++)
            {
                DataTableAsset table = tables[i];

                if (table == null)
                    continue;

                table.ClearCache();
            }
        }

        public bool TryGetTable<TTable>(out TTable table)
            where TTable : DataTableAsset
        {
            for (int i = 0; i < tables.Count; i++)
            {
                if (tables[i] is TTable typedTable)
                {
                    table = typedTable;
                    return true;
                }
            }

            table = null;
            return false;
        }

        public TTable GetTableOrThrow<TTable>()
            where TTable : DataTableAsset
        {
            if (TryGetTable(out TTable table))
                return table;

            throw new DataTableException(
                $"DataTableDatabase: Table not found. Type: {typeof(TTable).Name}");
        }

        public bool ContainsTable<TTable>()
            where TTable : DataTableAsset
        {
            return TryGetTable<TTable>(out _);
        }

#if UNITY_EDITOR
        public void Editor_SetTables(List<DataTableAsset> newTables)
        {
            tables = newTables ?? new List<DataTableAsset>();
        }
#endif
    }
}
