using System.Collections.Generic;
using INLO.Core.Events;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using INLO.Core.EditorUI.Editor;

namespace INLO.Core.Events.Editor
{
    public sealed class EventGraphPanel : VisualElement
    {
        private readonly List<EventChannelInfo> channels = new();
        private readonly List<EventChannelInfo> filteredChannels = new();

        private EventChannelInfo selectedChannel;
        private EventChannelUsageScanResult scanResult;

        private TextField searchField;
        private ListView channelListView;
        private ScrollView graphPanel;

        public EventGraphPanel()
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
            body.AddToClassList("inlo-split-container");
            body.style.flexGrow = 1;
            Add(body);

            // 1. 좌측 사이드바 (330px)
            VisualElement sidebar = new();
            sidebar.AddToClassList("inlo-card");
            sidebar.AddToClassList("inlo-sidebar-left");
            sidebar.style.width = 330;
            body.Add(sidebar);

            PopulateSidebar(sidebar);

            // 2. 우측 그래프 워크스페이스
            graphPanel = new ScrollView(ScrollViewMode.VerticalAndHorizontal) { name = "graph-panel-scroll-view" };
            graphPanel.AddToClassList("inlo-card");
            graphPanel.style.flexGrow = 1;
            graphPanel.style.flexShrink = 1;
            graphPanel.style.minWidth = 0;
            body.Add(graphPanel);
        }

        private void PopulateSidebar(VisualElement sidebar)
        {
            sidebar.Add(InloUIFactory.CreateSectionLabel("Channels Catalog"));

            // 검색창 윗줄에 11px 메타 설명 라벨 수직 정렬
            Label searchDesc = new("Search Channel (Filter):") { style = { fontSize = 11, marginBottom = 4 } };
            searchDesc.AddToClassList("inlo-muted");
            sidebar.Add(searchDesc);

            VisualElement searchRow = new();
            searchRow.AddToClassList("inlo-row");
            searchRow.style.marginBottom = 8;

            // 라벨을 걷어내어 가로 공간 100% 꽉 채우기
            searchField = new TextField { name = "search" };
            searchField.AddToClassList("inlo-field--grow");
            searchField.RegisterCallback<FocusOutEvent>(_ => RefreshUI());
            searchRow.Add(searchField);

            Button refreshBtn = InloUIFactory.CreateDefaultButton("Refresh", () =>
            {
                RefreshChannels();
                RefreshUI();
            });
            searchRow.Add(refreshBtn);
            sidebar.Add(searchRow);

            channelListView = new ListView();
            channelListView.style.flexGrow = 1;
            channelListView.style.minHeight = 120;
            channelListView.fixedItemHeight = 84;
            channelListView.selectionType = SelectionType.Single;
            channelListView.makeItem = MakeChannelListItem;
            channelListView.bindItem = BindChannelListItem;
            channelListView.selectionChanged += OnChannelSelectionChanged;
            sidebar.Add(channelListView);
        }

        public void RefreshUI()
        {
            RebuildChannelList();
            RenderGraph();
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

            scanResult = selectedChannel != null ? EventChannelUsageScanner.Scan(selectedChannel) : null;
        }

        private void RebuildChannelList()
        {
            filteredChannels.Clear();
            foreach (EventChannelInfo info in channels)
            {
                string search = searchField != null ? searchField.value : string.Empty;
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string lSearch = search.ToLowerInvariant();
                    bool mName = info.Asset.name.ToLowerInvariant().Contains(lSearch);
                    bool mPath = info.Path.ToLowerInvariant().Contains(lSearch);
                    bool mDesc = !string.IsNullOrEmpty(info.Asset.Description) && info.Asset.Description.ToLowerInvariant().Contains(lSearch);

                    if (!mName && !mPath && !mDesc) continue;
                }
                filteredChannels.Add(info);
            }

            if (channelListView != null)
            {
                channelListView.itemsSource = filteredChannels;
                channelListView.Rebuild();
            }

            if (selectedChannel == null || filteredChannels.Count == 0) return;
            for (int i = 0; i < filteredChannels.Count; i++)
            {
                if (filteredChannels[i].Path == selectedChannel.Path)
                {
                    channelListView.SetSelection(i);
                    break;
                }
            }
        }

        private void OnChannelSelectionChanged(IEnumerable<object> selectedObjects)
        {
            foreach (object obj in selectedObjects)
            {
                selectedChannel = obj as EventChannelInfo;
                scanResult = selectedChannel != null ? EventChannelUsageScanner.Scan(selectedChannel) : null;
                RefreshUI();
                return;
            }
        }

        private void RenderGraph()
        {
            if (graphPanel == null) return;
            graphPanel.Clear();

            if (selectedChannel == null)
            {
                graphPanel.Add(new HelpBox("왼쪽 목록에서 EventChannel을 선택해 주세요.", HelpBoxMessageType.Info));
                return;
            }

            if (scanResult == null)
            {
                scanResult = EventChannelUsageScanner.Scan(selectedChannel);
            }

            graphPanel.Add(CreateSummaryPanel());
            graphPanel.Add(CreateFlowBoard());
            graphPanel.Add(CreateOtherReferencesSection());
        }

        private VisualElement CreateSummaryPanel()
        {
            VisualElement panel = InloUIFactory.CreateCard();
            panel.style.marginBottom = 12;

            Label title = new(selectedChannel.Asset.name);
            title.AddToClassList("inlo-title");
            panel.Add(title);

            VisualElement chipRow = new();
            chipRow.style.flexDirection = FlexDirection.Row;
            chipRow.style.flexWrap = Wrap.Wrap;
            chipRow.style.marginBottom = 8;
            panel.Add(chipRow);

            AddChip(chipRow, GetDescriptionStatusText(selectedChannel), GetDescriptionStatusClass(selectedChannel));
            AddChip(chipRow, GetUsageCountDisplay(scanResult.TotalUsageCount), GetUsageCountBadgeClass(scanResult.TotalUsageCount));
            if (selectedChannel.Asset.IsDebugLogEnabled) AddChip(chipRow, "DEBUG LOG ACTIVE", "inlo-badge--warning");

            panel.Add(InloUIFactory.CreateKeyValue("ScriptableType", selectedChannel.Asset.GetType().Name));
            panel.Add(InloUIFactory.CreateKeyValue("Asset Path", selectedChannel.Path));

            string desc = string.IsNullOrWhiteSpace(selectedChannel.Asset.Description) ? "설명이 비어 있습니다." : selectedChannel.Asset.Description;
            VisualElement descCard = InloUIFactory.CreateCard();
            descCard.style.marginTop = 8;
            panel.Add(descCard);

            Label descTitle = new("Description");
            descTitle.AddToClassList("inlo-card-title");
            descCard.Add(descTitle);

            Label descLbl = new(desc);
            descLbl.AddToClassList("inlo-wrap");
            descCard.Add(descLbl);

            return panel;
        }

        private VisualElement CreateFlowBoard()
        {
            VisualElement board = InloUIFactory.CreateCard();
            board.style.marginBottom = 12;

            Label boardTitle = new("Visual Event Flow Graph");
            boardTitle.AddToClassList("inlo-card-title");
            board.Add(boardTitle);

            VisualElement row = new();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Stretch;
            board.Add(row);

            VisualElement pubCol = CreateFlowColumn($"Publishers ({GetUsageCountByKind(EventChannelUsageKind.PublisherCandidate)})");
            row.Add(pubCol);
            row.Add(CreateArrowConnector());

            VisualElement chanCol = CreateFlowColumn("Event Channel Node");
            row.Add(chanCol);
            row.Add(CreateArrowConnector());

            VisualElement listenCol = CreateFlowColumn($"Listeners ({GetUsageCountByKind(EventChannelUsageKind.Listener)})");
            row.Add(listenCol);

            AddUsageItems(pubCol, EventChannelUsageKind.PublisherCandidate);
            chanCol.Add(CreateChannelNodeCard());
            AddUsageItems(listenCol, EventChannelUsageKind.Listener);

            return board;
        }

        private VisualElement CreateOtherReferencesSection()
        {
            VisualElement panel = InloUIFactory.CreateCard();
            Label title = new($"Other References ({GetUsageCountByKind(EventChannelUsageKind.Reference)})");
            title.AddToClassList("inlo-card-title");
            panel.Add(title);

            AddUsageItems(panel, EventChannelUsageKind.Reference, false);
            return panel;
        }

        private VisualElement CreateFlowColumn(string title)
        {
            VisualElement col = InloUIFactory.CreateCard();
            col.style.flexGrow = 1;
            col.style.flexShrink = 0; // 찌그러짐 100% 방지!
            col.style.flexBasis = new StyleLength(0f);
            col.style.minWidth = 240; // 넉넉히 240px로 지정하여 텍스트와 Select/Ping 버튼이 이쁘게 나오게 함!
            col.style.marginRight = 6;

            Label label = new(title);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 14;
            label.style.marginBottom = 8;
            col.Add(label);

            return col;
        }

        private static VisualElement CreateArrowConnector()
        {
            VisualElement wrap = new();
            wrap.style.width = 28;
            wrap.style.flexShrink = 0;
            wrap.style.justifyContent = Justify.Center;
            wrap.style.alignItems = Align.Center;

            Label arrow = new("→")
            {
                style = { fontSize = 22, unityFontStyleAndWeight = FontStyle.Bold, opacity = 0.8f }
            };
            wrap.Add(arrow);
            return wrap;
        }

        private VisualElement CreateChannelNodeCard()
        {
            VisualElement card = InloUIFactory.CreateCard();
            card.AddToClassList("inlo-card--accent"); // 금색 테두리 포인트

            Label name = new(selectedChannel.Asset.name) { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 15, marginBottom = 4 } };
            card.Add(name);

            Label type = new(selectedChannel.Asset.GetType().Name) { style = { opacity = 0.85f, marginBottom = 6 } };
            card.Add(type);

            Label path = new(selectedChannel.Path) { style = { whiteSpace = WhiteSpace.Normal, opacity = 0.75f } };
            card.Add(path);

            VisualElement btnRow = InloUIFactory.CreateButtonRow();
            btnRow.style.marginTop = 8;
            card.Add(btnRow);

            Button sel = InloUIFactory.CreateDefaultButton("Select", () => Selection.activeObject = selectedChannel.Asset);
            Button ping = InloUIFactory.CreateDefaultButton("Ping", () => EditorGUIUtility.PingObject(selectedChannel.Asset));
            btnRow.Add(sel);
            btnRow.Add(ping);

            return card;
        }

        private void AddUsageItems(VisualElement container, EventChannelUsageKind kind, bool createCards = true)
        {
            List<EventChannelUsageInfo> matches = new();
            foreach (EventChannelUsageInfo info in scanResult.DetailedUsageInfos)
            {
                if (info.Kind == kind) matches.Add(info);
            }

            if (matches.Count == 0)
            {
                container.Add(new HelpBox(GetEmptyText(kind), HelpBoxMessageType.None));
                return;
            }

            foreach (EventChannelUsageInfo info in matches)
            {
                VisualElement item = createCards ? InloUIFactory.CreateCard() : new VisualElement();
                if (createCards) item.AddToClassList(GetUsageKindCardClass(kind));
                item.style.marginBottom = 8;
                container.Add(item);

                Label name = new(info.GameObjectPath) { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 13 } };
                name.AddToClassList("inlo-wrap");
                item.Add(name);

                Label comp = new($"{info.ComponentType}.{info.PropertyPath}") { style = { marginBottom = 4 } };
                comp.AddToClassList("inlo-muted");
                comp.AddToClassList("inlo-wrap");
                item.Add(comp);

                Label path = new(info.AssetPath) { style = { fontSize = 10, opacity = 0.72f, marginBottom = 8 } };
                path.AddToClassList("inlo-wrap");
                item.Add(path);

                VisualElement btnRow = InloUIFactory.CreateButtonRow();
                Button sel = InloUIFactory.CreateDefaultButton("Select", () =>
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
                Button ping = InloUIFactory.CreateDefaultButton("Ping", () =>
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

                btnRow.Add(sel);
                btnRow.Add(ping);
                item.Add(btnRow);
            }
        }

        private int GetUsageCountByKind(EventChannelUsageKind kind)
        {
            int count = 0;
            if (scanResult == null) return count;
            foreach (EventChannelUsageInfo info in scanResult.DetailedUsageInfos)
            {
                if (info.Kind == kind) count++;
            }
            return count;
        }

        private string GetEmptyText(EventChannelUsageKind kind)
        {
            return kind switch
            {
                EventChannelUsageKind.Listener => "수신 리스너가 감지되지 않았습니다.",
                EventChannelUsageKind.PublisherCandidate => "발행자 후보가 존재하지 않습니다.",
                _ => "관련 참조를 찾지 못했습니다."
            };
        }

        private static string GetUsageKindCardClass(EventChannelUsageKind kind)
        {
            return kind switch
            {
                EventChannelUsageKind.Listener => "inlo-card--ok",
                EventChannelUsageKind.PublisherCandidate => "inlo-card--warning",
                _ => "inlo-card--info"
            };
        }

        private static void AddChip(VisualElement parent, string text, string modifier)
        {
            Label b = new(text);
            b.AddToClassList("inlo-badge");
            b.style.marginRight = 4;
            if (!string.IsNullOrEmpty(modifier))
            {
                b.AddToClassList(modifier);
            }
            parent.Add(b);
        }

        private VisualElement MakeChannelListItem()
        {
            VisualElement root = new();
            root.AddToClassList("inlo-list-card");

            Label name = new() { name = "NameLabel", style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 13, marginBottom = 5 } };
            name.style.whiteSpace = WhiteSpace.NoWrap;
            name.style.textOverflow = TextOverflow.Ellipsis;
            root.Add(name);

            VisualElement meta = new() { name = "MetaRow", style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            root.Add(meta);

            return root;
        }

        private void BindChannelListItem(VisualElement element, int index)
        {
            EventChannelInfo info = filteredChannels[index];
            Label name = element.Q<Label>("NameLabel");
            name.text = info.Asset.name;

            VisualElement meta = element.Q<VisualElement>("MetaRow");
            meta.Clear();

            bool isSelected = selectedChannel != null && selectedChannel.Path == info.Path;

            // Browser와 100% 데칼코마니: 선택 하이라이트 활성화 처리
            element.EnableInClassList("inlo-list-card--selected", isSelected);
            element.RemoveFromClassList("inlo-list-card--ok");
            element.RemoveFromClassList("inlo-list-card--warning");
            element.RemoveFromClassList("inlo-list-card--error");
            element.RemoveFromClassList("inlo-list-card--accent");

            if (!isSelected)
            {
                element.AddToClassList(ChannelStatusClass(info));
            }

            AddChip(meta, info.Asset.IsDebugLogEnabled ? "DEBUG ON" : "DEBUG OFF", info.Asset.IsDebugLogEnabled ? "inlo-badge--warning" : null);
            AddChip(meta, GetDescriptionStatusText(info), GetDescriptionStatusClass(info));
            AddChip(meta, GetUsageCountDisplay(info), GetUsageCountBadgeClass(info.UsageCount));
        }

        private static string ChannelStatusClass(EventChannelInfo info)
        {
            if (info.IsUnusedCandidate()) return "inlo-list-card--error";
            if (info.GetDescriptionQuality() == EventChannelDescriptionQuality.Missing) return "inlo-list-card--warning";
            return "inlo-list-card--ok";
        }

        private static string GetDescriptionStatusText(EventChannelInfo info)
        {
            return info.GetDescriptionQuality() switch
            {
                EventChannelDescriptionQuality.Missing => "NO DESC",
                EventChannelDescriptionQuality.TooShort => "SHORT DESC",
                _ => "DESC OK"
            };
        }

        private static string GetDescriptionStatusClass(EventChannelInfo info)
        {
            return info.GetDescriptionQuality() switch
            {
                EventChannelDescriptionQuality.Missing => "inlo-badge--error",
                EventChannelDescriptionQuality.TooShort => "inlo-badge--warning",
                _ => "inlo-badge--ok"
            };
        }

        private static string GetUsageCountDisplay(int count)
        {
            if (count < 0) return "? usages";
            return count == 1 ? "1 usage" : $"{count} usages";
        }

        private static string GetUsageCountDisplay(EventChannelInfo info) { return GetUsageCountDisplay(info.UsageCount); }

        private static string GetUsageCountBadgeClass(int count)
        {
            if (count < 0) return null;
            return count == 0 ? "inlo-badge--error" : "inlo-badge--info";
        }
    }
}
