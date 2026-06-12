using INLO.Core.Pooling;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace INLO.Core.Pooling.Editor
{
    [CustomEditor(typeof(PoolDatabase))]
    public sealed class PoolDatabaseEditor : UnityEditor.Editor
    {
        private VisualElement _validationContainer;
        private Toggle _autoValidateToggle;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            root.style.paddingLeft = 4;
            root.style.paddingRight = 4;
            root.style.paddingTop = 4;
            root.style.paddingBottom = 4;

            SerializedProperty entriesProperty = serializedObject.FindProperty("entries");

            PropertyField entriesField = new(entriesProperty)
            {
                label = "Entries"
            };
            entriesField.Bind(serializedObject);
            root.Add(entriesField);

            VisualElement toolbar = new();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.marginTop = 8;
            toolbar.style.marginBottom = 6;

            _autoValidateToggle = new Toggle("Auto Validate")
            {
                value = true
            };
            _autoValidateToggle.style.width = 140;

            Button validateButton = new(ValidateAndDraw)
            {
                text = "Validate"
            };
            validateButton.style.width = 90;

            toolbar.Add(_autoValidateToggle);
            toolbar.Add(validateButton);

            root.Add(toolbar);

            _validationContainer = new VisualElement();
            root.Add(_validationContainer);

            entriesField.RegisterValueChangeCallback(_ =>
            {
                if (_autoValidateToggle != null && _autoValidateToggle.value)
                {
                    ValidateAndDraw();
                }
            });

            root.schedule.Execute(() =>
            {
                if (_autoValidateToggle != null && _autoValidateToggle.value)
                {
                    ValidateAndDraw();
                }
            }).ExecuteLater(100);

            return root;
        }

        private void ValidateAndDraw()
        {
            if (_validationContainer == null)
            {
                return;
            }

            serializedObject.ApplyModifiedProperties();

            PoolDatabase database = (PoolDatabase)target;
            PoolDatabaseValidationResult result = PoolDatabaseValidator.Validate(database);

            _validationContainer.Clear();

            DrawSummary(result);

            for (int i = 0; i < result.Messages.Count; i++)
            {
                PoolDatabaseValidationMessage message = result.Messages[i];
                _validationContainer.Add(CreateMessageBox(message));
            }
        }

        private void DrawSummary(PoolDatabaseValidationResult result)
        {
            string text;
            MessageType messageType;

            if (result.HasError)
            {
                text = "PoolDatabase has errors. Fix them before use.";
                messageType = MessageType.Error;
            }
            else if (result.HasWarning)
            {
                text = "PoolDatabase is usable but has warnings.";
                messageType = MessageType.Warning;
            }
            else
            {
                text = "PoolDatabase is valid.";
                messageType = MessageType.Info;
            }

            HelpBox summary = new(text, ToHelpBoxMessageType(messageType));
            summary.style.marginBottom = 4;
            _validationContainer.Add(summary);
        }

        private static HelpBox CreateMessageBox(PoolDatabaseValidationMessage message)
        {
            string prefix = message.EntryIndex >= 0 ? $"Entry {message.EntryIndex}: " : string.Empty;
            HelpBox box = new(prefix + message.Message, ToHelpBoxMessageType(ToMessageType(message.Severity)));
            box.style.marginTop = 2;
            return box;
        }

        private static MessageType ToMessageType(PoolDatabaseValidationSeverity severity)
        {
            switch (severity)
            {
                case PoolDatabaseValidationSeverity.Info:
                    return MessageType.Info;

                case PoolDatabaseValidationSeverity.Warning:
                    return MessageType.Warning;

                case PoolDatabaseValidationSeverity.Error:
                    return MessageType.Error;

                default:
                    return MessageType.None;
            }
        }

        private static HelpBoxMessageType ToHelpBoxMessageType(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Info:
                    return HelpBoxMessageType.Info;

                case MessageType.Warning:
                    return HelpBoxMessageType.Warning;

                case MessageType.Error:
                    return HelpBoxMessageType.Error;

                default:
                    return HelpBoxMessageType.None;
            }
        }
    }
}
