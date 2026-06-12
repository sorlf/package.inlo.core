using System;
using System.Globalization;

namespace INLO.Core.DataTable
{
    public static class DataTableValueParser
    {
        public static DataTableValueType ParseType(string rawType, int columnIndex)
        {
            if (string.IsNullOrWhiteSpace(rawType))
            {
                throw new DataTableException(
                    $"DataTable schema error. Type is empty at column index {columnIndex}.");
            }

            string normalized = rawType.Trim().ToLowerInvariant();

            switch (normalized)
            {
                case "string":
                case "str":
                    return DataTableValueType.String;
                case "int":
                case "integer":
                    return DataTableValueType.Int;
                case "long":
                    return DataTableValueType.Long;
                case "float":
                    return DataTableValueType.Float;
                case "double":
                    return DataTableValueType.Double;
                case "bool":
                case "boolean":
                    return DataTableValueType.Bool;
                default:
                    throw new DataTableException(
                        $"DataTable schema error. Unsupported type '{rawType}' at column index {columnIndex}.");
            }
        }

        public static object ParseValue(
            string rawValue,
            DataTableColumn column,
            int sourceRowNumber)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));

            rawValue ??= string.Empty;
            rawValue = rawValue.Trim();

            if (rawValue.Length == 0 && column.Type != DataTableValueType.String)
            {
                throw new DataTableException(
                    $"DataTable parse failed. Row {sourceRowNumber}, Column '{column.Name}': expected {column.Type} but value is empty.");
            }

            switch (column.Type)
            {
                case DataTableValueType.String:
                    return rawValue;
                case DataTableValueType.Int:
                    return ParseInt(rawValue, column, sourceRowNumber);
                case DataTableValueType.Long:
                    return ParseLong(rawValue, column, sourceRowNumber);
                case DataTableValueType.Float:
                    return ParseFloat(rawValue, column, sourceRowNumber);
                case DataTableValueType.Double:
                    return ParseDouble(rawValue, column, sourceRowNumber);
                case DataTableValueType.Bool:
                    return ParseBool(rawValue, column, sourceRowNumber);
                default:
                    throw new DataTableException(
                        $"DataTable parse failed. Row {sourceRowNumber}, Column '{column.Name}': unsupported type {column.Type}.");
            }
        }

        private static int ParseInt(string rawValue, DataTableColumn column, int sourceRowNumber)
        {
            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                return value;

            throw CreateParseException(rawValue, column, sourceRowNumber, "int");
        }

        private static long ParseLong(string rawValue, DataTableColumn column, int sourceRowNumber)
        {
            if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
                return value;

            throw CreateParseException(rawValue, column, sourceRowNumber, "long");
        }

        private static float ParseFloat(string rawValue, DataTableColumn column, int sourceRowNumber)
        {
            if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                return value;

            throw CreateParseException(rawValue, column, sourceRowNumber, "float");
        }

        private static double ParseDouble(string rawValue, DataTableColumn column, int sourceRowNumber)
        {
            if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                return value;

            throw CreateParseException(rawValue, column, sourceRowNumber, "double");
        }

        private static bool ParseBool(string rawValue, DataTableColumn column, int sourceRowNumber)
        {
            if (rawValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                rawValue == "1" ||
                rawValue.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                rawValue.Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (rawValue.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                rawValue == "0" ||
                rawValue.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                rawValue.Equals("n", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            throw CreateParseException(rawValue, column, sourceRowNumber, "bool");
        }

        private static DataTableException CreateParseException(
            string rawValue,
            DataTableColumn column,
            int sourceRowNumber,
            string expectedType)
        {
            return new DataTableException(
                $"DataTable parse failed. Row {sourceRowNumber}, Column '{column.Name}': expected {expectedType} but got '{rawValue}'.");
        }
    }
}
