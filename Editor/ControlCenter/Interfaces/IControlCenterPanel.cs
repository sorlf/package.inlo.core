namespace INLO.Core.EditorUI.Editor
{
    public interface IControlCenterPanel
    {
        void Initialize(InloControlCenterWindow window, UnityEngine.UIElements.VisualElement root);
        void OnPanelEnabled();
        void OnPanelDisabled();
        void UpdateUI();
    }
}
