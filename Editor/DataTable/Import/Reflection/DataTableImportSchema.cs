using INLO.Core.DataTable;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace INLO.Core.DataTable.Editor
{
    public sealed class DataTableImportField
    {
        public DataTableImportField(FieldInfo field, bool required)
        {
            Field = field;
            Required = required;
        }

        public FieldInfo Field { get; }
        public string Name => Field.Name;
        public Type ValueType => Field.FieldType;
        public bool Required { get; }
    }

    public sealed class DataTableImportSchema
    {
        private const BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly Dictionary<string, DataTableImportField> fieldsByName;

        private DataTableImportSchema(
            Type rowType,
            List<DataTableImportField> fields,
            Dictionary<string, DataTableImportField> fieldsByName)
        {
            RowType = rowType;
            Fields = fields;
            this.fieldsByName = fieldsByName;
        }

        public Type RowType { get; }
        public IReadOnlyList<DataTableImportField> Fields { get; }

        public bool TryGetField(string name, out DataTableImportField field)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                field = null;
                return false;
            }

            return fieldsByName.TryGetValue(name.Trim(), out field);
        }

        public static bool TryCreate(
            Type rowType,
            List<DataTableValidationError> errors,
            out DataTableImportSchema schema)
        {
            schema = null;

            if (rowType == null)
            {
                AddSchemaError(errors, string.Empty, "Row type is null.");
                return false;
            }

            if (!typeof(IDataTableRow).IsAssignableFrom(rowType))
            {
                AddSchemaError(
                    errors,
                    rowType.Name,
                    $"Row type must implement {nameof(IDataTableRow)}.");
                return false;
            }

            if (rowType.IsAbstract || rowType.GetConstructor(Type.EmptyTypes) == null)
            {
                AddSchemaError(
                    errors,
                    rowType.Name,
                    "Row type must be a non-abstract class with a public parameterless constructor.");
                return false;
            }

            List<DataTableImportField> fields = new();
            Dictionary<string, DataTableImportField> fieldsByName =
                new(StringComparer.OrdinalIgnoreCase);

            FieldInfo[] reflectedFields = rowType.GetFields(FieldFlags);

            for (int i = 0; i < reflectedFields.Length; i++)
            {
                FieldInfo field = reflectedFields[i];

                if (!IsImportableField(field))
                    continue;

                bool hasRequired = field.GetCustomAttribute<DataTableRequiredAttribute>() != null;
                bool hasOptional = field.GetCustomAttribute<DataTableOptionalAttribute>() != null;

                if (hasRequired && hasOptional)
                {
                    AddSchemaError(
                        errors,
                        field.Name,
                        $"Field '{field.Name}' cannot use both DataTableRequired and DataTableOptional.");
                    continue;
                }

                bool required = IsRequired(field, hasRequired, hasOptional);
                DataTableImportField importField = new(field, required);

                if (!fieldsByName.TryAdd(field.Name, importField))
                {
                    AddSchemaError(
                        errors,
                        field.Name,
                        $"Duplicate import field name found: {field.Name}");
                    continue;
                }

                fields.Add(importField);
            }

            if (!fieldsByName.ContainsKey("id"))
            {
                AddSchemaError(errors, "id", "Row type must contain an importable field named 'id'.");
            }

            if (errors != null && errors.Count > 0)
                return false;

            schema = new DataTableImportSchema(rowType, fields, fieldsByName);
            return true;
        }

        private static bool IsImportableField(FieldInfo field)
        {
            if (field == null || field.IsStatic || field.IsInitOnly || field.IsNotSerialized)
                return false;

            if (field.GetCustomAttribute<NonSerializedAttribute>() != null ||
                field.GetCustomAttribute<DataTableIgnoreAttribute>() != null)
            {
                return false;
            }

            return field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
        }

        private static bool IsRequired(
            FieldInfo field,
            bool hasRequired,
            bool hasOptional)
        {
            if (string.Equals(field.Name, "id", StringComparison.OrdinalIgnoreCase))
                return true;

            if (hasRequired)
                return true;

            if (hasOptional)
                return false;

            Type fieldType = field.FieldType;
            return fieldType != typeof(string) &&
                   Nullable.GetUnderlyingType(fieldType) == null &&
                   fieldType.IsValueType;
        }

        private static void AddSchemaError(
            List<DataTableValidationError> errors,
            string fieldName,
            string message)
        {
            errors?.Add(
                new DataTableValidationError(
                    DataTableValidationErrorType.Unknown,
                    1,
                    fieldName,
                    message));
        }
    }
}
