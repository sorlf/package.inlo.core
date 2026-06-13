using System.Collections.Generic;
using INLO.Core.Pooling;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using INLO.Core.EditorUI.Editor;

namespace INLO.Core.Pooling.Editor
{
    public sealed class PoolBrowserPanel : VisualElement
    {
        private readonly struct BrowserEntry
        {
            public readonly PoolDatabase Database;
            public readonly int DatabaseIndex;
            public readonly PoolEntry Entry;
            public readonly int EntryIndex;
            public readonly List<PoolDatabaseValidationMessage> Messages;

            public BrowserEntry(
                PoolDatabase database,
                int databaseIndex,
                PoolEntry entry,
                int entryIndex,
                List<PoolDatabaseValidationMessage> messages)
            {
                Database = database;
                DatabaseIndex = databaseIndex;
                Entry = entry;
                EntryIndex = entryIndex;
                Messages = messages;
            }
        }

        private readonly IPoolWindow _window;
        private ObjectField _sourceField;
        private TextField _searchField;
        private Toggle _errorsOnlyToggle;
        private Toggle _warningsOnlyToggle;
        private Label _sourceTypeLabel;
        private Label _summaryLabel;
        private ScrollView _entryList;

        private PoolDatabase _database;
        private PoolDatabaseGroup _group;
        private string _searchText = string.Empty;

        public PoolBrowserPanel(IPoolWindow window)
        {
            _window = window;
            BuildUI();
            TryUseSelectedAsset();
            RefreshSourceField();
            Refresh();
        }

        private void BuildUI()
        {
            style.flexGrow = 1;
            style.flexDirection = FlexDirection.Column;

            // 3단계 정보 설계: F-패턴 가로 분할 스플릿 레이아웃 적용
            VisualElement body = new();
            body.AddToClassList("inlo-split-container");
            body.style.flexGrow = 1;
            Add(body);

            // 1. 좌측 제어 사이드바 (Width 350px 고정)
            VisualElement sidebar = new();
            sidebar.AddToClassList("inlo-card");
            sidebar.AddToClassList("inlo-sidebar-left");
            body.Add(sidebar);

            PopulateSourceSection(sidebar);
            PopulateFilterSection(sidebar);

            // 2. 우측 데이터 메인 워크스페이스
            VisualElement workspace = new();
            workspace.AddToClassList("inlo-workspace-right");
            body.Add(workspace);

            // 우측 상단 요약 요약 카드
            VisualElement summaryCard = InloUIFactory.CreateCard();
            summaryCard.name = "pool-browser-summary";
            workspace.Add(summaryCard);
            PopulateSummarySection(summaryCard);

            // 우측 하단 데이터 테이블 그리드
            VisualElement entriesCard = InloUIFactory.CreateCard();
            entriesCard.AddToClassList("inlo-card--grow");
            workspace.Add(entriesCard);
            PopulateEntriesSection(entriesCard);
        }

        private void PopulateSourceSection(VisualElement sourceSlot)
        {
            sourceSlot.Add(InloUIFactory.CreateSectionLabel("Source Selection"));

            // 필드 위에 깔끔한 다크 그레이 안내 라벨 얹어 공간 활용 극대화
            Label fieldDesc = new("Target Pool Database / Group:") { style = { fontSize = 11, marginBottom = 4 } };
            fieldDesc.AddToClassList("inlo-muted");
            sourceSlot.Add(fieldDesc);

            VisualElement fieldRow = new();
            fieldRow.AddToClassList("inlo-row");
            fieldRow.style.marginBottom = 6;

            // 투박하게 좌측 절반을 낭비하던 기본 라벨을 소멸시켜 필드 가로폭 100% 확보
            _sourceField = new ObjectField
            {
                objectType = typeof(ScriptableObject), // 1차 필터 (ScriptableObject로 제한)
                allowSceneObjects = false
            };
            _sourceField.AddToClassList("inlo-field--grow");
            _sourceField.RegisterValueChangedCallback(evt =>
            {
                // PoolDatabase나 PoolDatabaseGroup이 들어왔을 때만 허용
                if (evt.newValue == null || evt.newValue is PoolDatabase || evt.newValue is PoolDatabaseGroup)
                {
                    SetSource(evt.newValue);
                }
                else
                {
                    SetSource(null);
                }
                RefreshSourceField();
                Refresh();
            });
            fieldRow.Add(_sourceField);

            // 프로젝트 내 올바른 에셋들만 한 눈에 걸러내어 1클릭으로 선택하는 프리미엄 드롭다운 메뉴 버튼
            Button quickSelectBtn = InloUIFactory.CreateDefaultButton("▼", ShowQuickSelectMenu);
            quickSelectBtn.style.marginLeft = 4;
            quickSelectBtn.style.paddingLeft = 8;
            quickSelectBtn.style.paddingRight = 8;
            quickSelectBtn.tooltip = "Quick Select Pool Assets from Project";
            fieldRow.Add(quickSelectBtn);

            sourceSlot.Add(fieldRow);

            VisualElement statusRow = new();
            statusRow.AddToClassList("inlo-row");
            statusRow.style.marginBottom = 10;

            Label typeMetaLabel = new("Active Type:") { style = { fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, marginRight = 6 } };
            typeMetaLabel.AddToClassList("inlo-muted");
            statusRow.Add(typeMetaLabel);

            _sourceTypeLabel = new Label("None Selected");
            _sourceTypeLabel.AddToClassList("inlo-badge");
            _sourceTypeLabel.AddToClassList("inlo-badge--info");
            _sourceTypeLabel.style.width = StyleKeyword.Auto;
            statusRow.Add(_sourceTypeLabel);

            sourceSlot.Add(statusRow);

            VisualElement buttonRow = InloUIFactory.CreateButtonRow();

            Button useSelectionButton = InloUIFactory.CreateDefaultButton("Use Selection", () =>
            {
                TryUseSelectedAsset();
                RefreshSourceField();
                Refresh();
            });

            Button pingButton = InloUIFactory.CreateDefaultButton("Ping Asset", () =>
            {
                Object target = GetCurrentObject();
                if (target == null) return;
                EditorGUIUtility.PingObject(target);
                Selection.activeObject = target;
            });

            buttonRow.Add(useSelectionButton);
            buttonRow.Add(pingButton);
            sourceSlot.Add(buttonRow);
        }

        private void ShowQuickSelectMenu()
        {
            GenericMenu menu = new();

            // 1. PoolDatabase 에셋만 정확히 긁기
            string[] dbGuids = AssetDatabase.FindAssets("t:PoolDatabase");
            List<PoolDatabase> databases = new();
            foreach (string guid in dbGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PoolDatabase asset = AssetDatabase.LoadAssetAtPath<PoolDatabase>(path);
                if (asset != null) databases.Add(asset);
            }
            databases.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            foreach (PoolDatabase db in databases)
            {
                menu.AddItem(new GUIContent($"Pool Databases/{db.name}"), _database == db, () =>
                {
                    SetSource(db);
                    RefreshSourceField();
                    Refresh();
                });
            }

            if (databases.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("Pool Databases/No Assets Found"));
            }

            // 2. PoolDatabaseGroup 에셋만 정확히 긁기
            string[] groupGuids = AssetDatabase.FindAssets("t:PoolDatabaseGroup");
            List<PoolDatabaseGroup> groups = new();
            foreach (string guid in groupGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PoolDatabaseGroup asset = AssetDatabase.LoadAssetAtPath<PoolDatabaseGroup>(path);
                if (asset != null) groups.Add(asset);
            }
            groups.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            foreach (PoolDatabaseGroup gp in groups)
            {
                menu.AddItem(new GUIContent($"Pool Groups/{gp.name}"), _group == gp, () =>
                {
                    SetSource(gp);
                    RefreshSourceField();
                    Refresh();
                });
            }

            if (groups.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("Pool Groups/No Assets Found"));
            }

            menu.ShowAsContext();
        }

        private void PopulateFilterSection(VisualElement filterSlot)
        {
            VisualElement divider = new();
            divider.style.height = 1;
            divider.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            divider.style.marginTop = 12;
            divider.style.marginBottom = 12;
            filterSlot.Add(divider);

            filterSlot.Add(InloUIFactory.CreateSectionLabel("Quick Filters"));

            _searchField = new TextField("Search Key/Prefab");
            _searchField.AddToClassList("inlo-field");
            _searchField.style.marginBottom = 8;
            _searchField.RegisterCallback<FocusOutEvent>(_ => {
                _searchText = _searchField.value ?? string.Empty;
                Refresh();
            });
            filterSlot.Add(_searchField);

            VisualElement toggleRow = new();
            toggleRow.AddToClassList("inlo-row");
            toggleRow.style.marginTop = 4;

            _errorsOnlyToggle = new Toggle("Errors Only");
            _errorsOnlyToggle.AddToClassList("inlo-field");
            _errorsOnlyToggle.style.marginRight = 16;
            _errorsOnlyToggle.RegisterValueChangedCallback(_ => Refresh());

            _warningsOnlyToggle = new Toggle("Warnings Only");
            _warningsOnlyToggle.AddToClassList("inlo-field");
            _warningsOnlyToggle.RegisterValueChangedCallback(_ => Refresh());

            toggleRow.Add(_errorsOnlyToggle);
            toggleRow.Add(_warningsOnlyToggle);
            filterSlot.Add(toggleRow);
        }

        private void PopulateSummarySection(VisualElement summarySlot)
        {
            summarySlot.Add(InloUIFactory.CreateSectionLabel("Source Status Overview"));

            _summaryLabel = new Label("No active source.");
            _summaryLabel.AddToClassList("inlo-summary-label");
            summarySlot.Add(_summaryLabel);
        }

        private void PopulateEntriesSection(VisualElement entriesSlot)
        {
            entriesSlot.Add(InloUIFactory.CreateSectionLabel("Pool Entry Items"));

            _entryList = new ScrollView { name = "pool-browser-scroll-view" };
            _entryList.AddToClassList("inlo-list");
            _entryList.style.marginTop = 6;
            _entryList.style.flexGrow = 1;

            entriesSlot.Add(_entryList);
        }

        public void SetSource(Object source)
        {
            if (source is PoolDatabase database)
            {
                _database = database;
                _group = null;
            }
            else if (source is PoolDatabaseGroup group)
            {
                _database = null;
                _group = group;
            }
            else
            {
                _database = null;
                _group = null;
            }
        }

        public void TryUseSelectedAsset()
        {
            SetSource(Selection.activeObject);
        }

        public void RefreshSourceField()
        {
            if (_sourceField != null)
            {
                _sourceField.SetValueWithoutNotify(GetCurrentObject());
            }

            if (_sourceTypeLabel != null)
            {
                if (_database != null)
                    _sourceTypeLabel.text = "PoolDatabase";
                else if (_group != null)
                    _sourceTypeLabel.text = "PoolDatabaseGroup";
                else
                    _sourceTypeLabel.text = "None Selected";
            }
        }

        private Object GetCurrentObject()
        {
            if (_group != null) return _group;
            return _database;
        }

        public void Refresh()
        {
            if (_summaryLabel == null || _entryList == null) return;

            _entryList.Clear();
            List<BrowserEntry> entries = BuildEntries();

            if (_database == null && _group == null)
            {
                _summaryLabel.text = "No active database or database group selection.";
                _entryList.Add(CreateEmptyState("Select a PoolDatabase or PoolDatabaseGroup asset and click Use Selection."));
                return;
            }

            int totalCount = entries.Count;
            int visibleCount = 0;
            int errorCount = 0;
            int warningCount = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                if (HasSeverity(entries[i].Messages, PoolDatabaseValidationSeverity.Error))
                {
                    errorCount++;
                }
                if (HasSeverity(entries[i].Messages, PoolDatabaseValidationSeverity.Warning))
                {
                    warningCount++;
                }
            }

            _entryList.Add(CreateHeader());

            for (int i = 0; i < entries.Count; i++)
            {
                BrowserEntry entry = entries[i];
                if (!ShouldShowEntry(entry)) continue;

                _entryList.Add(CreateEntryRow(entry));
                visibleCount++;
            }

            string sourceName = _group != null ? $"Group: {_group.name}" : $"Database: {_database.name}";
            _summaryLabel.text = $"{sourceName}  |  Total Entries: {totalCount}  |  Matching: {visibleCount}  |  Errors: {errorCount}  |  Warnings: {warningCount}";

            if (visibleCount == 0)
            {
                _entryList.Add(CreateEmptyState("No pool entries match the active search and filter conditions."));
            }
        }

        private List<BrowserEntry> BuildEntries()
        {
            List<BrowserEntry> result = new();
            if (_group != null)
            {
                IReadOnlyList<PoolDatabase> databases = _group.Databases;
                if (databases != null)
                {
                    HashSet<PoolDatabase> visited = new();
                    for (int i = 0; i < databases.Count; i++)
                    {
                        PoolDatabase database = databases[i];
                        if (database == null || !visited.Add(database)) continue;

                        AddDatabaseEntries(result, database, i);
                    }
                }
            }
            else if (_database != null)
            {
                AddDatabaseEntries(result, _database, 0);
            }
            return result;
        }

        private static void AddDatabaseEntries(List<BrowserEntry> result, PoolDatabase database, int databaseIndex)
        {
            PoolDatabaseValidationResult validationResult = PoolDatabaseValidator.Validate(database);
            Dictionary<int, List<PoolDatabaseValidationMessage>> messagesByEntry = BuildMessagesByEntry(validationResult);

            IReadOnlyList<PoolEntry> entries = database.Entries;
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    messagesByEntry.TryGetValue(i, out List<PoolDatabaseValidationMessage> messages);
                    result.Add(new BrowserEntry(database, databaseIndex, entries[i], i, messages));
                }
            }
        }

        private static Dictionary<int, List<PoolDatabaseValidationMessage>> BuildMessagesByEntry(PoolDatabaseValidationResult result)
        {
            Dictionary<int, List<PoolDatabaseValidationMessage>> messagesByEntry = new();
            for (int i = 0; i < result.Messages.Count; i++)
            {
                PoolDatabaseValidationMessage message = result.Messages[i];
                if (message.EntryIndex < 0) continue;

                if (!messagesByEntry.TryGetValue(message.EntryIndex, out List<PoolDatabaseValidationMessage> list))
                {
                    list = new List<PoolDatabaseValidationMessage>();
                    messagesByEntry.Add(message.EntryIndex, list);
                }
                list.Add(message);
            }
            return messagesByEntry;
        }

        private bool ShouldShowEntry(BrowserEntry browserEntry)
        {
            PoolEntry entry = browserEntry.Entry;

            if (_errorsOnlyToggle != null && _errorsOnlyToggle.value)
            {
                if (!HasSeverity(browserEntry.Messages, PoolDatabaseValidationSeverity.Error)) return false;
            }

            if (_warningsOnlyToggle != null && _warningsOnlyToggle.value)
            {
                if (!HasSeverity(browserEntry.Messages, PoolDatabaseValidationSeverity.Warning)) return false;
            }

            if (string.IsNullOrWhiteSpace(_searchText)) return true;

            string search = _searchText.Trim().ToLowerInvariant();
            string databaseName = browserEntry.Database != null ? browserEntry.Database.name : string.Empty;
            string key = entry?.PoolKeyValue ?? string.Empty;
            string prefabName = entry?.Prefab != null ? entry.Prefab.name : string.Empty;

            return databaseName.ToLowerInvariant().Contains(search) ||
                   key.ToLowerInvariant().Contains(search) ||
                   prefabName.ToLowerInvariant().Contains(search);
        }

        private static bool HasSeverity(List<PoolDatabaseValidationMessage> messages, PoolDatabaseValidationSeverity severity)
        {
            if (messages == null) return false;
            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].Severity == severity) return true;
            }
            return false;
        }

        private static VisualElement CreateHeader()
        {
            VisualElement row = CreateRowContainer(true);
            row.Add(CreateCell("DB Source", 140, true));
            row.Add(CreateCell("#", 30, true));
            row.Add(CreateCell("Status", 70, true));
            row.Add(CreateCell("PoolKey Value", 200, true));
            row.Add(CreateCell("Linked Prefab", 160, true));
            row.Add(CreateCell("Preload", 60, true));
            row.Add(CreateCell("Max Limit", 65, true));
            row.Add(CreateCell("Policy", 100, true));
            row.Add(CreateCell("Validation Notes & Recommendation", 380, true));
            return row;
        }

        private static VisualElement CreateEntryRow(BrowserEntry browserEntry)
        {
            PoolEntry entry = browserEntry.Entry;
            List<PoolDatabaseValidationMessage> messages = browserEntry.Messages;

            VisualElement row = CreateRowContainer(false);

            bool hasError = HasSeverity(messages, PoolDatabaseValidationSeverity.Error);
            bool hasWarning = !hasError && HasSeverity(messages, PoolDatabaseValidationSeverity.Warning);
            bool isOk = !hasError && !hasWarning;

            row.EnableInClassList("inlo-list-item--error", hasError);
            row.EnableInClassList("inlo-list-item--warning", hasWarning);
            row.EnableInClassList("inlo-list-item--ok", isOk);

            string status = hasError ? "ERROR" : (hasWarning ? "WARN" : "OK");
            string notes = GetNotesText(messages);
            string databaseName = browserEntry.Database != null ? browserEntry.Database.name : "(Missing DB)";

            row.Add(CreateCell(databaseName, 140));
            row.Add(CreateCell(browserEntry.EntryIndex.ToString(), 30));

            VisualElement statusCell = new();
            statusCell.AddToClassList("inlo-cell");
            statusCell.style.width = 70;

            Label statusBadge = new(status);
            statusBadge.AddToClassList("inlo-badge");
            statusBadge.EnableInClassList("inlo-badge--error", hasError);
            statusBadge.EnableInClassList("inlo-badge--warning", hasWarning);
            statusBadge.EnableInClassList("inlo-badge--ok", isOk);
            statusCell.Add(statusBadge);
            row.Add(statusCell);

            row.Add(CreateCell(entry?.PoolKeyValue ?? "(null)", 200));
            row.Add(CreateCell(entry?.Prefab != null ? entry.Prefab.name : "(Missing)", 160));
            row.Add(CreateCell(entry != null ? entry.PreloadCount.ToString() : "-", 60));
            row.Add(CreateCell(entry != null ? entry.MaxCount.ToString() : "-", 65));
            row.Add(CreateCell(entry != null ? entry.OverflowPolicy.ToString() : "-", 100));
            row.Add(CreateCell(notes, 380));

            return row;
        }

        private static string GetNotesText(List<PoolDatabaseValidationMessage> messages)
        {
            if (messages == null || messages.Count == 0) return string.Empty;
            List<string> parts = new();
            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].Severity == PoolDatabaseValidationSeverity.Info) continue;
                parts.Add(messages[i].Message);
            }
            return parts.Count == 0 ? string.Empty : string.Join(" | ", parts);
        }

        private static VisualElement CreateRowContainer(bool isHeader)
        {
            VisualElement row = new();
            row.AddToClassList("inlo-list-item");
            if (isHeader) row.AddToClassList("inlo-list-item--header");
            return row;
        }

        private static Label CreateCell(string text, float width, bool bold = false)
        {
            Label label = new(text);
            label.AddToClassList("inlo-cell");
            label.style.width = width;
            if (bold) label.AddToClassList("inlo-cell--header");
            return label;
        }

        private static Label CreateEmptyState(string text)
        {
            Label label = new(text);
            label.AddToClassList("inlo-empty");
            return label;
        }
    }
}
