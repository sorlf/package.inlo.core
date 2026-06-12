using System.Collections.Generic;

namespace INLO.Core.DataTable.Editor
{
    public sealed class DataTableCodeGenerationPlan
    {
        public readonly List<string> Errors = new();

        public string BaseName { get; set; }
        public string Namespace { get; set; }
        public string OutputFolder { get; set; }
        public string AssemblyName { get; set; }
        public string RowPath { get; set; }
        public string TablePath { get; set; }
        public string RowCode { get; set; }
        public string TableCode { get; set; }

        public bool CanApply =>
            Errors.Count == 0 &&
            !string.IsNullOrWhiteSpace(RowPath) &&
            !string.IsNullOrWhiteSpace(TablePath);
    }
}
