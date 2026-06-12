using INLO.Core.DataTable;
using UnityEditor;

namespace INLO.Core.DataTable.Editor
{
    public static class DataTableDatabaseSearchService
    {
        public static DataTableDatabase FindDefault()
        {
            return AssetDatabase.LoadAssetAtPath<DataTableDatabase>(
                DataTableDatabaseGenerator.DefaultDatabasePath);
        }

        public static string GetAssetPath(DataTableDatabase database)
        {
            if (database == null)
                return string.Empty;

            return AssetDatabase.GetAssetPath(database);
        }
    }
}
