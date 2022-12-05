using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Cysharp.Threading.Tasks;
using Nekoyume.Pattern;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.Game.Notice
{
    public class NoticeManager : MonoSingleton<NoticeManager>
    {
        private const string EventJsonUrl =
            "https://raw.githubusercontent.com/planetarium/NineChronicles.LiveAssets/feature/renew-notice/Assets/Json/Event.json";

        private const string NoticeJsonUrl =
            "https://raw.githubusercontent.com/planetarium/NineChronicles.LiveAssets/feature/renew-notice/Assets/Json/Notice.json";

        private const string ImageUrl =
            "https://raw.githubusercontent.com/planetarium/NineChronicles.LiveAssets/feature/renew-notice/Assets/Images";
        private static readonly Vector2 Pivot = new(0.5f, 0.5f);

        private readonly List<EventNoticeData> _bannerData = new();
        private Notices _notices;

        public IReadOnlyList<EventNoticeData> BannerData => _bannerData;
        public IReadOnlyList<NoticeData> NoticeData => _notices.NoticeData;
        public bool IsInitialized { get; private set; }

        public void InitializeData()
        {
            StartCoroutine(RequestManager.instance.GetJson(EventJsonUrl, SetEventData));
            StartCoroutine(RequestManager.instance.GetJson(NoticeJsonUrl, SetNotices));
        }

        private void SetNotices(string response)
        {
            _notices = JsonSerializer.Deserialize<Notices>(response);
        }

        private void SetEventData(string response)
        {
            var responseData = JsonSerializer.Deserialize<EventBanners>(response);
            MakeNoticeData(responseData?.Banners.OrderBy(x => x.Priority)).Forget();
        }

        private async UniTaskVoid MakeNoticeData(IEnumerable<EventBannerData> bannerData)
        {
            var tasks = new List<UniTask>();
            foreach (var banner in bannerData)
            {
                if (banner.UseDateTime && !Helper.Util.IsInTime(banner.BeginDateTime, banner.EndDateTime))
                {
                    continue;
                }

                var newData = new EventNoticeData
                {
                    Priority = banner.Priority,
                    BannerImage = null,
                    PopupImage = null,
                    UseDateTime = banner.UseDateTime,
                    BeginDateTime = banner.BeginDateTime,
                    EndDateTime = banner.EndDateTime,
                    Url = banner.Url,
                    UseAgentAddress = banner.UseAgentAddress,
                    Description = banner.Description
                };
                _bannerData.Add(newData);

                var bannerTask = GetTexture("Banner", banner.BannerImageName)
                    .ContinueWith(sprite => newData.BannerImage = sprite);
                var popupTask = GetTexture("Notice", banner.PopupImageName)
                    .ContinueWith(sprite => newData.PopupImage = sprite);
                tasks.Add(bannerTask);
                tasks.Add(popupTask);
            }

            await UniTask.WaitUntil(() =>
                tasks.TrueForAll(task => task.Status == UniTaskStatus.Succeeded) &&
                _notices is not null);
            IsInitialized = true;
        }

        private async UniTask<Sprite> GetTexture(string textureType, string imageName)
        {
            var www = UnityWebRequestTexture.GetTexture($"{ImageUrl}/{textureType}/{imageName}.png");
            await www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                return null;
            }

            var myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            return Sprite.Create(
                myTexture,
                new Rect(0, 0, myTexture.width, myTexture.height),
                Pivot);
        }
    }
}
