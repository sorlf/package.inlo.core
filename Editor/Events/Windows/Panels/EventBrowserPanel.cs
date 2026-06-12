using System.Collections.Generic;
using INLO.Core.Events;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using INLO.Core.EditorUI.Editor;

namespace INLO.Core.Editor.Events
{
    public sealed class EventBrowserPanel : VisualElement
    {
        private readonly List<EventChannelInfo> channels = new();
        private readonly List<EventChannelInfo> filteredChannels = new();

        private EventChannelInfo selectedChannel;
        private EventChannelUsageScanResult selectedScanResult;

        private TextField searchField;
        private Toggle debugOnlyToggle;
        private Toggle descriptionIssuesOnlyToggle;
        private Toggle unusedOnlyToggle;
        private ListView channelListView;

        private VisualElement _bodyContainer;
        private VisualElement _sidebarContainer;
        private VisualElement _workspaceContainer;
        private ScrollView detailScrollView;
        private ScrollView usageScrollView;
        private VisualElement detailPanel;
        private Label countLabel;

        // 인라인 전환용 패널들
        private EventCreatorPanel _creatorPanel;
        private bool _isCreatorActive;

        public EventBrowserPanel()
        {
            BuildUI();
            RefreshChannels();
            RefreshUI();
        }

        private void BuildUI()
        {
            style.flexGrow = 1;
            style.flexDirection = FlexDirection.Column;

            // F-패턴 가로 분할 스플릿
            VisualElement body = new();
            _bodyContainer = body;
            _bodyContainer.AddToClassList("inlo-split-container");
            _bodyContainer.style.flexGrow = 1;
            Add(_bodyContainer);

            // 1. 좌측 사이드바 (370px 고정)
            VisualElement sidebar = new();
            _sidebarContainer = sidebar;
            _sidebarContainer.AddToClassList("inlo-card");
            _sidebarContainer.AddToClassList("inlo-sidebar-left");
            _sidebarContainer.style.width = 370;
            _bodyContainer.Add(_sidebarContainer);

            PopulateSidebar(_sidebarContainer);

            // 2. 우측 워크스페이스 컨테이너
            _workspaceContainer = new VisualElement();
            _workspaceContainer.AddToClassList("inlo-workspace-right");
            _bodyContainer.Add(_workspaceContainer);

            // 브라우저 기본 우측 뷰 트리 뼈대
            detailScrollView = new ScrollView { name = "browser-detail" };
            detailScrollView.AddToClassList("inlo-card");
            detailScrollView.AddToClassList("inlo-card--grow");
            detailScrollView.style.minHeight = 280;

            detailPanel = new VisualElement();
            detailPanel.style.flexGrow = 1;
            detailScrollView.Add(detailPanel);

            usageScrollView = new ScrollView { name = "browser-usage" };
            usageScrollView.AddToClassList("inlo-card");
            usageScrollView.style.height = 310;
            usageScrollView.style.marginTop = 8;

            RestoreBrowserViews();
        }

        private void RestoreBrowserViews()
        {
            if (_bodyContainer != null)
            {
                _bodyContainer.Clear();
                if (_sidebarContainer != null) _bodyContainer.Add(_sidebarContainer);
                if (_workspaceContainer != null) _bodyContainer.Add(_workspaceContainer);
            }

            _workspaceContainer.Clear();
            _workspaceContainer.Add(detailScrollView);
            _workspaceContainer.Add(usageScrollView);
            _isCreatorActive = false;
            RefreshUI();
        }

        private void ShowCreatorPanel()
        {
            if (_creatorPanel == null)
            {
                _creatorPanel = new EventCreatorPanel(() =>
                {
                    // 채널 생성 성공 완료 콜백
                    RefreshChannels();
                    RefreshUI();
                    RestoreBrowserViews();
                });
                
                // 크리에이터 상단 툴바에 Back to Browser 용도로 커스텀 닫기 행을 하나 넣어줍니다.
                VisualElement closeRow = new();
                closeRow.AddToClassList("inlo-row");
                closeRow.style.marginBottom = 12;

                Button backBtn = InloUIFactory.CreateDefaultButton("◀ Back to Channel Browser", RestoreBrowserViews);
                backBtn.style.flexGrow = 1;
                closeRow.Add(backBtn);

                _creatorPanel.Insert(0, closeRow);
            }

            if (_bodyContainer != null)
            {
                _bodyContainer.Clear();
                _bodyContainer.Add(_workspaceContainer);
            }

            _workspaceContainer.Clear();
            _workspaceContainer.Add(_creatorPanel);
            _isCreatorActive = true;
        }

        private void PopulateSidebar(VisualElement sidebar)
        {
            sidebar.Add(InloUIFactory.CreateSectionLabel("Channels Database"));

            // 검색 필드 위에 11px 메타 안내 라벨 얹기
            Label searchDesc = new("Search Channel (Filter):") { style = { fontSize = 11, marginBottom = 4 } };
            searchDesc.AddToClassList("inlo-muted");
            sidebar.Add(searchDesc);

            // 툴바/필터들을 사이드바 내부에 집약
            VisualElement searchRow = new();
            searchRow.AddToClassList("inlo-row");
            searchRow.style.marginBottom = 8;

            // 라벨을 삭제하여 입력란 가로폭 100% 확보
            searchField = new TextField { name = "search" };
            searchField.AddToClassList("inlo-field--grow");
            searchField.RegisterCallback<FocusOutEvent>(_ => RefreshUI());
            searchRow.Add(searchField);

            Button creatorOpenBtn = InloUIFactory.CreateAccentButton("+ Create", ShowCreatorPanel);
            creatorOpenBtn.style.marginLeft = 6;
            searchRow.Add(creatorOpenBtn);
            sidebar.Add(searchRow);

            VisualElement toggleCol = new();
            toggleCol.style.flexDirection = FlexDirection.Column;
            toggleCol.style.marginBottom = 8;

            debugOnlyToggle = new Toggle("Debug Log Enabled");
            debugOnlyToggle.RegisterValueChangedCallback(_ => RefreshUI());
            toggleCol.Add(debugOnlyToggle);

            descriptionIssuesOnlyToggle = new Toggle("Description Issues Only");
            descriptionIssuesOnlyToggle.RegisterValueChangedCallback(_ => RefreshUI());
            toggleCol.Add(descriptionIssuesOnlyToggle);

            unusedOnlyToggle = new Toggle("Unused Candidates");
            unusedOnlyToggle.RegisterValueChangedCallback(_ => RefreshUI());
            toggleCol.Add(unusedOnlyToggle);

            sidebar.Add(toggleCol);

            countLabel = new Label("Showing 0 / Total 0");
            countLabel.AddToClassList("inlo-muted");
            countLabel.style.marginBottom = 8;
            sidebar.Add(countLabel);

            channelListView = new ListView();
            channelListView.style.flexGrow = 1;
            channelListView.style.minHeight = 120;
            channelListView.fixedItemHeight = 84;
            channelListView.makeItem = MakeChannelListItem;
            channelListView.bindItem = BindChannelListItem;
            channelListView.selectionType = SelectionType.Single;
            channelListView.selectionChanged += OnChannelSelectionChanged;
            sidebar.Add(channelListView);
        }

        public void RefreshUI()
        {
            if (_isCreatorActive) return;

            RebuildChannelList();
            RefreshDetails();
            RefreshUsagePanel();
        }

        private void RefreshChannels()
        {
            string prev = selectedChannel != null ? selectedChannel.Path : string.Empty;
            channels.Clear();

            string[] guids = AssetDatabase.FindAssets("t:EventChannelBaseSO");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EventChannelBaseSO asset = AssetDatabase.LoadAssetAtPath<EventChannelBaseSO>(path);
                if (asset == null) continue;
                channels.Add(new EventChannelInfo(asset, path));
            }

            channels.Sort((a, b) => string.Compare(a.Asset.name, b.Asset.name, System.StringComparison.Ordinal));
            selectedChannel = null;

            if (!string.IsNullOrEmpty(prev))
            {
                foreach (EventChannelInfo info in channels)
                {
                    if (info.Path == prev)
                    {
                        selectedChannel = info;
                        break;
                    }
                }
            }

            if (selectedChannel == null && channels.Count > 0)
            {
                selectedChannel = channels[0];
            }
        }

        private void RebuildChannelList()
        {
            filteredChannels.Clear();
            foreach (EventChannelInfo info in channels)
            {
                if (debugOnlyToggle != null && debugOnlyToggle.value && !info.Asset.IsDebugLogEnabled) continue;
                if (descriptionIssuesOnlyToggle != null && descriptionIssuesOnlyToggle.value && info.GetDescriptionQuality() == EventChannelDescriptionQuality.Ok) continue;
                if (unusedOnlyToggle != null && unusedOnlyToggle.value && !info.IsUnusedCandidate()) continue;

                string search = searchField != null ? searchField.value : string.Empty;
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string lSearch = search.ToLowerInvariant();
                    bool mName = info.Asset.name.ToLowerInvariant().Contains(lSearch);
                    bool mPath = info.Path.ToLowerInvariant().Contains(lSearch);
                    bool mType = info.Asset.GetType().Name.ToLowerInvariant().Contains(lSearch);
                    bool mDesc = !string.IsNullOrEmpty(info.Asset.Description) && info.Asset.Description.ToLowerInvariant().Contains(lSearch);

                    if (!mName && !mPath && !mType && !mDesc) continue;
                }

                filteredChannels.Add(info);
            }

            if (countLabel != null) countLabel.text = $"Showing {filteredChannels.Count} / Total {channels.Count}";
            if (channelListView != null)
            {
                channelListView.itemsSource = filteredChannels;
                channelListView.Rebuild();
            }
        }

        private void RefreshDetails()
        {
            detailPanel.Clear();
            detailPanel.Add(InloUIFactory.CreateSectionLabel("Channel Details"));

            if (selectedChannel == null)
            {
                detailPanel.Add(CreateEmptyState("사이드바 채널 목록에서 EventChannel을 선택해 주세요."));
                return;
            }

            EventChannelBaseSO asset = selectedChannel.Asset;

            VisualElement titleRow = new();
            titleRow.AddToClassList("inlo-row");
            titleRow.style.marginBottom = 8;
            detailPanel.Add(titleRow);

            Label title = new(asset.name);
            title.AddToClassList("inlo-title");
            title.style.flexGrow = 1;
            titleRow.Add(title);

            if (asset.IsDebugLogEnabled) titleRow.Add(CreateBadge("DEBUG LOG ON", "inlo-badge--info"));
            if (selectedChannel.IsUnusedCandidate()) titleRow.Add(CreateBadge("UNUSED CANDIDATE", "inlo-badge--error"));

            VisualElement meta = InloUIFactory.CreateCard();
            detailPanel.Add(meta);

            AddMetaRow(meta, "Channel SO Type", asset.GetType().Name);
            AddMetaRow(meta, "Scriptable Path", selectedChannel.Path);
            AddMetaRow(meta, "Active Usages", selectedChannel.UsageCount < 0 ? "Not scanned yet" : selectedChannel.UsageCount.ToString());
            AddMetaRow(meta, "Description Quality", selectedChannel.GetDescriptionQuality().ToString());

            Toggle debugToggle = new Toggle("Activate Debug Logging") { value = asset.IsDebugLogEnabled };
            debugToggle.style.marginTop = 8;
            debugToggle.RegisterValueChangedCallback(evt =>
            {
                SetDebugLog(asset, evt.newValue);
                RebuildChannelList();
                RefreshDetails();
            });
            detailPanel.Add(debugToggle);

            detailPanel.Add(InloUIFactory.CreateSectionLabel("Description Manual"));
            bool empty = string.IsNullOrWhiteSpace(asset.Description);
            Label descLabel = new(empty ? "Description이 비어 있습니다. 해당 채널이 동작하는 시나리오를 작성해 주세요." : asset.Description);
            descLabel.AddToClassList("inlo-notice");
            descLabel.EnableInClassList("inlo-notice--warning", empty);
            descLabel.EnableInClassList("inlo-notice--ok", !empty);
            descLabel.style.marginTop = 4;
            detailPanel.Add(descLabel);

            // 액션 버튼 모음
            VisualElement btnRow = InloUIFactory.CreateButtonRow();
            btnRow.style.marginTop = 12;

            Button selBtn = InloUIFactory.CreateDefaultButton("Select Asset", () => Selection.activeObject = asset);
            selBtn.style.flexGrow = 1;
            Button pingBtn = InloUIFactory.CreateDefaultButton("Ping Object", () => EditorGUIUtility.PingObject(asset));
            pingBtn.style.flexGrow = 1;

            Button findBtn = InloUIFactory.CreateAccentButton("Scan Usages", () =>
            {
                selectedScanResult = EventChannelUsageScanner.Scan(selectedChannel);
                selectedChannel.UsageCount = selectedScanResult.TotalUsageCount;
                RebuildChannelList();
                RefreshDetails();
                RefreshUsagePanel();
            });
            findBtn.style.flexGrow = 1.2f;

            btnRow.Add(selBtn);
            btnRow.Add(pingBtn);
            btnRow.Add(findBtn);
            detailPanel.Add(btnRow);
        }

        private void RefreshUsagePanel()
        {
            usageScrollView.Clear();
            usageScrollView.Add(InloUIFactory.CreateSectionLabel("Active Usage Scans"));

            if (selectedChannel == null)
            {
                usageScrollView.Add(CreateEmptyState("채널을 선택하세요."));
                return;
            }

            if (selectedScanResult == null)
            {
                usageScrollView.Add(CreateEmptyState("오른쪽 [Scan Usages] 버튼을 누르면 프로젝트 내의 Prefab, Scene 사용처를 검출합니다."));
                return;
            }

            AddUsageGroup("Open Scene / Event Listener", "Open Scene", EventChannelUsageKind.Listener);
            AddUsageGroup("Open Scene / Event Publisher", "Open Scene", EventChannelUsageKind.PublisherCandidate);
            AddUsageGroup("Open Scene / General Reference", "Open Scene", EventChannelUsageKind.Reference);

            AddUsageGroup("Prefab Asset / Event Listener", "Prefab", EventChannelUsageKind.Listener);
            AddUsageGroup("Prefab Asset / Event Publisher", "Prefab", EventChannelUsageKind.PublisherCandidate);
            AddUsageGroup("Prefab Asset / General Reference", "Prefab", EventChannelUsageKind.Reference);

            AddAssetDependencies();
        }

        private void AddUsageGroup(string title, string sourceType, EventChannelUsageKind kind)
        {
            List<EventChannelUsageInfo> items = new();
            foreach (EventChannelUsageInfo info in selectedScanResult.DetailedUsageInfos)
            {
                if (info.SourceType == sourceType && info.Kind == kind) items.Add(info);
            }
            if (items.Count == 0) return;

            VisualElement group = CreateUsageGroup(title, items.Count, KindBadgeClass(kind));
            usageScrollView.Add(group);

            foreach (EventChannelUsageInfo info in items)
            {
                group.Add(CreateUsageRow(info));
            }
        }

        private VisualElement CreateUsageRow(EventChannelUsageInfo info)
        {
            VisualElement row = new();
            row.AddToClassList("inlo-list-card");
            row.AddToClassList(KindCardClass(info.Kind));

            Label objLine = new(info.GameObjectPath);
            objLine.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(objLine);

            Label compLine = new($"{info.ComponentType}  /  Field: {info.PropertyPath}");
            compLine.AddToClassList("inlo-muted");
            row.Add(compLine);

            Label pathLine = new(info.AssetPath);
            pathLine.AddToClassList("inlo-muted");
            pathLine.style.fontSize = 10;
            row.Add(pathLine);

            VisualElement btnRow = InloUIFactory.CreateButtonRow();
            btnRow.style.marginTop = 6;

            Button selBtn = InloUIFactory.CreateDefaultButton("Select Object", () =>
            {
                if (info.TargetObject != null)
                {
                    Selection.activeObject = info.TargetObject;
                }
                else if (info.SourceType == "Prefab" && !string.IsNullOrEmpty(info.AssetPath))
                {
                    GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(info.AssetPath);
                    if (prefabAsset != null) Selection.activeObject = prefabAsset;
                }
            });
            Button pingBtn = InloUIFactory.CreateDefaultButton("Ping", () =>
            {
                if (info.TargetObject != null)
                {
                    EditorGUIUtility.PingObject(info.TargetObject);
                }
                else if (info.SourceType == "Prefab" && !string.IsNullOrEmpty(info.AssetPath))
                {
                    GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(info.AssetPath);
                    if (prefabAsset != null) EditorGUIUtility.PingObject(prefabAsset);
                }
            });

            btnRow.Add(selBtn);
            btnRow.Add(pingBtn);
            row.Add(btnRow);

            return row;
        }

        private void AddAssetDependencies()
        {
            if (selectedScanResult.AssetDependencyPaths.Count == 0) return;

            VisualElement group = CreateUsageGroup("Project Asset Dependencies", selectedScanResult.AssetDependencyPaths.Count, null);
            usageScrollView.Add(group);

            foreach (string path in selectedScanResult.AssetDependencyPaths)
            {
                VisualElement row = new();
                row.AddToClassList("inlo-list-card");
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                group.Add(row);

                Label lbl = new(path) { style = { flexGrow = 1 } };
                lbl.AddToClassList("inlo-wrap");
                row.Add(lbl);

                Button sel = InloUIFactory.CreateDefaultButton("Select", () =>
                {
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (asset != null) Selection.activeObject = asset;
                });
                Button ping = InloUIFactory.CreateDefaultButton("Ping", () =>
                {
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (asset != null) EditorGUIUtility.PingObject(asset);
                });

                row.Add(sel);
                row.Add(ping);
            }
        }

        private VisualElement CreateUsageGroup(string title, int count, string badgeClass)
        {
            VisualElement group = new();
            group.style.marginTop = 10;

            VisualElement header = new();
            header.AddToClassList("inlo-row");
            header.style.marginBottom = 4;
            group.Add(header);

            Label titleLbl = new(title);
            titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLbl.style.flexGrow = 1;
            header.Add(titleLbl);

            header.Add(CreateBadge($"{count} matches", badgeClass));
            return group;
        }

        private static string KindBadgeClass(EventChannelUsageKind kind)
        {
            return kind switch
            {
                EventChannelUsageKind.Listener => "inlo-badge--ok",
                EventChannelUsageKind.PublisherCandidate => "inlo-badge--warning",
                _ => "inlo-badge--info"
            };
        }

        private static string KindCardClass(EventChannelUsageKind kind)
        {
            return kind switch
            {
                EventChannelUsageKind.Listener => "inlo-list-card--ok",
                EventChannelUsageKind.PublisherCandidate => "inlo-list-card--warning",
                _ => "inlo-list-card--info"
            };
        }

        private void SetDebugLog(EventChannelBaseSO asset, bool value)
        {
            SerializedObject serializedObject = new(asset);
            SerializedProperty debugLogProperty = serializedObject.FindProperty("debugLog");
            if (debugLogProperty != null)
            {
                serializedObject.Update();
                debugLogProperty.boolValue = value;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }
        }

        private void OnChannelSelectionChanged(IEnumerable<object> selectedObjects)
        {
            foreach (object obj in selectedObjects)
            {
                selectedChannel = obj as EventChannelInfo;
                selectedScanResult = null;
                RefreshUI();
                return;
            }
        }

        private VisualElement MakeChannelListItem()
        {
            VisualElement item = new();
            item.AddToClassList("inlo-list-card");

            VisualElement row = new();
            row.AddToClassList("inlo-row");
            item.Add(row);

            Label name = new() { name = "channel-name", style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 13, flexGrow = 1 } };
            name.style.whiteSpace = WhiteSpace.NoWrap;
            name.style.textOverflow = TextOverflow.Ellipsis;
            row.Add(name);

            Label type = new() { name = "channel-type", style = { fontSize = 10, marginLeft = 6 } };
            type.AddToClassList("inlo-muted");
            row.Add(type);

            VisualElement badges = new() { name = "channel-badges", style = { flexDirection = FlexDirection.Row, marginTop = 6 } };
            item.Add(badges);

            return item;
        }

        private void BindChannelListItem(VisualElement element, int index)
        {
            EventChannelInfo info = filteredChannels[index];
            Label name = element.Q<Label>("channel-name");
            Label type = element.Q<Label>("channel-type");
            VisualElement badges = element.Q<VisualElement>("channel-badges");

            bool isSelected = selectedChannel != null && selectedChannel.Path == info.Path;

            element.EnableInClassList("inlo-list-card--selected", isSelected);
            element.RemoveFromClassList("inlo-list-card--ok");
            element.RemoveFromClassList("inlo-list-card--warning");
            element.RemoveFromClassList("inlo-list-card--error");
            element.RemoveFromClassList("inlo-list-card--accent");

            if (!isSelected)
            {
                element.AddToClassList(ChannelStatusClass(info));
            }

            name.text = info.Asset.name;
            type.text = info.Asset.GetType().Name.Replace("EventChannelSO", "");

            badges.Clear();
            if (info.Asset.IsDebugLogEnabled) badges.Add(CreateBadge("DEBUG", "inlo-badge--info"));

            EventChannelDescriptionQuality q = info.GetDescriptionQuality();
            if (q == EventChannelDescriptionQuality.Missing) badges.Add(CreateBadge("NO DESC", "inlo-badge--error"));
            else if (q == EventChannelDescriptionQuality.TooShort) badges.Add(CreateBadge("SHORT DESC", "inlo-badge--warning"));
            else badges.Add(CreateBadge("DESC OK", "inlo-badge--ok"));

            if (info.UsageCount >= 0)
            {
                badges.Add(CreateBadge(info.UsageCount == 1 ? "1 usage" : $"{info.UsageCount} usages", info.UsageCount == 0 ? "inlo-badge--warning" : "inlo-badge--ok"));
            }
        }

        private static string ChannelStatusClass(EventChannelInfo info)
        {
            if (info.IsUnusedCandidate()) return "inlo-list-card--error";
            if (info.GetDescriptionQuality() == EventChannelDescriptionQuality.Missing) return "inlo-list-card--warning";
            return "inlo-list-card--ok";
        }

        private static void AddMetaRow(VisualElement parent, string key, string val)
        {
            parent.Add(InloUIFactory.CreateKeyValue(key, string.IsNullOrWhiteSpace(val) ? "-" : val));
        }

        private static Label CreateBadge(string text, string modifier)
        {
            Label b = new(text);
            b.AddToClassList("inlo-badge");
            if (!string.IsNullOrEmpty(modifier))
            {
                b.AddToClassList(modifier);
            }
            return b;
        }

        private static Label CreateEmptyState(string text)
        {
            Label lbl = new(text);
            lbl.AddToClassList("inlo-empty");
            return lbl;
        }
    }
}
