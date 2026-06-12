using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace INLO.Core.DataTable.Editor
{
    public static class DataTableRowMapper
    {
        private const BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static IList MapRows(
            DataTableGrid grid,
            Type rowType,
            List<DataTableValidationError> errors)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));

            if (rowType == null)
                throw new ArgumentNullException(nameof(rowType));

            if (errors == null)
                throw new ArgumentNullException(nameof(errors));

            if (!DataTableImportSchema.TryCreate(rowType, errors, out DataTableImportSchema schema))
                return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(rowType));

            return MapRows(grid, schema, errors);
        }

        public static IList MapRows(
            DataTableGrid grid,
            DataTableImportSchema schema,
            List<DataTableValidationError> errors)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));

            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            if (errors == null)
                throw new ArgumentNullException(nameof(errors));

            Type rowType = schema.RowType;
            Type listType = typeof(List<>).MakeGenericType(rowType);
            IList rows = (IList)Activator.CreateInstance(listType);

            Dictionary<int, DataTableImportField> columnMap =
                BuildColumnMap(grid, schema, errors);

            if (errors.Count > 0)
                return rows;

            int idColumnIndex = FindIdColumnIndex(grid);
            Dictionary<string, int> idSourceRows = new(StringComparer.OrdinalIgnoreCase);

            for (int rowIndex = 0; rowIndex < grid.Rows.Count; rowIndex++)
            {
                DataTableGridRow gridRow = grid.Rows[rowIndex];

                if (ShouldSkipRow(gridRow, idColumnIndex))
                    continue;

                object rowInstance = Activator.CreateInstance(rowType);

                foreach (KeyValuePair<int, DataTableImportField> pair in columnMap)
                {
                    int columnIndex = pair.Key;
                    DataTableImportField importField = pair.Value;
                    FieldInfo field = importField.Field;

                    string rawValue = gridRow.GetCell(columnIndex);

                    if (string.Equals(field.Name, "id", StringComparison.OrdinalIgnoreCase))
                        rawValue = rawValue?.Trim() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(rawValue))
                    {
                        if (importField.Required)
                        {
                            errors.Add(
                                new DataTableValidationError(
                                    DataTableValidationErrorType.EmptyRequiredValue,
                                    gridRow.SourceRowNumber,
                                    field.Name,
                                    $"Required value is empty. Field: {field.Name}"));
                        }

                        field.SetValue(
                            rowInstance,
                            CreateEmptyValue(field.FieldType));
                        continue;
                    }

                    if (!TryConvertValue(rawValue, field.FieldType, out object convertedValue, out string errorMessage))
                    {
                        errors.Add(
                            new DataTableValidationError(
                                DataTableValidationErrorType.InvalidType,
                                gridRow.SourceRowNumber,
                                field.Name,
                                $"Invalid value. Field: {field.Name}, Value: {rawValue}, Reason: {errorMessage}"));

                        continue;
                    }

                    field.SetValue(rowInstance, convertedValue);
                }

                if (rowInstance is IDataTableRow tableRow)
                {
                    string id = tableRow.Id?.Trim();

                    if (string.IsNullOrWhiteSpace(id))
                    {
                        errors.Add(
                            new DataTableValidationError(
                                DataTableValidationErrorType.EmptyId,
                                gridRow.SourceRowNumber,
                                "id",
                                "Id is empty."));

                        continue;
                    }

                    if (idSourceRows.TryGetValue(id, out int firstSourceRowNumber))
                    {
                        errors.Add(
                            new DataTableValidationError(
                                DataTableValidationErrorType.DuplicateId,
                                gridRow.SourceRowNumber,
                                "id",
                                $"Duplicate id found: {id}. First occurrence is at row {firstSourceRowNumber}."));

                        continue;
                    }

                    idSourceRows.Add(id, gridRow.SourceRowNumber);
                }

                rows.Add(rowInstance);
            }

            return rows;
        }

        private static bool ShouldSkipRow(
            DataTableGridRow row,
            int idColumnIndex)
        {
            if (row == null)
                return true;

            if (idColumnIndex < 0)
                return false;

            string id = row.GetCell(idColumnIndex);

            return DataTableImportRules.IsIgnoredRowId(id);
        }

        private static int FindIdColumnIndex(DataTableGrid grid)
        {
            for (int i = 0; i < grid.Headers.Count; i++)
            {
                string header = grid.Headers[i];

                if (string.Equals(header, "id", StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static Dictionary<int, DataTableImportField> BuildColumnMap(
            DataTableGrid grid,
            DataTableImportSchema schema,
            List<DataTableValidationError> errors)
        {
            Dictionary<int, DataTableImportField> result = new();

            for (int columnIndex = 0; columnIndex < grid.Headers.Count; columnIndex++)
            {
                string header = grid.Headers[columnIndex];

                if (string.IsNullOrWhiteSpace(header))
                    continue;

                if (DataTableImportRules.IsIgnoredColumn(header))
                    continue;

                if (!schema.TryGetField(header, out DataTableImportField field))
                    continue;

                result[columnIndex] = field;
            }

            if (!ContainsIdField(result))
            {
                errors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.MissingColumn,
                        1,
                        "id",
                        "Required field column is missing: id"));
            }

            return result;
        }

        private static bool ContainsIdField(Dictionary<int, DataTableImportField> columnMap)
        {
            foreach (DataTableImportField field in columnMap.Values)
            {
                if (string.Equals(field.Name, "id", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static object CreateEmptyValue(Type fieldType)
        {
            if (Nullable.GetUnderlyingType(fieldType) != null)
                return null;

            if (fieldType == typeof(string))
                return string.Empty;

            return fieldType.IsValueType
                ? Activator.CreateInstance(fieldType)
                : null;
        }

        private static bool TryConvertValue(
            string rawValue,
            Type targetType,
            out object convertedValue,
            out string errorMessage)
        {
            convertedValue = null;
            errorMessage = string.Empty;

            try
            {
                Type nullableUnderlyingType = Nullable.GetUnderlyingType(targetType);

                if (nullableUnderlyingType != null)
                {
                    targetType = nullableUnderlyingType;
                }

                if (targetType == typeof(string))
                {
                    convertedValue = rawValue;
                    return true;
                }

                rawValue = rawValue.Trim();

                if (targetType == typeof(int))
                {
                    return TryConvertInt(rawValue, out convertedValue, out errorMessage);
                }

                if (targetType == typeof(long))
                {
                    return TryConvertLong(rawValue, out convertedValue, out errorMessage);
                }

                if (targetType == typeof(float))
                {
                    return TryConvertFloat(rawValue, out convertedValue, out errorMessage);
                }

                if (targetType == typeof(double))
                {
                    return TryConvertDouble(rawValue, out convertedValue, out errorMessage);
                }

                if (targetType == typeof(bool))
                {
                    if (TryParseBool(rawValue, out bool value))
                    {
                        convertedValue = value;
                        return true;
                    }

                    errorMessage = "Expected bool. Allowed: true, false, 1, 0, yes, no, y, n, on, off.";
                    return false;
                }

                if (targetType.IsEnum)
                {
                    if (Enum.TryParse(targetType, rawValue, true, out object enumValue) &&
                        Enum.IsDefined(targetType, enumValue))
                    {
                        convertedValue = enumValue;
                        return true;
                    }

                    string allowedValues = string.Join(", ", Enum.GetNames(targetType));
                    errorMessage = $"Expected enum value of {targetType.Name}. Allowed: {allowedValues}";
                    return false;
                }

                errorMessage = $"Unsupported field type: {targetType.Name}";
                return false;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        private static bool TryConvertInt(
            string rawValue,
            out object convertedValue,
            out string errorMessage)
        {
            convertedValue = null;
            errorMessage = string.Empty;

            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            {
                convertedValue = intValue;
                return true;
            }

            if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
            {
                errorMessage = "Expected int.";
                return false;
            }

            if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
            {
                errorMessage = "Expected finite int.";
                return false;
            }

            double roundedValue = Math.Round(doubleValue);

            if (Math.Abs(doubleValue - roundedValue) > 0.000001d)
            {
                errorMessage = "Expected int. Decimal values like 50.5 are not allowed.";
                return false;
            }

            if (roundedValue < int.MinValue || roundedValue > int.MaxValue)
            {
                errorMessage = $"Expected int between {int.MinValue} and {int.MaxValue}.";
                return false;
            }

            convertedValue = (int)roundedValue;
            return true;
        }

        private static bool TryConvertLong(
            string rawValue,
            out object convertedValue,
            out string errorMessage)
        {
            convertedValue = null;
            errorMessage = string.Empty;

            if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
            {
                convertedValue = longValue;
                return true;
            }

            if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
            {
                errorMessage = "Expected long.";
                return false;
            }

            if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
            {
                errorMessage = "Expected finite long.";
                return false;
            }

            double roundedValue = Math.Round(doubleValue);

            if (Math.Abs(doubleValue - roundedValue) > 0.000001d)
            {
                errorMessage = "Expected long. Decimal values like 50.5 are not allowed.";
                return false;
            }

            if (roundedValue < long.MinValue || roundedValue > long.MaxValue)
            {
                errorMessage = $"Expected long between {long.MinValue} and {long.MaxValue}.";
                return false;
            }

            convertedValue = (long)roundedValue;
            return true;
        }

        private static bool TryConvertFloat(
            string rawValue,
            out object convertedValue,
            out string errorMessage)
        {
            convertedValue = null;
            errorMessage = string.Empty;

            if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                convertedValue = value;
                return true;
            }

            errorMessage = "Expected float.";
            return false;
        }

        private static bool TryConvertDouble(
            string rawValue,
            out object convertedValue,
            out string errorMessage)
        {
            convertedValue = null;
            errorMessage = string.Empty;

            if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                convertedValue = value;
                return true;
            }

            errorMessage = "Expected double.";
            return false;
        }

        private static bool TryParseBool(string rawValue, out bool value)
        {
            value = false;

            if (bool.TryParse(rawValue, out value))
                return true;

            if (rawValue == "1" ||
                rawValue.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                rawValue.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                rawValue.Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (rawValue == "0" ||
                rawValue.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                rawValue.Equals("n", StringComparison.OrdinalIgnoreCase) ||
                rawValue.Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            return false;
        }

        private static void ValidateDuplicateIds(IList rows, List<DataTableValidationError> errors)
        {
            HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < rows.Count; i++)
            {
                object row = rows[i];

                if (row is not IDataTableRow tableRow)
                    continue;

                string id = tableRow.Id;

                if (string.IsNullOrWhiteSpace(id))
                {
                    errors.Add(
                        new DataTableValidationError(
                            DataTableValidationErrorType.EmptyId,
                            i + 2,
                            "id",
                            "Id is empty."));

                    continue;
                }

                if (!ids.Add(id))
                {
                    errors.Add(
                        new DataTableValidationError(
                            DataTableValidationErrorType.DuplicateId,
                            i + 2,
                            "id",
                            $"Duplicate id found: {id}"));
                }
            }
        }
    }
}
