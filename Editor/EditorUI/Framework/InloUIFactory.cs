using UnityEngine;
using UnityEngine.UIElements;

namespace INLO.Core.EditorUI.Editor
{
    /// <summary>
    /// 에디터 UI 요소들을 일관된 테마(USS) 규칙에 맞춰 안전하고 빠르게 생성하는 정적 팩토리 클래스입니다.
    /// </summary>
    public static class InloUIFactory
    {
        public static VisualElement CreateCard()
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("inlo-card");
            return card;
        }

        public static Label CreateSectionLabel(string text)
        {
            Label label = new Label(text);
            label.AddToClassList("inlo-card-title");
            return label;
        }

        public static VisualElement CreateKeyValue(string key, string value)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("inlo-kv");

            Label keyLabel = new Label(key);
            keyLabel.AddToClassList("inlo-kv__key");

            Label valueLabel = new Label(value);
            valueLabel.AddToClassList("inlo-kv__value");

            row.Add(keyLabel);
            row.Add(valueLabel);

            return row;
        }

        public static Label CreateInfoLabel(string text, Color backgroundColor)
        {
            Label label = new Label(text);
            label.AddToClassList("inlo-notice");
            label.style.backgroundColor = backgroundColor;
            return label;
        }

        public static VisualElement CreateWorkflowStep(int number, string text)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("inlo-workflow-step");

            Label numberLabel = new Label(number.ToString());
            numberLabel.AddToClassList("inlo-workflow-step__number");

            Label textLabel = new Label(text);
            textLabel.AddToClassList("inlo-workflow-step__text");

            row.Add(numberLabel);
            row.Add(textLabel);

            return row;
        }

        public static Label CreateBadge(string text, Color backgroundColor)
        {
            Label label = new Label(text);
            label.AddToClassList("inlo-badge");
            label.style.backgroundColor = backgroundColor;
            return label;
        }

        public static VisualElement CreateHighlightBar()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("inlo-control-row");
            return row;
        }

        public static VisualElement CreateButtonRow()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("inlo-button-row");
            return row;
        }

        public static Button CreateDefaultButton(string text, System.Action action)
        {
            Button button = new Button(action) { text = text };
            button.AddToClassList("inlo-button");
            return button;
        }

        public static Button CreateAccentButton(string text, System.Action action, string tooltip = "주요 작업 버튼입니다.")
        {
            Button button = new Button(action) { text = text };
            button.tooltip = tooltip;
            button.AddToClassList("inlo-button");
            button.AddToClassList("inlo-button--accent");
            return button;
        }

        public static Button CreateDangerButton(string text, System.Action action, string tooltip = "삭제 또는 초기화 작업입니다. 실행 전 내용을 확인하세요.")
        {
            Button button = new Button(action) { text = text };
            button.tooltip = tooltip;
            button.AddToClassList("inlo-button");
            button.AddToClassList("inlo-button--danger");
            return button;
        }

        public static Button CreateTabButton(string text, bool selected, System.Action action)
        {
            Button button = new Button(action) { text = text };
            button.AddToClassList("inlo-tab-button");
            button.EnableInClassList("inlo-tab-button--selected", selected);
            button.EnableInClassList("inlo-tab-button--idle", !selected);
            return button;
        }
    }
}
