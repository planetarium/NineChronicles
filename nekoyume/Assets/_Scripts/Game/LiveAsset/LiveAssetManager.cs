using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.Helper;
using Nekoyume.Pattern;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace Nekoyume.Game.LiveAsset
{
    using UniRx;

    public class LiveAssetManager : MonoSingleton<LiveAssetManager>
    {
        private const string AlreadyReadNoticeKey = "AlreadyReadNoticeList";
        private static readonly Vector2 Pivot = new(0.5f, 0.5f);
        private const string CLOEndpointPrefix = "https://raw.githubusercontent.com/planetarium/NineChronicles.LiveAssets/main/Assets/Json/CloForAppVersion/clo-app-ver-";

        private readonly List<EventNoticeData> _bannerData = new();
        private readonly ReactiveCollection<string> _alreadyReadNotices = new();
        private Notices _notices;
        private LiveAssetEndpointScriptableObject _endpoint;

        public bool HasUnreadEvent =>
            _bannerData.Any(d => !_alreadyReadNotices.Contains(d.Description));

        public bool HasUnreadNotice =>
            _notices.NoticeData.Any(d => !_alreadyReadNotices.Contains(d.Header));

        public bool HasUnread => HasUnreadEvent || HasUnreadNotice;

        public IObservable<bool> ObservableHasUnreadEvent => _alreadyReadNotices
            .ObserveAdd()
            .Select(_ => HasUnreadEvent);

        public IObservable<bool> ObservableHasUnreadNotice => _alreadyReadNotices
            .ObserveAdd()
            .Select(_ => HasUnreadNotice);

        public IObservable<bool> ObservableHasUnread => _alreadyReadNotices
            .ObserveAdd()
            .Select(_ => HasUnread);

        public IReadOnlyList<EventNoticeData> BannerData => _bannerData;
        public IReadOnlyList<NoticeData> NoticeData => _notices.NoticeData;
        public GameConfig GameConfig { get; private set; }
        public CommandLineOptions CommandLineOptions { get; private set; }
        public bool IsInitialized { get; private set; }

        public void InitializeData()
        {
            _endpoint = Resources.Load<LiveAssetEndpointScriptableObject>("ScriptableObject/LiveAssetEndpoint");
            StartCoroutine(RequestManager.instance.GetJson(_endpoint.EventJsonUrl, SetEventData));
            StartCoroutine(RequestManager.instance.GetJson(_endpoint.NoticeJsonUrl, SetNotices));
            StartCoroutine(RequestManager.instance.GetJson(_endpoint.GameConfigJsonUrl, SetLiveAssetData));
        }

        public IEnumerator InitializeApplicationCLO()
        {
            var osKey = string.Empty;
#if UNITY_ANDROID
            osKey = "-aos";
#elif UNITY_IOS
            osKey = "-ios";
#endif
            var cloEndpoint = $"{CLOEndpointPrefix}{Application.version.Replace(".", "-")}{osKey}.json";
            Debug.Log($"[InitializeApplicationCLO] cloEndpoint: {cloEndpoint}");
            yield return StartCoroutine(RequestManager.instance.GetJson(cloEndpoint, SetCommandLineOptions));

            if(CommandLineOptions == null)
                yield return StartCoroutine(RequestManager.instance.GetJson(_endpoint.CommandLineOptionsJsonUrl, SetCommandLineOptions));
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

        private void SetLiveAssetData(string response)
        {
            GameConfig = JsonSerializer.Deserialize<GameConfig>(response);
        }

        private void SetCommandLineOptions(string response)
        {
            var options = CommandLineParser.GetCommandLineOptions<CommandLineOptions>();
            if (options is { Empty: false })
            {
                Debug.Log($"Get options from commandline.");
                CommandLineOptions = options;
            }

            var jsonOptions = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                Converters =
                {
                    new CommandLineOptions.StringEnumerableConverter(),
                },
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip,
            };

            CommandLineOptions = JsonSerializer.Deserialize<CommandLineOptions>(response, jsonOptions);
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
