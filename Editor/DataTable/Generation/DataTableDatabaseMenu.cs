using UnityEditor;
using UnityEngine;

namespace INLO.Core.DataTable.Editor
{
    public static class DataTableDatabaseMenu
    {
        [MenuItem("Tools/INLO/DataTable/Generate Database")]
        private static void GenerateDatabase()
        {
            DataTableImporterWindow.OpenWindow();
            Debug.Log("Use Database Preview / Apply in the DataTable Importer.");
        }
    }
}
