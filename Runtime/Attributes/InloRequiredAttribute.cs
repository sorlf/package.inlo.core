using System;

namespace INLO.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class InloRequiredAttribute : Attribute
    {
        public string Message { get; }

        public InloRequiredAttribute(string message = "This field is required and cannot be null.")
        {
            Message = message;
        }
    }
}
