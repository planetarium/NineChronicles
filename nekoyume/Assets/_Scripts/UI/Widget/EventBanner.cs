using UnityEngine;
using UnityEngine.UI;
using Nekoyume.UI.Model;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.ResourceManagement.AsyncOperations;

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

        private const string Url =
            "https://raw.githubusercontent.com/planetarium/NineChronicles/feature/banner-test/nekoyume/Assets/Dynamic/Json/Banner.json";

        private void Start()
        {
            StartCoroutine(RequestManager.instance.GetJson(Url, Set));
        }

        private void Set(string json)
        {
            var eb = JsonSerializer.Deserialize<EventBanners>(json);
            var dataList = eb.Banners.OrderBy(x => x.Priority);
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
