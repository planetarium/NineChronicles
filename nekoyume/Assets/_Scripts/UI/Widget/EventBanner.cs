using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.LiveAsset;
using Nekoyume.UI.Module.Common;

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

        private async UniTask InitializeAsync()
        {
            await LiveAssetManager.instance.InitializeEventAsync();
            await UniTask.WaitUntil(() => LiveAssetManager.instance.IsInitialized);

            // 배너 이미지 로딩이 완료될 때까지 대기
            var dataList = LiveAssetManager.instance.BannerData;
            if (dataList.Any(data => data.BannerImage == null))
            {
                NcDebug.LogWarning($"[{nameof(EventBanner)}] Some banner images are still loading, waiting...");
                await WaitForBannerImagesAsync();
            }

            foreach (var data in dataList)
            {
                if (data.UseDateTime &&
                    !DateTime.UtcNow.IsInTime(data.BeginDateTime, data.EndDateTime))
                {
                    continue;
                }

                var ba = Instantiate(Banner, content);
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
            GetComponent<NotchAdjuster>()?.RefreshNotchByScreenState();
        }

        private async UniTask WaitForBannerImagesAsync()
        {
            var liveAssetManager = LiveAssetManager.instance;
            var timeout = TimeSpan.FromSeconds(10);
            var cancellationTokenSource = new CancellationTokenSource(timeout);

            try
            {
                await UniTask.WaitUntil(() =>
                    liveAssetManager.BannerData.All(data => data.BannerImage != null),
                    cancellationToken: cancellationTokenSource.Token);

                NcDebug.Log($"[{nameof(EventBanner)}] All banner images loaded successfully");
            }
            catch (OperationCanceledException)
            {
                NcDebug.LogError($"[{nameof(EventBanner)}] Banner image loading timeout after {timeout.TotalSeconds} seconds");
                // 타임아웃이 발생해도 계속 진행 (EventBannerItem에서 null 체크가 있으므로 안전)
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            if (!LiveAssetManager.instance.IsInitialized)
            {
                InitializeAsync().Forget();
            }

#if UNITY_ANDROID || UNITY_IOS
            transform.SetSiblingIndex(Find<LobbyMenu>().transform.GetSiblingIndex()+1);
#endif
        }
    }
}
