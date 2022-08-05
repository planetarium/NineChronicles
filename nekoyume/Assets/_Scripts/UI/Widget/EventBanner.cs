using System.Collections.Generic;
using Nekoyume.Helper;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EventBanner : Widget
    {
        [SerializeField]
        private RectTransform content = null;

        [SerializeField]
        private RectTransform indexContent = null;

        [SerializeField]
        private GameObject Banner;

        [SerializeField]
        private GameObject IndexOn;

        [SerializeField]
        private GameObject IndexOff;

        [SerializeField]
        private PageView pageView;

        private AsyncOperationHandle _handle;

        private readonly List<GameObject> banners = new();

        protected override void Awake()
        {
            base.Awake();
            AddressablesHelper.LoadAssets("banner", banners, Set);
        }

        private void Set()
        {
            foreach (var banner in banners)
            {
                Instantiate(banner, content);
            }
            var destroyList = new List<GameObject>();
            for (var i = 0; i < content.childCount; i++)
            {
                var eventBannerItem = content.GetChild(i).GetComponent<EventBannerItem>();
                if (eventBannerItem is null)
                {
                    continue;
                }

                if (!eventBannerItem.IsInTime())
                {
                    destroyList.Add(content.GetChild(i).gameObject);
                }
            }

            foreach (var item in destroyList)
            {
                DestroyImmediate(item);
            }

            for (var i = 0; i < content.childCount; i++)
            {
                Instantiate(i == 0 ? IndexOn : IndexOff, indexContent);
            }

            var indexImages = new List<Image>();
            for (var i = 0; i < indexContent.childCount; i++)
            {
                indexImages.Add(indexContent.GetChild(i).GetComponent<Image>());
            }

            pageView.Set(content, indexImages);
        }
    }
}
