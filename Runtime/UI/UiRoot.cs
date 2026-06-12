using UnityEngine;

namespace INLO.Core.UI
{
    [DisallowMultipleComponent]
    public sealed class UiRoot : MonoBehaviour
    {
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private UiLayerRegistry layerRegistry;

        public Canvas RootCanvas => rootCanvas;
        public RectTransform SafeAreaRoot => safeAreaRoot;
        public UiLayerRegistry LayerRegistry => layerRegistry;

        private void Awake()
        {
            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInChildren<Canvas>(true);
            }

            if (layerRegistry == null)
            {
                layerRegistry = GetComponentInChildren<UiLayerRegistry>(true);
            }
        }
    }
}
