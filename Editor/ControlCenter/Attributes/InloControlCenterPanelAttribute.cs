using System;

namespace INLO.Core.EditorUI.Editor
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class InloControlCenterPanelAttribute : Attribute
    {
        public string DisplayName { get; }
        public string Icon { get; }
        public int Order { get; }
        public string ElementName { get; }

        public InloControlCenterPanelAttribute(string displayName, string icon, string elementName, int order = 100)
        {
            DisplayName = displayName;
            Icon = icon;
            ElementName = elementName;
            Order = order;
        }
    }
}
