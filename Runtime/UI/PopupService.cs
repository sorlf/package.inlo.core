using UnityEngine;

namespace INLO.Core.UI
{
    [DisallowMultipleComponent]
    public sealed class PopupService : MonoBehaviour
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

        public GameObject Open(GameObject popupPrefab)
        {
            if (popupPrefab == null)
            {
                Debug.LogError("[PopupService] Popup prefab is null.");
                return null;
            }

            Transform parent = null;
            if (layerRegistry != null)
            {
                layerRegistry.TryGetLayer(UiLayerId.Popup, out parent);
            }

            return Instantiate(popupPrefab, parent);
        }

        public void Close(GameObject popupInstance)
        {
            if (popupInstance != null)
            {
                Destroy(popupInstance);
            }
        }
    }
}
