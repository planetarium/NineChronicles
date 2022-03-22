using System.Collections.Generic;
using UnityEngine;
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

        private void Awake()
        {
            if (EventManager.TryGetArenaSeasonInfo(Game.Game.instance.Agent.BlockIndex, out var info))
            {
                var banner = Instantiate(Banner, content);
                banner.GetComponent<EventBannerItem>().Set(info.SeasonBanner, info.SeasonUrl);
            }

            var childCount = content.childCount;
            for (var i = 0; i < childCount; i++)
            {
                var eventBannerItem = content.GetChild(i).GetComponent<EventBannerItem>();
                if (eventBannerItem is null)
                {
                    continue;
                }

                if (eventBannerItem.DestroyIfNecessary())
                {
                    childCount--;
                }
            }

            for (var i = 0; i < childCount; i++)
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
