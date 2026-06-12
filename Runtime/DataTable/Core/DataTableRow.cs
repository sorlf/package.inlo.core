using System;

namespace INLO.Core.DataTable
{
    public sealed class DataTableRow
    {
        private readonly object[] values;

        internal DataTableRow(DataTableSchema schema, object[] values, int sourceRowNumber)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            this.values = values ?? throw new ArgumentNullException(nameof(values));
            SourceRowNumber = sourceRowNumber;

            if (values.Length != schema.Columns.Count)
            {
                throw new ArgumentException(
                    "DataTable row error. Value count must match schema column count.",
                    nameof(values));
            }
        }

        public DataTableSchema Schema { get; }
        public int SourceRowNumber { get; }
        public int Id => GetInt("id");
        public long LongId => GetLong("id");

        public int GetInt(string columnName)
        {
            object value = GetValue(columnName, DataTableValueType.Int);
            return value is int intValue ? intValue : Convert.ToInt32(value);
        }

        public long GetLong(string columnName)
        {
            object value = GetValue(columnName);

            if (value is long longValue)
                return longValue;

            if (value is int intValue)
                return intValue;

            throw CreateTypeMismatchException(columnName, "long");
        }

        public float GetFloat(string columnName)
        {
            object value = GetValue(columnName, DataTableValueType.Float);
            return (float)value;
        }

        public double GetDouble(string columnName)
        {
            object value = GetValue(columnName);

            if (value is double doubleValue)
                return doubleValue;

            if (value is float floatValue)
                return floatValue;

            throw CreateTypeMismatchException(columnName, "double");
        }

        public bool GetBool(string columnName)
        {
            object value = GetValue(columnName, DataTableValueType.Bool);
            return (bool)value;
        }

        public string GetString(string columnName)
        {
            object value = GetValue(columnName, DataTableValueType.String);
            return (string)value;
        }

        public T Get<T>(string columnName)
        {
            object value = GetValue(columnName);

            if (value is T typedValue)
                return typedValue;

            throw CreateTypeMismatchException(columnName, typeof(T).Name);
        }

        public bool TryGetInt(string columnName, out int value)
        {
            value = default;

            if (!TryGetValue(columnName, out object rawValue))
                return false;

            if (rawValue is int intValue)
            {
                value = intValue;
                return true;
            }

            return false;
        }

        public bool TryGetString(string columnName, out string value)
        {
            value = default;

            if (!TryGetValue(columnName, out object rawValue))
                return false;

            if (rawValue is string stringValue)
            {
                value = stringValue;
                return true;
            }

            return false;
        }

        public bool TryGetValue(string columnName, out object value)
        {
            value = null;

            if (!Schema.TryGetColumn(columnName, out DataTableColumn column))
                return false;

            value = values[column.Index];
            return true;
        }

        public object GetValue(string columnName)
        {
            DataTableColumn column = Schema.GetColumn(columnName);
            return values[column.Index];
        }

        private object GetValue(string columnName, DataTableValueType expectedType)
        {
            DataTableColumn column = Schema.GetColumn(columnName);

            if (column.Type != expectedType)
                throw CreateTypeMismatchException(columnName, expectedType.ToString());

            return values[column.Index];
        }

        private DataTableException CreateTypeMismatchException(string columnName, string expectedType)
        {
            DataTableColumn column = Schema.GetColumn(columnName);

            return new DataTableException(
                $"DataTable row error. Row {SourceRowNumber}, Column '{column.Name}': expected {expectedType} but column type is {column.Type}.");
        }
    }
}
