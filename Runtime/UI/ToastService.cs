using UnityEngine;

namespace INLO.Core.UI
{
    [DisallowMultipleComponent]
    public sealed class ToastService : MonoBehaviour
    {
        [SerializeField] private UiLayerRegistry layerRegistry;

        private void Awake()
        {
            if (layerRegistry == null)
            {
                layerRegistry = GetComponentInChildren<UiLayerRegistry>(true);
            }
        }

        public void Configure(UiLayerRegistry registry)
        {
            layerRegistry = registry;
        }

        public GameObject Show(GameObject toastPrefab)
        {
            if (toastPrefab == null)
            {
                Debug.LogError("[ToastService] Toast prefab is null.");
                return null;
            }

            Transform parent = null;
            if (layerRegistry != null)
            {
                layerRegistry.TryGetLayer(UiLayerId.Toast, out parent);
            }

            return Instantiate(toastPrefab, parent);
        }
    }
}
