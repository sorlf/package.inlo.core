using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using INLO.Core.Attributes;
using Object = UnityEngine.Object;

namespace INLO.Core.Inspector.Editor
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    [CanEditMultipleObjects]
    public class InloInspectorEditor : UnityEditor.Editor
    {
        private const string CommonStylePath = "Packages/com.inlo.core/Editor/EditorUI/USS/InloWindowCommon.uss";

        // Performance Caching: Type-based caching of attributes to ensure zero GC per frame
        private static readonly Dictionary<Type, List<(FieldInfo field, InloRequiredAttribute attr)>> RequiredFieldsCache = new();
        private static readonly Dictionary<Type, List<(MethodInfo method, InloButtonAttribute attr)>> ButtonsCache = new();

        public override VisualElement CreateInspectorGUI()
        {
            Type targetType = target.GetType();

            // Cache attributes if not already cached
            CacheTargetAttributes(targetType);

            var requiredFields = RequiredFieldsCache[targetType];
            var buttons = ButtonsCache[targetType];

            // If there are no custom attributes, fallback to Unity's default inspector drawing
            if (requiredFields.Count == 0 && buttons.Count == 0)
            {
                return null;
            }

            // Create main container
            VisualElement root = new VisualElement();
            root.AddToClassList("inlo-window-root");

            // Apply light/dark mode theme wrapper class
            bool isDark = EditorGUIUtility.isProSkin;
            root.AddToClassList(isDark ? "inlo-theme-orchid-dark" : "inlo-theme-orchid-light");

            // Load and append shared stylesheet
            StyleSheet commonStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(CommonStylePath);
            if (commonStyle != null)
            {
                root.styleSheets.Add(commonStyle);
            }

            // Draw default serialized properties using modern UI Toolkit API
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            // 1. Post-process Required Fields Validation
            ApplyRequiredFieldValidation(root, requiredFields);

            // 2. Append Custom Method Buttons at the bottom
            if (buttons.Count > 0)
            {
                VisualElement buttonGroup = new VisualElement();
                buttonGroup.AddToClassList("inlo-card");
                buttonGroup.AddToClassList("inlo-inspector-button-group");

                // Add Point Label / Section Title
                Label sectionTitle = new Label("INLO Inspector Actions");
                sectionTitle.AddToClassList("inlo-card-title");
                sectionTitle.AddToClassList("inlo-inspector-action-title");
                buttonGroup.Add(sectionTitle);

                foreach (var (method, attr) in buttons)
                {
                    string label = attr.DisplayName ?? ObjectNames.NicifyVariableName(method.Name);
                    Button btn = new Button(() => ExecuteMethod(method))
                    {
                        text = label
                    };

                    btn.AddToClassList("inlo-button");
                    btn.AddToClassList("inlo-inspector-btn");

                    // Set button style depending on Color enum
                    if (attr.Color == ButtonColor.Accent)
                    {
                        btn.AddToClassList("inlo-button--accent");
                    }
                    else if (attr.Color == ButtonColor.Danger)
                    {
                        btn.AddToClassList("inlo-button--danger");
                    }

                    buttonGroup.Add(btn);
                }

                root.Add(buttonGroup);
            }

            return root;
        }

        private void CacheTargetAttributes(Type type)
        {
            if (RequiredFieldsCache.ContainsKey(type)) return;

            // Scan Fields
            var requiredList = new List<(FieldInfo, InloRequiredAttribute)>();
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attr = field.GetCustomAttribute<InloRequiredAttribute>();
                if (attr != null)
                {
                    requiredList.Add((field, attr));
                }
            }
            RequiredFieldsCache[type] = requiredList;

            // Scan Methods
            var buttonList = new List<(MethodInfo, InloButtonAttribute)>();
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attr = method.GetCustomAttribute<InloButtonAttribute>();
                if (attr != null)
                {
                    // Buttons must not have parameters
                    if (method.GetParameters().Length == 0)
                    {
                        buttonList.Add((method, attr));
                    }
                    else
                    {
                        Debug.LogWarning($"[InloInspector] Method '{method.Name}' in '{type.Name}' has InloButton attribute but requires parameters. Skipping button creation.");
                    }
                }
            }
            ButtonsCache[type] = buttonList;
        }

        private void ApplyRequiredFieldValidation(VisualElement root, List<(FieldInfo field, InloRequiredAttribute attr)> requiredFields)
        {
            foreach (var (field, attr) in requiredFields)
            {
                string propertyPath = field.Name;
                SerializedProperty prop = serializedObject.FindProperty(propertyPath);

                if (prop == null) continue;

                // Query for the auto-generated PropertyField matching this property path
                PropertyField propField = root.Q<PropertyField>(null, PropertyField.ussClassName);
                if (propField == null) continue;

                // Let's find the exact matching PropertyField by walking the children if simple Q isn't specific enough
                PropertyField targetField = null;
                root.Query<PropertyField>().ForEach(pf =>
                {
                    if (pf.bindingPath == propertyPath)
                    {
                        targetField = pf;
                    }
                });

                if (targetField == null) continue;

                // Create a premium validation wrapper and alert helpbox
                VisualElement alertBox = new VisualElement();
                alertBox.AddToClassList("inlo-required-alert");
                
                Label alertLabel = new Label(attr.Message);
                alertLabel.AddToClassList("inlo-required-alert-text");
                alertBox.Add(alertLabel);

                // Insert the alertBox right under the property field
                int idx = targetField.parent.IndexOf(targetField);
                targetField.parent.Insert(idx + 1, alertBox);

                // Register tracking and validation loop
                root.TrackPropertyValue(prop, p => ValidateProperty(p, targetField, alertBox));
                
                // Perform initial validation
                ValidateProperty(prop, targetField, alertBox);
            }
        }

        private void ValidateProperty(SerializedProperty property, PropertyField fieldElement, VisualElement alertBox)
        {
            bool hasMissingReference = false;

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                hasMissingReference = property.objectReferenceValue == null;
            }
            else if (property.propertyType == SerializedPropertyType.String)
            {
                hasMissingReference = string.IsNullOrEmpty(property.stringValue);
            }

            fieldElement.EnableInClassList("inlo-field-error-state", hasMissingReference);
            alertBox.style.display = hasMissingReference ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ExecuteMethod(MethodInfo method)
        {
            serializedObject.ApplyModifiedProperties();

            // Execute method on all selected targets (supporting multi-object editing cleanly)
            foreach (var targetObj in targets)
            {
                try
                {
                    method.Invoke(targetObj, null);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[InloInspector] Exception raised while executing '{method.Name}' on '{targetObj.name}': {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }
    }
}
