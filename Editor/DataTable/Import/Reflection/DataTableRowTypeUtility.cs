using System;
using INLO.Core.DataTable;

namespace INLO.Core.DataTable.Editor
{
    public static class DataTableRowTypeUtility
    {
        public static bool TryGetRowType(DataTableAsset tableAsset, out Type rowType)
        {
            rowType = null;

            if (tableAsset == null)
                return false;

            Type currentType = tableAsset.GetType();

            while (currentType != null)
            {
                if (currentType.IsGenericType &&
                    currentType.GetGenericTypeDefinition() == typeof(DataTableAsset<>))
                {
                    rowType = currentType.GetGenericArguments()[0];
                    return true;
                }

                currentType = currentType.BaseType;
            }

            return false;
        }
    }
}