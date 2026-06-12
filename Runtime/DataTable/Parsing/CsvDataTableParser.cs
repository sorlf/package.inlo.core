using System.Collections.Generic;

namespace INLO.Core.DataTable
{
    public static class CsvDataTableParser
    {
        public static DataTable Parse(string csvText)
        {
            return Parse(csvText, "DataTable");
        }

        public static DataTable Parse(string csvText, string tableName)
        {
            List<List<string>> csvRows = CsvReader.Read(csvText);

            if (csvRows.Count < 2)
            {
                throw new DataTableException(
                    $"{tableName}: DataTable parse failed. CSV must contain column names on row 1 and types on row 2.");
            }

            DataTableSchema schema = BuildSchema(csvRows[0], csvRows[1], tableName);
            List<DataTableRow> rows = new();

            for (int rowIndex = 2; rowIndex < csvRows.Count; rowIndex++)
            {
                rows.Add(BuildRow(schema, csvRows[rowIndex], rowIndex + 1, tableName));
            }

            return new DataTable(schema, rows);
        }

        private static DataTableSchema BuildSchema(
            List<string> columnNames,
            List<string> columnTypes,
            string tableName)
        {
            if (columnNames == null || columnNames.Count == 0)
                throw new DataTableException($"{tableName}: DataTable schema error. Header row is empty.");

            if (columnTypes == null || columnTypes.Count < columnNames.Count)
            {
                throw new DataTableException(
                    $"{tableName}: DataTable schema error. Type row must contain a type for every column.");
            }

            List<DataTableColumn> columns = new();

            for (int i = 0; i < columnNames.Count; i++)
            {
                string columnName = columnNames[i];

                if (string.IsNullOrWhiteSpace(columnName))
                {
                    throw new DataTableException(
                        $"{tableName}: DataTable schema error. Column name is empty at index {i}.");
                }

                DataTableValueType type = DataTableValueParser.ParseType(columnTypes[i], i);
                columns.Add(new DataTableColumn(columnName, type, i));
            }

            DataTableSchema schema = new(columns);

            if (!schema.HasIdColumn)
                throw new DataTableException($"{tableName}: DataTable schema error. Required column is missing: id.");

            return schema;
        }

        private static DataTableRow BuildRow(
            DataTableSchema schema,
            List<string> csvRow,
            int sourceRowNumber,
            string tableName)
        {
            object[] values = new object[schema.Columns.Count];

            for (int i = 0; i < schema.Columns.Count; i++)
            {
                DataTableColumn column = schema.Columns[i];
                string rawValue = i < csvRow.Count ? csvRow[i] : string.Empty;
                values[i] = DataTableValueParser.ParseValue(rawValue, column, sourceRowNumber);
            }

            for (int i = schema.Columns.Count; i < csvRow.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(csvRow[i]))
                {
                    throw new DataTableException(
                        $"{tableName}: DataTable parse failed. Row {sourceRowNumber} has a value outside the schema at column index {i}.");
                }
            }

            return new DataTableRow(schema, values, sourceRowNumber);
        }
    }
}
