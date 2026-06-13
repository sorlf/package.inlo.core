using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using INLO.Core.EditorUI.Editor;
using INLO.Core.Pooling.Editor;
using Object = UnityEngine.Object;

namespace INLO.Core.EditorUI.Editor
{
    public sealed class InloControlCenterWindow : InloBaseEditorWindow, IPoolWindow
    {
        private const string WindowUssPath = "Packages/com.inlo.core/Editor/ControlCenter/USS/InloControlCenterWindow.uss";
        private const string WindowUxmlPath = "Packages/com.inlo.core/Editor/ControlCenter/UXML/InloControlCenterWindow.uxml";

        protected override string UxmlPath => WindowUxmlPath;
        protected override string UssPath => WindowUssPath;
        protected override string MainScrollViewName => string.Empty;

        // Dynamic Panels list
        private readonly List<(IControlCenterPanel panel, InloControlCenterPanelAttribute attr, Button button, VisualElement container)> registeredPanels = new();
        private string activePanelElementName = "panel-dashboard";

        [MenuItem("Tools/INLO/INLO Control Center", false, 0)]
        public static void Open()
        {
            InloControlCenterWindow window = GetWindow<InloControlCenterWindow>("INLO Control Center");
            window.minSize = new Vector2(1040f, 680f);
            window.Show();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // Solution C: Restore state across domain reloads
            activePanelElementName = SessionState.GetString("INLO_CC_ActivePanel", "panel-dashboard");
        }

        protected override void OnDisable()
        {
            foreach (var entry in registeredPanels)
            {
                entry.panel.OnPanelDisabled();
            }
            base.OnDisable();
        }

        protected override void OnBindElements()
        {
            registeredPanels.Clear();

            // Clear the static sidebar buttons from UXML and we will spawn them dynamically
            VisualElement sidebarMenu = Require<VisualElement>("cc-sidebar-menu");
            sidebarMenu.Clear();

            // Solution A: Scan assemblies dynamically for [InloControlCenterPanelAttribute]
            var panelTypes = new List<(Type type, InloControlCenterPanelAttribute attr)>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.GetName().Name.StartsWith("INLO.Core")) continue;

                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(IControlCenterPanel).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    {
                        var attr = (InloControlCenterPanelAttribute)Attribute.GetCustomAttribute(type, typeof(InloControlCenterPanelAttribute));
                        if (attr != null)
                        {
                            panelTypes.Add((type, attr));
                        }
                    }
                }
            }

            // Sort by order defined in attribute
            panelTypes.Sort((a, b) => a.attr.Order.CompareTo(b.attr.Order));

            // Instantiate and bind panels
            foreach (var (type, attr) in panelTypes)
            {
                try
                {
                    IControlCenterPanel panelInstance = (IControlCenterPanel)Activator.CreateInstance(type);
                    VisualElement container = Require<VisualElement>(attr.ElementName);

                    // Create Dynamic Sidebar Button
                    Button button = new Button();
                    button.AddToClassList("cc-menu-btn");
                    button.AddToClassList("cc-menu-btn--idle");

                    VisualElement iconEl = new VisualElement();
                    iconEl.AddToClassList("cc-menu-icon-element");
                    Texture2D iconTex = EditorGUIUtility.IconContent(attr.Icon)?.image as Texture2D;
                    if (iconTex != null)
                    {
                        iconEl.style.backgroundImage = new StyleBackground(iconTex);
                    }
                    button.Add(iconEl);

                    Label textLabel = new Label(attr.DisplayName);
                    textLabel.AddToClassList("cc-menu-text");
                    button.Add(textLabel);

                    button.clicked += () => SwitchToTab(attr.ElementName);
                    sidebarMenu.Add(button);

                    panelInstance.Initialize(this, container);
                    registeredPanels.Add((panelInstance, attr, button, container));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ControlCenter] Failed to initialize dynamic panel '{type.Name}': {ex.Message}");
                }
            }

            // If restored active panel doesn't exist, fall back to first panel
            bool exists = registeredPanels.Exists(p => p.attr.ElementName == activePanelElementName);
            if (!exists && registeredPanels.Count > 0)
            {
                activePanelElementName = registeredPanels[0].attr.ElementName;
            }

            // Switch to the active tab to initialize its visual states
            SwitchToTab(activePanelElementName);
        }

        public override void UpdateUI()
        {
            foreach (var entry in registeredPanels)
            {
                bool isActive = entry.attr.ElementName == activePanelElementName;

                // Update tab visibility
                entry.container.EnableInClassList("cc-panel--active", isActive);
                entry.container.EnableInClassList("cc-panel--inactive", !isActive);

                // Update sidebar button classes
                entry.button.EnableInClassList("cc-menu-btn--selected", isActive);
                entry.button.EnableInClassList("cc-menu-btn--idle", !isActive);

                // Let the panel update its UI
                entry.panel.UpdateUI();
            }
        }

        // ===== IPoolWindow implementation =====
        public void SelectBrowserTabWithSource(Object source)
        {
            SwitchToTab("panel-pool");
            foreach (var entry in registeredPanels)
            {
                if (entry.panel is PoolManagerPanel poolPanel)
                {
                    poolPanel.SelectBrowserTabWithSource(source);
                    break;
                }
            }
        }

        // ===== Tab Switching API =====
        public void SwitchToTab(string elementName)
        {
            string oldElementName = activePanelElementName;
            activePanelElementName = elementName;

            // Save active panel across sessions
            SessionState.SetString("INLO_CC_ActivePanel", elementName);

            // Handle panel transition lifecycles
            foreach (var entry in registeredPanels)
            {
                if (entry.attr.ElementName == oldElementName)
                {
                    entry.panel.OnPanelDisabled();
                }
                if (entry.attr.ElementName == elementName)
                {
                    entry.panel.OnPanelEnabled();
                }
            }

            UpdateUI();
        }

        // ===== Utility Hub Interface API =====
        public void RunCodeConventionAudit()
        {
            foreach (var entry in registeredPanels)
            {
                if (entry.panel is ControlAuditPanel auditPanel)
                {
                    auditPanel.RunCodeConventionAudit();
                    break;
                }
            }
        }

        private T Require<T>(string elementName) where T : VisualElement
        {
            T element = rootVisualElement.Q<T>(elementName);
            if (element == null)
                throw new MissingReferenceException($"[ControlCenter] Required UXML element missing: '{elementName}'");
            return element;
        }
    }
}
