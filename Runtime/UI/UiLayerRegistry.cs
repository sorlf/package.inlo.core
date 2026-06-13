using UnityEngine;

namespace INLO.Core.UI
{
    [DisallowMultipleComponent]
    public sealed class UiLayerRegistry : MonoBehaviour
    {
        [SerializeField] private Transform hudLayer;
        [SerializeField] private Transform screenLayer;
        [SerializeField] private Transform popupLayer;
        [SerializeField] private Transform toastLayer;
        [SerializeField] private Transform tutorialLayer;
        [SerializeField] private Transform blockerLayer;

        public void Configure(
            Transform hud = null,
            Transform screen = null,
            Transform popup = null,
            Transform toast = null,
            Transform tutorial = null,
            Transform blocker = null)
        {
            hudLayer = hud;
            screenLayer = screen;
            popupLayer = popup;
            toastLayer = toast;
            tutorialLayer = tutorial;
            blockerLayer = blocker;
        }

        public bool TryGetLayer(UiLayerId layerId, out Transform layer)
        {
            layer = GetSerializedLayer(layerId);
            return layer != null;
        }

        public Transform GetSerializedLayer(UiLayerId layerId)
        {
            return layerId switch
            {
                UiLayerId.Hud => hudLayer,
                UiLayerId.Screen => screenLayer,
                UiLayerId.Popup => popupLayer,
                UiLayerId.Toast => toastLayer,
                UiLayerId.Tutorial => tutorialLayer,
                UiLayerId.Blocker => blockerLayer,
                _ => null
            };
        }
    }
}
