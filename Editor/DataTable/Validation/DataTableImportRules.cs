namespace INLO.Core.DataTable.Editor
{
    public static class DataTableImportRules
    {
        public static bool IsIgnoredColumn(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
                return false;

            string trimmed = header.Trim();

            return trimmed.StartsWith("#") ||
                   trimmed.StartsWith("//");
        }

        public static bool IsIgnoredRowId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            string trimmed = id.Trim();

            return trimmed.StartsWith("#") ||
                   trimmed.StartsWith("//");
        }
    }
}
