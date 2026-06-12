using System;

namespace INLO.Core.DataTable
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DataTableIgnoreAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DataTableOptionalAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DataTableRequiredAttribute : Attribute
    {
    }
}
