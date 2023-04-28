using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.LiveAsset;

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

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => LiveAssetManager.instance.IsInitialized);

            var dataList = LiveAssetManager.instance.BannerData;
            foreach (var data in dataList)
            {
                if (data.UseDateTime &&
                    !DateTime.UtcNow.IsInTime(data.BeginDateTime, data.EndDateTime))
                {
                    continue;
                }

                var ba= Instantiate(Banner, content);
                ba.GetComponent<EventBannerItem>().Set(data);
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
