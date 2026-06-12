using System.Collections.Generic;

namespace INLO.Core.DataTable
{
    public static class CsvReader
    {
        public static List<List<string>> Read(string csvText)
        {
            List<List<string>> rows = new();

            if (string.IsNullOrEmpty(csvText))
                return rows;

            List<string> row = new();
            System.Text.StringBuilder cell = new();
            bool inQuotes = false;

            for (int i = 0; i < csvText.Length; i++)
            {
                char current = csvText[i];

                if (current == '"')
                {
                    if (inQuotes && i + 1 < csvText.Length && csvText[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (!inQuotes && current == ',')
                {
                    AddCell(row, cell);
                    continue;
                }

                if (!inQuotes && (current == '\r' || current == '\n'))
                {
                    AddCell(row, cell);
                    AddRow(rows, row);
                    row = new List<string>();

                    if (current == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                        i++;

                    continue;
                }

                cell.Append(current);
            }

            if (inQuotes)
                throw new DataTableException("CSV parse failed. Quoted field is not closed.");

            AddCell(row, cell);
            AddRow(rows, row);

            return rows;
        }

        private static void AddCell(List<string> row, System.Text.StringBuilder cell)
        {
            row.Add(cell.ToString().Trim());
            cell.Clear();
        }

        private static void AddRow(List<List<string>> rows, List<string> row)
        {
            bool hasAnyValue = false;

            for (int i = 0; i < row.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(row[i]))
                {
                    hasAnyValue = true;
                    break;
                }
            }

            if (hasAnyValue)
                rows.Add(row);
        }
    }
}
