using INLO.Core.UI;
using NUnit.Framework;
using UnityEngine;

namespace INLO.Core.Tests
{
    public sealed class UiLayerServiceTests
    {
        [Test]
        public void PopupService_InstantiatesPopupUnderPopupLayer()
        {
            GameObject root = new("UiRoot");
            GameObject prefab = new("PopupPrefab");

            try
            {
                Transform popupLayer = new GameObject("PopupLayer").transform;
                popupLayer.SetParent(root.transform);

                UiLayerRegistry registry = root.AddComponent<UiLayerRegistry>();
                registry.Configure(popup: popupLayer);

                PopupService service = root.AddComponent<PopupService>();
                service.Configure(registry);

                GameObject instance = service.Open(prefab);

                Assert.That(instance, Is.Not.Null);
                Assert.That(instance.transform.parent, Is.EqualTo(popupLayer));

                Object.DestroyImmediate(instance);
            }
            finally
            {
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ToastService_InstantiatesToastUnderToastLayer()
        {
            GameObject root = new("UiRoot");
            GameObject prefab = new("ToastPrefab");

            try
            {
                Transform toastLayer = new GameObject("ToastLayer").transform;
                toastLayer.SetParent(root.transform);

                UiLayerRegistry registry = root.AddComponent<UiLayerRegistry>();
                registry.Configure(toast: toastLayer);

                ToastService service = root.AddComponent<ToastService>();
                service.Configure(registry);

                GameObject instance = service.Show(prefab);

                Assert.That(instance, Is.Not.Null);
                Assert.That(instance.transform.parent, Is.EqualTo(toastLayer));

                Object.DestroyImmediate(instance);
            }
            finally
            {
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(root);
            }
        }
    }
}
