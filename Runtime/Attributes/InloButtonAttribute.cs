using System;

namespace INLO.Core.Attributes
{
    public enum ButtonColor
    {
        Default,
        Accent,
        Danger
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class InloButtonAttribute : Attribute
    {
        public string DisplayName { get; }
        public ButtonColor Color { get; }

        public InloButtonAttribute(string displayName = null, ButtonColor color = ButtonColor.Accent)
        {
            DisplayName = displayName;
            Color = color;
        }
    }
}
