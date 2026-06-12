using System;
using System.Collections.Generic;

namespace INLO.Core.DataTable.Editor
{
    public static class DataTableSchemaValidator
    {
        public static void Validate(
            DataTableGrid grid,
            Type rowType,
            List<DataTableValidationError> errors)
        {
            if (!DataTableImportSchema.TryCreate(rowType, errors, out DataTableImportSchema schema))
                return;

            Validate(grid, schema, errors);
        }

        public static void Validate(
            DataTableGrid grid,
            DataTableImportSchema schema,
            List<DataTableValidationError> errors)
        {
            if (grid == null)
            {
                errors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.Unknown,
                        -1,
                        string.Empty,
                        "DataTable grid is null."));
                return;
            }

            if (schema == null)
            {
                errors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.Unknown,
                        -1,
                        string.Empty,
                        "Import schema is null."));
                return;
            }

            HashSet<string> headerSet = new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < grid.Headers.Count; i++)
            {
                string header = grid.Headers[i];

                if (string.IsNullOrWhiteSpace(header) ||
                    DataTableImportRules.IsIgnoredColumn(header))
                {
                    continue;
                }

                string normalizedHeader = header.Trim();
                headerSet.Add(normalizedHeader);

                if (!schema.TryGetField(normalizedHeader, out _))
                {
                    errors.Add(
                        new DataTableValidationError(
                            DataTableValidationErrorType.UnknownColumn,
                            1,
                            normalizedHeader,
                            $"Column '{normalizedHeader}' exists in the source table, but no matching field exists in the Row type."));
                }
            }

            for (int i = 0; i < schema.Fields.Count; i++)
            {
                DataTableImportField field = schema.Fields[i];

                if (!field.Required || headerSet.Contains(field.Name))
                    continue;

                errors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.MissingField,
                        1,
                        field.Name,
                        $"Required field '{field.Name}' exists in the Row type, but no matching column exists in the source table."));
            }
        }
    }
}
