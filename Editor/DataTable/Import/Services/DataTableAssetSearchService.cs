using INLO.Core.DataTable;
using System.Collections.Generic;
using UnityEditor;

namespace INLO.Core.DataTable.Editor
{
    public static class DataTableAssetSearchService
    {
        public static List<DataTableAsset> FindAll()
        {
            List<DataTableAsset> result = new();

            string[] guids = AssetDatabase.FindAssets("t:DataTableAsset");

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                DataTableAsset asset = AssetDatabase.LoadAssetAtPath<DataTableAsset>(path);

                if (asset == null)
                    continue;

                result.Add(asset);
            }

            result.Sort(
                (left, right) => string.CompareOrdinal(left.name, right.name));

            return result;
        }

        public static string GetAssetPath(DataTableAsset asset)
        {
            if (asset == null)
                return string.Empty;

            return AssetDatabase.GetAssetPath(asset);
        }
    }
}
