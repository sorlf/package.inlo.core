namespace INLO.Core.Pooling.Editor
{
    public readonly struct PoolDatabaseValidationMessage
    {
        public readonly PoolDatabaseValidationSeverity Severity;
        public readonly int EntryIndex;
        public readonly string Message;

        public PoolDatabaseValidationMessage(
            PoolDatabaseValidationSeverity severity,
            int entryIndex,
            string message)
        {
            Severity = severity;
            EntryIndex = entryIndex;
            Message = message;
        }
    }
}
