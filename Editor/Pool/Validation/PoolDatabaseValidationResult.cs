using System.Collections.Generic;

namespace INLO.Core.Pooling.Editor
{
    public sealed class PoolDatabaseValidationResult
    {
        private readonly List<PoolDatabaseValidationMessage> _messages = new();

        public IReadOnlyList<PoolDatabaseValidationMessage> Messages => _messages;

        public bool HasError { get; private set; }
        public bool HasWarning { get; private set; }
        public bool IsValid => !HasError;

        public void Add(PoolDatabaseValidationSeverity severity, int entryIndex, string message)
        {
            _messages.Add(new PoolDatabaseValidationMessage(severity, entryIndex, message));

            if (severity == PoolDatabaseValidationSeverity.Error)
            {
                HasError = true;
            }

            if (severity == PoolDatabaseValidationSeverity.Warning)
            {
                HasWarning = true;
            }
        }
    }
}
