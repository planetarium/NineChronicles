using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.Pattern;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.Game.Notice
{
    using UniRx;

    public class NoticeManager : MonoSingleton<NoticeManager>
    {
        private const string AlreadyReadNoticeKey = "AlreadyReadNoticeList";
        private static readonly Vector2 Pivot = new(0.5f, 0.5f);

        private readonly List<EventNoticeData> _bannerData = new();
        private readonly ReactiveCollection<string> _alreadyReadNotices = new();
        private Notices _notices;
        private NoticeEndpointScriptableObject _endpoint;

        public bool HasUnreadEvent =>
            _bannerData.Any(d => !_alreadyReadNotices.Contains(d.Description));

        public bool HasUnreadNotice =>
            _notices.NoticeData.Any(d => !_alreadyReadNotices.Contains(d.Header));

        public IObservable<bool> ObservableHasUnreadEvent => _alreadyReadNotices
            .ObserveAdd()
            .Select(_ => HasUnreadEvent);

        public IObservable<bool> ObservableHasUnreadNotice => _alreadyReadNotices
            .ObserveAdd()
            .Select(_ => HasUnreadNotice);

        public IReadOnlyList<EventNoticeData> BannerData => _bannerData;
        public IReadOnlyList<NoticeData> NoticeData => _notices.NoticeData;
        public bool IsInitialized { get; private set; }

        public void InitializeData()
        {
            _endpoint = Resources.Load<NoticeEndpointScriptableObject>("ScriptableObject/NoticeEndpoint");
            StartCoroutine(RequestManager.instance.GetJson(_endpoint.EventJsonUrl, SetEventData));
            StartCoroutine(RequestManager.instance.GetJson(_endpoint.NoticeJsonUrl, SetNotices));
        }

        public void AddToCheckedList(string key)
        {
            if (_alreadyReadNotices.Contains(key))
            {
                return;
            }

            _alreadyReadNotices.Add(key);
            PlayerPrefs.SetString(AlreadyReadNoticeKey,
                _alreadyReadNotices.Aggregate((a, b) => $"{a}#{b}"));
        }

        public bool IsAlreadyReadNotice(string key)
        {
            return _alreadyReadNotices.Contains(key);
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
                if (banner.UseDateTime && !DateTime.UtcNow.IsInTime(banner.BeginDateTime, banner.EndDateTime))
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

            if (PlayerPrefs.HasKey(AlreadyReadNoticeKey))
            {
                var listString = PlayerPrefs.GetString(AlreadyReadNoticeKey);
                var currentDataSet = _bannerData.Select(d => d.Description)
                    .Concat(_notices.NoticeData.Select(d => d.Header)).ToHashSet();
                foreach (var noticeKey in listString.Split("#"))
                {
                    if (currentDataSet.Contains(noticeKey))
                    {
                        AddToCheckedList(noticeKey);
                    }
                }
            }
        }

        private async UniTask<Sprite> GetTexture(string textureType, string imageName)
        {
            var www = UnityWebRequestTexture.GetTexture(
                $"{_endpoint.ImageRootUrl}/{textureType}/{imageName}.png");
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
