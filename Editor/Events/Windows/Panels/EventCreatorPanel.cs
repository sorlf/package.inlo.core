using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using INLO.Core.EditorUI.Editor;

namespace INLO.Core.Editor.Events
{
    public sealed class EventCreatorPanel : VisualElement
    {
        private enum EventFieldType
        {
            Int,
            Float,
            Bool,
            String,
            GameObject,
            Vector2,
            Vector3
        }

        [System.Serializable]
        private class EventFieldDefinition
        {
            public string Name = "Target";
            public EventFieldType Type = EventFieldType.GameObject;
        }

        private string eventName = "DamageTaken";
        private string namespaceName = "Game.Events";
        private string outputFolder = "Assets/GeneratedEvents";
        private string assetFolder = "Assets/GameEvents";
        private string eventDescription = "이 이벤트가 언제 발생하는지, 어떤 시스템이 들어야 하는지 작성하세요.";

        private bool createEventData = true;
        private bool createEventChannel = true;
        private bool createChannelAsset = true;
        private bool refreshAssetDatabase = true;

        private readonly List<EventFieldDefinition> fields = new()
        {
            new EventFieldDefinition { Name = "Target", Type = EventFieldType.GameObject }
        };

        private TextField eventNameField;
        private TextField namespaceField;
        private TextField descriptionField;
        private TextField outputFolderField;
        private TextField assetFolderField;

        private Toggle createEventDataToggle;
        private Toggle createEventChannelToggle;
        private Toggle createChannelAssetToggle;
        private Toggle refreshAssetDatabaseToggle;

        private VisualElement fieldsContainer;
        private VisualElement previewContainer;
        private VisualElement validationContainer;
        private Button generateButton;
        private Button createAssetButton;

        private readonly System.Action _onCreatedCallback;

        public EventCreatorPanel(System.Action onCreated)
        {
            _onCreatedCallback = onCreated;
            BuildUI();
            RefreshAll();
        }

        private void BuildUI()
        {
            style.flexGrow = 1;
            style.flexDirection = FlexDirection.Column;
            style.minHeight = 0;

            ScrollView scroll = new() { name = "event-creator-scroll" };
            scroll.style.flexGrow = 1;
            scroll.style.minHeight = 0;
            Add(scroll);

            scroll.Add(CreateEventInfoPanel());
            scroll.Add(CreateOutputPanel());
            scroll.Add(CreateOptionsPanel());
            scroll.Add(CreateFieldsPanel());
            scroll.Add(CreateActionsPanel());
            scroll.Add(CreatePreviewPanel());
        }
        private VisualElement CreateEventInfoPanel()
        {
            VisualElement panel = InloUIFactory.CreateCard();
            panel.style.marginBottom = 8;

            AddSectionTitle(panel, "Event Metadata", "이름과 설명을 작성합니다.");

            eventNameField = new TextField("Event Name") { value = eventName };
            eventNameField.RegisterCallback<FocusOutEvent>(_ =>
            {
                eventName = eventNameField.value;
                RefreshAll();
            });
            panel.Add(eventNameField);

            namespaceField = new TextField("Namespace") { value = namespaceName };
            namespaceField.RegisterCallback<FocusOutEvent>(_ =>
            {
                namespaceName = namespaceField.value;
                RefreshAll();
            });
            panel.Add(namespaceField);

            Label descLbl = new("Description");
            descLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            descLbl.style.marginTop = 8;
            descLbl.style.marginBottom = 4;
            panel.Add(descLbl);

            descriptionField = new TextField { multiline = true, value = eventDescription };
            descriptionField.style.height = 80;
            descriptionField.style.whiteSpace = WhiteSpace.Normal;
            descriptionField.RegisterCallback<FocusOutEvent>(_ =>
            {
                eventDescription = descriptionField.value;
                RefreshAll();
            });
            panel.Add(descriptionField);

            return panel;
        }

        private VisualElement CreateOutputPanel()
        {
            VisualElement panel = InloUIFactory.CreateCard();
            panel.style.marginBottom = 8;

            AddSectionTitle(panel, "Output Paths", "저장 폴더 위치를 지정합니다.");

            outputFolderField = CreateFolderTextField("Scripts Folder", outputFolder, val => { outputFolder = val; RefreshAll(); }, panel);
            assetFolderField = CreateFolderTextField("Assets Folder", assetFolder, val => { assetFolder = val; RefreshAll(); }, panel);

            return panel;
        }

        private VisualElement CreateOptionsPanel()
        {
            VisualElement panel = InloUIFactory.CreateCard();

            AddSectionTitle(panel, "Options", "코드 생성 및 에셋 임포트 설정을 관리합니다.");

            createEventDataToggle = new Toggle("Create EventData struct") { value = createEventData };
            createEventDataToggle.RegisterValueChangedCallback(evt => { createEventData = evt.newValue; RefreshAll(); });
            panel.Add(createEventDataToggle);

            createEventChannelToggle = new Toggle("Create EventChannel Script") { value = createEventChannel };
            createEventChannelToggle.RegisterValueChangedCallback(evt => { createEventChannel = evt.newValue; RefreshAll(); });
            panel.Add(createEventChannelToggle);

            createChannelAssetToggle = new Toggle("Create ScriptableAsset") { value = createChannelAsset };
            createChannelAssetToggle.RegisterValueChangedCallback(evt => { createChannelAsset = evt.newValue; RefreshAll(); });
            panel.Add(createChannelAssetToggle);

            refreshAssetDatabaseToggle = new Toggle("Auto-Refresh DB") { value = refreshAssetDatabase };
            refreshAssetDatabaseToggle.RegisterValueChangedCallback(evt => refreshAssetDatabase = evt.newValue);
            panel.Add(refreshAssetDatabaseToggle);

            return panel;
        }

        private VisualElement CreateFieldsPanel()
        {
            VisualElement panel = InloUIFactory.CreateCard();
            panel.style.flexGrow = 1;
            panel.style.minHeight = 220;

            VisualElement headerRow = new();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 8;
            panel.Add(headerRow);

            VisualElement titleGroup = new();
            titleGroup.style.flexGrow = 1;
            headerRow.Add(titleGroup);

            Label title = new("Event Payload Fields");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 14;
            titleGroup.Add(title);

            Label sub = new("EventData 구조체에 전달할 매개변수 컬럼 목록을 정의합니다.");
            sub.AddToClassList("inlo-muted");
            titleGroup.Add(sub);

            Button addBtn = InloUIFactory.CreateAccentButton("+ Add Field", () =>
            {
                fields.Add(new EventFieldDefinition { Name = "NewField", Type = EventFieldType.Int });
                RebuildFields();
                RefreshAll();
            });
            addBtn.style.width = 110;
            headerRow.Add(addBtn);

            fieldsContainer = new VisualElement();
            fieldsContainer.style.flexGrow = 1;
            panel.Add(fieldsContainer);

            RebuildFields();
            return panel;
        }

        private VisualElement CreateActionsPanel()
        {
            VisualElement panel = InloUIFactory.CreateCard();
            AddSectionTitle(panel, "Actions", "단계별로 진행합니다.");

            generateButton = new Button(Generate) { text = "1. Generate Scripts" };
            StylePrimaryButton(generateButton);
            panel.Add(generateButton);

            createAssetButton = new Button(CreateChannelAssetFromGeneratedType) { text = "2. Create Channel Asset" };
            StylePrimaryButton(createAssetButton);
            createAssetButton.style.marginTop = 8;
            panel.Add(createAssetButton);

            HelpBox box = new HelpBox("스크립트를 먼저 [Generate]하고 컴파일이 끝나면 [Create Asset]을 누릅니다.", HelpBoxMessageType.None);
            box.style.marginTop = 8;
            panel.Add(box);

            return panel;
        }

        private VisualElement CreatePreviewPanel()
        {
            VisualElement panel = InloUIFactory.CreateCard();
            AddSectionTitle(panel, "Preview & Validation", "코드 검증 및 산출물 프리뷰입니다.");

            validationContainer = new VisualElement();
            panel.Add(validationContainer);

            previewContainer = new VisualElement();
            previewContainer.style.marginTop = 6;
            panel.Add(previewContainer);

            return panel;
        }

        private TextField CreateFolderTextField(string label, string initialValue, System.Action<string> onChanged, VisualElement parent)
        {
            VisualElement row = new();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 6;
            parent.Add(row);

            TextField field = new(label) { value = initialValue };
            field.style.flexGrow = 1;
            field.style.minWidth = 0;
            field.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
            row.Add(field);

            Button selectBtn = InloUIFactory.CreateDefaultButton("Select", () =>
            {
                string path = EditorUtility.OpenFolderPanel($"Select {label}", Application.dataPath, string.Empty);
                if (string.IsNullOrEmpty(path)) return;

                string projectPath = Directory.GetParent(Application.dataPath).FullName.Replace("\\", "/");
                path = path.Replace("\\", "/");

                if (path.StartsWith(projectPath))
                {
                    string relPath = path.Substring(projectPath.Length + 1);
                    field.value = relPath;
                    onChanged(relPath);
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Folder", "Unity 프로젝트 내부 폴더만 선택할 수 있습니다.", "OK");
                }
            });
            selectBtn.style.width = 65;
            selectBtn.style.marginLeft = 6;
            row.Add(selectBtn);

            return field;
        }

        private void RebuildFields()
        {
            if (fieldsContainer == null) return;
            fieldsContainer.Clear();

            if (fields.Count == 0)
            {
                HelpBox box = new("정의된 필드가 없습니다. 매개변수가 비어 있는 이벤트가 생성됩니다.", HelpBoxMessageType.Warning);
                fieldsContainer.Add(box);
                return;
            }

            for (int i = 0; i < fields.Count; i++)
            {
                fieldsContainer.Add(CreateFieldRow(i));
            }
        }

        private VisualElement CreateFieldRow(int index)
        {
            EventFieldDefinition def = fields[index];

            VisualElement row = new();
            row.AddToClassList("inlo-list-card");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            UpdateFieldRowStatus(row, def.Name);

            TextField nameFld = new() { value = def.Name };
            nameFld.style.flexGrow = 1;
            nameFld.style.minWidth = 0;
            nameFld.RegisterCallback<FocusOutEvent>(_ =>
            {
                def.Name = nameFld.value;
                UpdateFieldRowStatus(row, def.Name);
                RefreshAll();
            });
            row.Add(nameFld);

            EnumField typeFld = new(def.Type);
            typeFld.style.width = 120;
            typeFld.style.marginLeft = 6;
            typeFld.RegisterValueChangedCallback(evt =>
            {
                def.Type = (EventFieldType)evt.newValue;
                RefreshAll();
            });
            row.Add(typeFld);

            Button rmBtn = InloUIFactory.CreateDefaultButton("Remove", () =>
            {
                fields.RemoveAt(index);
                RebuildFields();
                RefreshAll();
            });
            rmBtn.style.width = 70;
            rmBtn.style.marginLeft = 6;
            row.Add(rmBtn);

            return row;
        }

        private void UpdateFieldRowStatus(VisualElement row, string name)
        {
            bool valid = IsValidCodeIdentifierInput(name);
            row.EnableInClassList("inlo-list-card--ok", valid);
            row.EnableInClassList("inlo-list-card--error", !valid);
        }

        private void RefreshAll()
        {
            RefreshValidation();
            RefreshPreview();

            if (generateButton != null) generateButton.SetEnabled(CanGenerateScripts());
            if (createAssetButton != null) createAssetButton.SetEnabled(CanCreateChannelAsset());
        }

        private void RefreshValidation()
        {
            if (validationContainer == null) return;
            validationContainer.Clear();

            bool hasIssue = false;

            if (string.IsNullOrWhiteSpace(eventName))
            {
                validationContainer.Add(new HelpBox("이벤트 이름을 작성해 주세요.", HelpBoxMessageType.Warning));
                hasIssue = true;
            }
            else if (!IsValidCodeIdentifierInput(eventName))
            {
                validationContainer.Add(new HelpBox("이벤트 이름은 영문, 숫자, 언더스코어(_)만 사용할 수 있습니다.", HelpBoxMessageType.Error));
                hasIssue = true;
            }

            if (!IsValidNamespaceInput(namespaceName))
            {
                validationContainer.Add(new HelpBox("Namespace는 영문, 숫자, 언더스코어, 점(.)만 가능합니다.", HelpBoxMessageType.Error));
                hasIssue = true;
            }

            if (!AreFieldNamesValid())
            {
                validationContainer.Add(new HelpBox("필드 이름은 영문, 숫자, 언더스코어(_)만 가능합니다.", HelpBoxMessageType.Error));
                hasIssue = true;
            }

            if (!hasIssue)
            {
                validationContainer.Add(CreateInlineStatus("유효성 검증 통과 (Validation OK)", "inlo-badge--ok"));
            }
        }

        private void RefreshPreview()
        {
            if (previewContainer == null) return;
            previewContainer.Clear();

            previewContainer.Add(CreatePreviewRow("EventData Struct", GetEventDataScriptPath(), createEventData));
            previewContainer.Add(CreatePreviewRow("EventChannelSO Class", GetEventChannelScriptPath(), createEventChannel));
            previewContainer.Add(CreatePreviewRow("ScriptableAsset File", GetChannelAssetPath(), createChannelAsset));
        }

        private VisualElement CreatePreviewRow(string label, string path, bool enabled)
        {
            VisualElement row = InloUIFactory.CreateCard();
            row.style.marginBottom = 4;
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;

            VisualElement top = new();
            top.style.flexDirection = FlexDirection.Row;
            top.style.alignItems = Align.Center;
            row.Add(top);

            Label title = new(label);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.flexGrow = 1;
            top.Add(title);

            if (!enabled)
            {
                top.Add(CreateBadge("Disabled", null));
            }
            else
            {
                bool exists = File.Exists(path) || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null;
                top.Add(CreateBadge(exists ? "Exists" : "New", exists ? "inlo-badge--warning" : "inlo-badge--ok"));
            }

            Label pathLabel = new(path);
            pathLabel.AddToClassList("inlo-muted");
            pathLabel.AddToClassList("inlo-wrap");
            pathLabel.style.marginTop = 4;
            pathLabel.style.fontSize = 10;
            row.Add(pathLabel);

            return row;
        }

        private static Label CreateInlineStatus(string text, string modifierClass)
        {
            Label badge = new(text);
            badge.AddToClassList("inlo-badge");
            badge.style.marginBottom = 4;
            if (!string.IsNullOrEmpty(modifierClass)) badge.AddToClassList(modifierClass);
            return badge;
        }

        private bool CanGenerateScripts()
        {
            return !string.IsNullOrWhiteSpace(eventName) &&
                   IsValidCodeIdentifierInput(eventName) &&
                   IsValidNamespaceInput(namespaceName) &&
                   AreFieldNamesValid();
        }

        private bool CanCreateChannelAsset()
        {
            return !string.IsNullOrWhiteSpace(eventName) &&
                   IsValidCodeIdentifierInput(eventName) &&
                   IsValidNamespaceInput(namespaceName);
        }

        private void Generate()
        {
            string cleanName = SanitizeTypeName(eventName);
            if (!ValidateBeforeGenerate(cleanName)) return;

            if (HasExistingGeneratedFiles())
            {
                bool check = EditorUtility.DisplayDialog("파일 중복 경고", "기존에 자동 생성된 파일이 이미 존재합니다.\n덮어쓰시겠습니까?", "Yes", "Cancel");
                if (!check) return;
            }

            try
            {
                AssetDatabase.StartAssetEditing();

                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                if (createEventData) CreateEventDataScript(cleanName);
                if (createEventChannel) CreateEventChannelScript(cleanName);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            if (refreshAssetDatabase)
            {
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("Generation Successful", $"{cleanName} 스크립트 생성이 완료되었습니다.\n\nUnity 컴파일이 완료된 후 'Create Channel Asset' 버튼을 활성화하여 에셋을 만드세요.", "OK");
            RefreshAll();
        }

        private bool ValidateBeforeGenerate(string cleanName)
        {
            if (string.IsNullOrWhiteSpace(cleanName) || !IsValidCodeIdentifierInput(eventName))
            {
                EditorUtility.DisplayDialog("Error", "Event Name은 영어, 숫자, 언더스코어만 가능합니다.", "OK");
                return false;
            }
            if (!IsValidNamespaceInput(namespaceName))
            {
                EditorUtility.DisplayDialog("Error", "Namespace는 영어, 숫자, 언더스코어, 점(.)만 가능합니다.", "OK");
                return false;
            }
            if (!AreFieldNamesValid())
            {
                EditorUtility.DisplayDialog("Error", "Field Name은 영어, 숫자, 언더스코어만 가능합니다.", "OK");
                return false;
            }
            return true;
        }

        private bool HasExistingGeneratedFiles()
        {
            if (createEventData && File.Exists(GetEventDataScriptPath())) return true;
            if (createEventChannel && File.Exists(GetEventChannelScriptPath())) return true;
            return false;
        }

        private string GetCleanEventName() { return SanitizeTypeName(eventName); }
        private string GetEventDataScriptPath() { return Path.Combine(outputFolder, $"{GetCleanEventName()}EventData.cs").Replace("\\", "/"); }
        private string GetEventChannelScriptPath() { return Path.Combine(outputFolder, $"{GetCleanEventName()}EventChannelSO.cs").Replace("\\", "/"); }
        private string GetChannelAssetPath() { return Path.Combine(assetFolder, $"{GetCleanEventName()}Channel.asset").Replace("\\", "/"); }

        private string BuildAutoGeneratedHeader(string name, string kind)
        {
            return
$@"// <auto-generated>
// This file was generated by INLO Event Channel Creator.
// Event Name: {name}
// File Kind: {kind}
// Generated At: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}
// Do not modify this file manually.
// </auto-generated>

";
        }

        private void CreateEventDataScript(string cleanName)
        {
            string className = $"{cleanName}EventData";
            string path = Path.Combine(outputFolder, $"{className}.cs");

            string fieldCode = BuildFieldCode();
            string toStringCode = BuildToStringCode();
            string header = BuildAutoGeneratedHeader(cleanName, "EventData");

            string code =
$@"{header}using UnityEngine;

namespace {namespaceName}
{{
    [System.Serializable]
    public struct {className}
    {{
{fieldCode}

        public override string ToString()
        {{
            return $""{toStringCode}"";
        }}
    }}
}}
";
            File.WriteAllText(path, code, Encoding.UTF8);
        }

        private void CreateEventChannelScript(string cleanName)
        {
            string eventDataName = $"{cleanName}EventData";
            string className = $"{cleanName}EventChannelSO";
            string path = Path.Combine(outputFolder, $"{className}.cs");
            string menuName = AddSpacesBeforeUppercase(cleanName);

            string header = BuildAutoGeneratedHeader(cleanName, "EventChannelSO");

            string code =
$@"{header}using UnityEngine;
using INLO.Core.Events;

namespace {namespaceName}
{{
    [CreateAssetMenu(menuName = ""INLO/Game/Events/{menuName} Event Channel"")]
    public class {className} : EventChannelSO<{eventDataName}>
    {{
    }}
}}
";
            File.WriteAllText(path, code, Encoding.UTF8);
        }

        private void CreateChannelAssetFromGeneratedType()
        {
            string cleanName = SanitizeTypeName(eventName);
            if (string.IsNullOrWhiteSpace(cleanName) || !IsValidCodeIdentifierInput(eventName)) return;

            string typeName = $"{namespaceName}.{cleanName}EventChannelSO";
            System.Type channelType = FindType(typeName);

            if (channelType == null)
            {
                EditorUtility.DisplayDialog("Compile Required", $"타입을 찾을 수 없습니다:\n{typeName}\n\n스크립트가 생성되었는지, 유니티 컴파일이 완전히 끝났는지 확인해 주세요.", "OK");
                return;
            }

            if (!Directory.Exists(assetFolder))
            {
                Directory.CreateDirectory(assetFolder);
                AssetDatabase.Refresh();
            }

            string assetName = $"{cleanName}Channel.asset";
            string assetPath = Path.Combine(assetFolder, assetName).Replace("\\", "/");

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
            {
                bool dup = EditorUtility.DisplayDialog("에셋 존재함", $"에셋 파일이 이미 존재합니다:\n{assetPath}\n\n고유한 이름을 가진 사본을 생성하시겠습니까?", "Create Copy", "Cancel");
                if (!dup) return;
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            }

            ScriptableObject asset = ScriptableObject.CreateInstance(channelType);
            if (asset == null) return;

            ApplyDescriptionToAsset(asset, eventDescription);
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            EditorUtility.DisplayDialog("Asset Created", $"이벤트 에셋이 정상 생성되었습니다:\n{assetPath}", "OK");

            // 생성 완료 후 콜백 호출 (이벤트 브라우저 리스트 자동 새로고침)
            if (_onCreatedCallback != null) _onCreatedCallback();

            RefreshAll();
        }

        private static void ApplyDescriptionToAsset(ScriptableObject asset, string description)
        {
            SerializedObject serializedObject = new SerializedObject(asset);
            SerializedProperty descriptionProperty = serializedObject.FindProperty("description");
            if (descriptionProperty != null)
            {
                descriptionProperty.stringValue = description;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private string BuildFieldCode()
        {
            StringBuilder builder = new();
            foreach (EventFieldDefinition field in fields)
            {
                builder.AppendLine($"        public {ToCSharpTypeName(field.Type)} {SanitizeFieldName(field.Name)};");
            }
            return builder.ToString().TrimEnd();
        }

        private string BuildToStringCode()
        {
            StringBuilder builder = new();
            for (int i = 0; i < fields.Count; i++)
            {
                string fieldName = SanitizeFieldName(fields[i].Name);
                if (i > 0) builder.Append(", ");
                builder.Append($"{fieldName}: {{{fieldName}}}");
            }
            return builder.Length == 0 ? "No Data" : builder.ToString();
        }

        private static string ToCSharpTypeName(EventFieldType type)
        {
            return type switch
            {
                EventFieldType.Int => "int",
                EventFieldType.Float => "float",
                EventFieldType.Bool => "bool",
                EventFieldType.String => "string",
                EventFieldType.GameObject => "GameObject",
                EventFieldType.Vector2 => "Vector2",
                EventFieldType.Vector3 => "Vector3",
                _ => "object"
            };
        }

        private static string SanitizeTypeName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return string.Empty;
            string result = string.Empty;
            foreach (char c in rawName)
            {
                if (IsAsciiLetterOrDigit(c) || c == '_') result += c;
            }
            if (string.IsNullOrEmpty(result)) return string.Empty;
            if (char.IsDigit(result[0])) result = "_" + result;
            return result;
        }

        private static string SanitizeFieldName(string rawName)
        {
            string clean = SanitizeTypeName(rawName);
            return string.IsNullOrWhiteSpace(clean) ? "Value" : clean;
        }

        private static string AddSpacesBeforeUppercase(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            string result = value[0].ToString();
            for (int i = 1; i < value.Length; i++)
            {
                char current = value[i];
                if (char.IsUpper(current)) result += " ";
                result += current;
            }
            return result;
        }

        private static System.Type FindType(string fullTypeName)
        {
            System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (System.Reflection.Assembly assembly in assemblies)
            {
                System.Type type = assembly.GetType(fullTypeName);
                if (type != null) return type;
            }
            return null;
        }

        private static bool IsValidCodeIdentifierInput(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            foreach (char c in value)
            {
                if (!IsAsciiLetterOrDigit(c) && c != '_') return false;
            }
            return true;
        }

        private static bool IsValidNamespaceInput(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            foreach (char c in value)
            {
                if (!IsAsciiLetterOrDigit(c) && c != '_' && c != '.') return false;
            }
            return true;
        }

        private bool AreFieldNamesValid()
        {
            foreach (EventFieldDefinition field in fields)
            {
                if (!IsValidCodeIdentifierInput(field.Name)) return false;
            }
            return true;
        }

        private static bool IsAsciiLetterOrDigit(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
        }

        private static void AddSectionTitle(VisualElement parent, string titleText, string subtitleText)
        {
            parent.Add(InloUIFactory.CreateSectionLabel(titleText));
            Label subtitle = new(subtitleText);
            subtitle.AddToClassList("inlo-muted");
            subtitle.AddToClassList("inlo-wrap");
            subtitle.style.marginBottom = 8;
            parent.Add(subtitle);
        }

        private static void StylePrimaryButton(Button button)
        {
            button.AddToClassList("inlo-button");
            button.AddToClassList("inlo-button--accent");
            button.style.height = 30;
        }

        private static Label CreateBadge(string text, string modifierClass)
        {
            Color badgeColor = modifierClass == "inlo-badge--ok" 
                ? new Color(0.18f, 0.49f, 0.2f) 
                : modifierClass == "inlo-badge--info" 
                    ? new Color(0.0f, 0.47f, 0.74f) 
                    : modifierClass == "inlo-badge--warning"
                        ? new Color(0.85f, 0.65f, 0.13f)
                        : new Color(0.3f, 0.3f, 0.3f);

            Label badge = InloUIFactory.CreateBadge(text, badgeColor);
            badge.style.marginLeft = 5;
            if (!string.IsNullOrEmpty(modifierClass)) badge.AddToClassList(modifierClass);
            return badge;
        }
    }
}
