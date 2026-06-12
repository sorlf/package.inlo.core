namespace INLO.Core.Editor.Bootstrap
{
    public readonly struct BootstrapUiValidationIssue
    {
        public BootstrapUiValidationIssue(BootstrapUiValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }

        public BootstrapUiValidationSeverity Severity { get; }
        public string Message { get; }
    }
}
