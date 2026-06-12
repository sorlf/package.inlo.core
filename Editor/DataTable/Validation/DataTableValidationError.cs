namespace INLO.Core.DataTable.Editor
{
    public enum DataTableValidationErrorType
    {
        EmptyId,
        DuplicateId,
        MissingColumn,
        MissingField,
        UnknownColumn,
        InvalidType,
        EmptyRequiredValue,
        Unknown
    }

    public readonly struct DataTableValidationError
    {
        public readonly DataTableValidationErrorType Type;
        public readonly int RowIndex;
        public readonly string ColumnName;
        public readonly string Message;

        public DataTableValidationError(
            DataTableValidationErrorType type,
            int rowIndex,
            string columnName,
            string message)
        {
            Type = type;
            RowIndex = rowIndex;
            ColumnName = columnName;
            Message = message;
        }

        public override string ToString()
        {
            return $"[{Type}] Row: {RowIndex}, Column: {ColumnName}, Message: {Message}";
        }
    }
}
