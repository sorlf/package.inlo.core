using INLO.Core.DataTable;
using System;
using System.Collections.Generic;

namespace INLO.Core.DataTable.Editor
{
    public static class GooglePublishedCsvGridReader
    {
        public static DataTableGrid Read(string csvUrl)
        {
            if (string.IsNullOrWhiteSpace(csvUrl))
                throw new ArgumentException("Published CSV URL is empty.");

            if (!IsSupportedUrl(csvUrl))
                throw new ArgumentException("Published CSV URL must start with http:// or https://.");

            string csvText = DownloadCsv(csvUrl);
            return Parse(csvText);
        }

        public static DataTableGrid Parse(string csvText)
        {
            List<List<string>> parsedRows = CsvReader.Read(csvText);

            return BuildGrid(parsedRows);
        }

        public static bool IsSupportedUrl(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
                return false;

            if (uri.Scheme != Uri.UriSchemeHttps ||
                !string.Equals(uri.Host, "docs.google.com", StringComparison.OrdinalIgnoreCase) ||
                !uri.AbsolutePath.StartsWith("/spreadsheets/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string query = uri.Query ?? string.Empty;
            return query.IndexOf("output=csv", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   query.IndexOf("tqx=out:csv", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string DownloadCsv(string csvUrl)
        {
            throw new InvalidOperationException(
                "Synchronous Google CSV download is disabled. Use GooglePublishedCsvRequest.DownloadAsync.");
        }

        private static DataTableGrid BuildGrid(List<List<string>> parsedRows)
        {
            DataTableGrid grid = new DataTableGrid();

            if (parsedRows.Count == 0)
                return grid;

            List<string> headerRow = parsedRows[0];
            int maxColumnCount = headerRow.Count;

            for (int rowIndex = 1; rowIndex < parsedRows.Count; rowIndex++)
                maxColumnCount = Math.Max(maxColumnCount, parsedRows[rowIndex].Count);

            for (int columnIndex = 0; columnIndex < maxColumnCount; columnIndex++)
            {
                string header = columnIndex < headerRow.Count
                    ? headerRow[columnIndex]
                    : string.Empty;

                grid.Headers.Add((header ?? string.Empty).Trim());
            }

            for (int rowIndex = 1; rowIndex < parsedRows.Count; rowIndex++)
            {
                List<string> parsedRow = parsedRows[rowIndex];
                List<string> cells = new List<string>();
                bool hasAnyValue = false;

                for (int columnIndex = 0; columnIndex < maxColumnCount; columnIndex++)
                {
                    string cellValue = columnIndex < parsedRow.Count
                        ? parsedRow[columnIndex] ?? string.Empty
                        : string.Empty;

                    if (!string.IsNullOrWhiteSpace(cellValue))
                        hasAnyValue = true;

                    cells.Add(cellValue);
                }

                if (!hasAnyValue)
                    continue;

                grid.Rows.Add(new DataTableGridRow(rowIndex + 1, cells));
            }

            return grid;
        }
    }
}
