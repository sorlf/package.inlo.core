using INLO.Core.Events;
using UnityEditor;
using UnityEngine;

namespace INLO.Core.Events.Editor
{
    [CustomEditor(typeof(EventChannelBaseSO), true)]
    public class EventChannelBaseEditor : UnityEditor.Editor
    {
        private SerializedProperty descriptionProperty;
        private SerializedProperty debugLogProperty;

        private void OnEnable()
        {
            descriptionProperty = serializedObject.FindProperty("description");
            debugLogProperty = serializedObject.FindProperty("debugLog");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawEventChannelHeader();

            EditorGUILayout.Space(10);

            DrawDescriptionField();

            EditorGUILayout.Space(8);

            DrawDebugLogField();

            EditorGUILayout.Space(10);

            DrawUsageNotice();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawEventChannelHeader()
        {
            EditorGUILayout.LabelField("INLO Event Channel", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "This asset is an event channel.\n\n" +
                "It does not store game state.\n" +
                "It only broadcasts that something happened.\n\n" +
                "The actual state should be owned by a Controller, Service, or Model.",
                MessageType.Info
            );
        }

        private void DrawDescriptionField()
        {
            if (descriptionProperty == null)
            {
                EditorGUILayout.HelpBox(
                    "Description property was not found.",
                    MessageType.Warning
                );
                return;
            }

            EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);

            descriptionProperty.stringValue = EditorGUILayout.TextArea(
                descriptionProperty.stringValue,
                GUILayout.MinHeight(120)
            );
        }

        private void DrawDebugLogField()
        {
            if (debugLogProperty == null)
            {
                EditorGUILayout.HelpBox(
                    "Debug Log property was not found.",
                    MessageType.Warning
                );
                return;
            }

            EditorGUILayout.PropertyField(debugLogProperty, new GUIContent("Debug Log"));
        }

        private void DrawUsageNotice()
        {
            EditorGUILayout.HelpBox(
                "Recommended usage:\n\n" +
                "- Use this channel to reduce direct references between systems.\n" +
                "- Keep event data as a snapshot of what happened.\n" +
                "- Do not use this asset as a global variable or state container.",
                MessageType.None
            );
        }
    }
}