using System.Collections.Generic;
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

        private readonly Dictionary<UiLayerId, Transform> _layers = new();

        private void Awake()
        {
            Refresh();
        }

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
            Refresh();
        }

        public void Refresh()
        {
            _layers[UiLayerId.Hud] = hudLayer;
            _layers[UiLayerId.Screen] = screenLayer;
            _layers[UiLayerId.Popup] = popupLayer;
            _layers[UiLayerId.Toast] = toastLayer;
            _layers[UiLayerId.Tutorial] = tutorialLayer;
            _layers[UiLayerId.Blocker] = blockerLayer;
        }

        public bool TryGetLayer(UiLayerId layerId, out Transform layer)
        {
            if (_layers.TryGetValue(layerId, out layer) && layer != null)
            {
                return true;
            }

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
