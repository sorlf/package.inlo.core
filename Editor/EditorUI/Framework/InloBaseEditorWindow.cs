using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace INLO.Core.EditorUI.Editor
{
    /// <summary>
    /// INLO Core 에디터 윈도우용 베이스 클래스입니다.
    /// 기존 전체 Rebuild 패턴을 대체하고 "1회 쿼리, 프로퍼티 변경" 모델을 표준화합니다.
    /// </summary>
    public abstract class InloBaseEditorWindow : EditorWindow
    {
        protected abstract string UxmlPath { get; }
        protected abstract string UssPath { get; }
        protected abstract string MainScrollViewName { get; }

        protected Vector2 mainScrollOffset = Vector2.zero;

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }

        public virtual void CreateGUI()
        {
            SaveScrollOffset();

            rootVisualElement.Clear();

            // 1. 스타일시트(USS) 로드 및 바인딩
            if (!string.IsNullOrEmpty(UssPath))
            {
                StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
                if (styleSheet != null && !rootVisualElement.styleSheets.Contains(styleSheet))
                {
                    rootVisualElement.styleSheets.Add(styleSheet);
                }
            }

            // 2. 레이아웃(UXML) 로드 및 인스턴스화
            if (!string.IsNullOrEmpty(UxmlPath))
            {
                VisualTreeAsset layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
                if (layout != null)
                {
                    layout.CloneTree(rootVisualElement);
                }
            }

            // 3. UI 엘리먼트 1회 쿼리 및 바인딩
            OnBindElements();

            // 4. 초기 상태 데이터 기반 UI 동기화
            UpdateUI();

            // 5. 이전 스크롤 오프셋이 있다면 복원
            RestoreScrollOffset();
        }

        /// <summary>
        /// 윈도우 로드 시점에 단 1회 실행되며, Q(Query)를 사용하여 변경에 관여할 엘리먼트들을 쿼리하고 필드 변수로 캐싱합니다.
        /// </summary>
        protected abstract void OnBindElements();

        /// <summary>
        /// 데이터 상태가 바뀌었을 때 엘리먼트를 새로 만들지 않고, 캐싱해 둔 필드의 프로퍼티(text, value, display 등)값만 수정하여 화면을 동기화합니다.
        /// </summary>
        public abstract void UpdateUI();

        /// <summary>
        /// 수동으로 스크롤 위치를 기록하고 UI를 갱신합니다.
        /// 엘리먼트가 그대로 유지되는 경우 스크롤 위치가 자동으로 보존되나, 동적 리스트가 대량 갱신되어 스크롤 바운드가 변할 때 유용하게 활용됩니다.
        /// </summary>
        protected void RefreshUI()
        {
            SaveScrollOffset();
            UpdateUI();
            RestoreScrollOffset();
        }

        protected void SaveScrollOffset()
        {
            if (string.IsNullOrEmpty(MainScrollViewName)) return;

            ScrollView scrollView = rootVisualElement.Q<ScrollView>(MainScrollViewName);
            if (scrollView != null)
            {
                mainScrollOffset = scrollView.scrollOffset;
            }
        }

        protected void RestoreScrollOffset()
        {
            if (string.IsNullOrEmpty(MainScrollViewName) || mainScrollOffset == Vector2.zero) return;

            ScrollView scrollView = rootVisualElement.Q<ScrollView>(MainScrollViewName);
            if (scrollView != null)
            {
                scrollView.schedule.Execute(() =>
                {
                    scrollView.scrollOffset = mainScrollOffset;
                });
            }
        }
    }
}
