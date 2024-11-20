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
using System.Collections;
using System.Text.Json.Serialization;
using System.Threading;
using Nekoyume.L10n;

namespace Nekoyume.Game.LiveAsset
{
    using UniRx;

    public class LiveAssetManager : MonoSingleton<LiveAssetManager>
    {
        private enum InitializingState
        {
            NeedInitialize,
            Initializing,
            Initialized
        }

        private const string AlreadyReadNoticeKey = "AlreadyReadNoticeList";

        private const string KoreanImagePostfix = "_KR";
        private const string JapaneseImagePostfix = "_JP";

        // TODO: this is temporary url and file.
        private const string StakingLevelImageUrl = "Etc/NcgStaking.png";
        private const string StakingRewardImageUrl = "Etc/StakingReward.png";
        private const string StakingArenaBonusUrl = "https://assets.nine-chronicles.com/live-assets/Json/arena-bonus-values";

        private readonly List<EventNoticeData> _bannerData = new();
        private readonly ReactiveCollection<string> _alreadyReadNotices = new();
        private InitializingState _state = InitializingState.NeedInitialize;
        private Notices _notices;
        private LiveAssetEndpointScriptableObject _endpoint;
        private ApiClient.ThorSchedules _cachedThorSchedules;

        public bool HasUnreadEvent => _bannerData.Any(d => !_alreadyReadNotices.Contains(d.Description));

        public bool HasUnreadNotice => _notices.NoticeData.Any(d => !_alreadyReadNotices.Contains(d.Header));

        public bool HasUnread => IsInitialized && (HasUnreadEvent || HasUnreadNotice);

        public IObservable<bool> ObservableHasUnreadEvent =>
            _alreadyReadNotices
                .ObserveAdd()
                .Select(_ => HasUnreadEvent);

        public IObservable<bool> ObservableHasUnreadNotice =>
            _alreadyReadNotices
                .ObserveAdd()
                .Select(_ => HasUnreadNotice);

        public IObservable<bool> ObservableHasUnread =>
            _alreadyReadNotices
                .ObserveAdd()
                .Select(_ => HasUnread);

        public IReadOnlyList<EventNoticeData> BannerData => _bannerData;
        public IReadOnlyList<NoticeData> NoticeData => _notices.NoticeData;
        public GameConfig GameConfig { get; private set; }
        public CommandLineOptions CommandLineOptions { get; private set; }
        public ApiClient.ThorSchedule ThorSchedule { get; private set; }
        public EventRewardPopupData EventRewardPopupData { get; private set; }
        public Sprite StakingLevelSprite { get; private set; }
        public Sprite StakingRewardSprite { get; private set; }
        public int[] StakingArenaBonusValues { get; private set; }
        public bool IsInitialized => _state == InitializingState.Initialized;

        public System.Action<Nekoyume.ApiClient.ThorSchedule> OnChangedThorSchedule;

        public void InitializeData()
        {
            _endpoint = Resources.Load<LiveAssetEndpointScriptableObject>("ScriptableObject/LiveAssetEndpoint");
            StartCoroutine(RequestManager.instance.GetJson(_endpoint.GameConfigJsonUrl, SetLiveAssetData));
            StartCoroutine(InitializeThorSchedule());
            InitializeStakingResource().Forget();
            InitializeEvent();

            StartCoroutine(RequestManager.instance.GetJson(
                _endpoint.EventRewardPopupDataJsonUrl,
                value => SetEventRewardPopupData(value).Forget()));
        }

        private IEnumerator InitializeThorSchedule()
        {
            yield return StartCoroutine(
                RequestManager.instance.GetJson(
                    _endpoint.ThorScheduleUrl,
                    SetThorScheduleUrl));
        }

        public void InitializeEvent()
        {
            if (_state != InitializingState.NeedInitialize)
            {
                return;
            }

            _state = InitializingState.Initializing;
            var noticeUrl = L10nManager.CurrentLanguage switch
            {
                LanguageType.Korean => Platform.IsMobilePlatform()
                    ? _endpoint.NoticeJsonKoreanUrl
                    : _endpoint.NoticeJsonUrl,
                LanguageType.Japanese => _endpoint.NoticeJsonJapaneseUrl,
                _ => _endpoint.NoticeJsonUrl
            };
            StartCoroutine(RequestManager.instance.GetJson(_endpoint.EventJsonUrl, SetEventData));
            StartCoroutine(RequestManager.instance.GetJson(noticeUrl, SetNotices));
        }

        public IEnumerator InitializeApplicationCLO()
        {
            var osKey = string.Empty;
#if UNITY_ANDROID
            osKey = "-aos";
#elif UNITY_IOS
            osKey = "-ios";
#endif

            var languageKey = string.Empty;
            if (GameConfig.IsKoreanBuild)
            {
                languageKey = "-kr";
            }

            var cloEndpoint = $"{_endpoint.CommandLineOptionsJsonUrlPrefix}{Application.version.Replace(".", "-")}{osKey}{languageKey}.json";
            NcDebug.Log($"[InitializeApplicationCLO] cloEndpoint: {cloEndpoint}");
            yield return StartCoroutine(
                RequestManager.instance.GetJson(
                    cloEndpoint,
                    SetCommandLineOptions));

            if (CommandLineOptions == null)
            {
                yield return StartCoroutine(
                    RequestManager.instance.GetJson(
                        GameConfig.IsKoreanBuild
                            ? _endpoint.CommandLineOptionsJsonDefaultUrlKr
                            : _endpoint.CommandLineOptionsJsonDefaultUrl,
                        SetCommandLineOptions));
            }
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

#region ThorSchedule
        private void SetThorScheduleUrl(string response)
        {
            _cachedThorSchedules = JsonSerializer.Deserialize<ApiClient.ThorSchedules>(
                response,
                CommandLineOptions.JsonOptions);

            var planetId = Nekoyume.Game.Game.instance.CurrentPlanetId;
            SetThorSchedule(planetId);
        }

        public void SetThorSchedule(Multiplanetary.PlanetId? planetId)
        {
            if (planetId == null)
            {
                // PlanetId 초기화 전에는 항상 인텨널 기준으로 설정
                ThorSchedule = _cachedThorSchedules.Others;
                // 모바일 메인넷에서 인터널 관련 정보가 보이지 않게 OnChangedThorSchedule를 호출하지 않는다.
                return;
            }

            var isMainNet = Multiplanetary.PlanetId.IsMainNet(planetId.Value);
            ThorSchedule = isMainNet ?
                _cachedThorSchedules.MainNet :
                _cachedThorSchedules.Others;

            OnChangedThorSchedule?.Invoke(ThorSchedule);
        }
#endregion ThorSchedule

        private void SetCommandLineOptions(string response)
        {
            var options = CommandLineParser.GetCommandLineOptions<CommandLineOptions>();
            if (options is { Empty: false })
            {
                NcDebug.Log($"Get options from commandline.");
                CommandLineOptions = options;
            }

            CommandLineOptions = JsonSerializer.Deserialize<CommandLineOptions>(
                response,
                CommandLineOptions.JsonOptions);
        }

        private async UniTask SetEventRewardPopupData(string response)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter(), },
            };
            EventRewardPopupData = JsonSerializer.Deserialize<EventRewardPopupData>(response, options);
            EventRewardPopupData.EventRewards = EventRewardPopupData.EventRewards
                .Where(reward => DateTime.UtcNow.IsInTime(reward.BeginDateTime, reward.EndDateTime))
                .ToArray();

            var tasks = EventRewardPopupData.EventRewards
                .Where(reward => reward.Content != null && reward.Content.ImageName != null)
                .Select(reward => GetSpriteTask(reward.Content))
                .ToList();
            var thorEnabledContent = EventRewardPopupData.EnabledThorChainContent;
            if (thorEnabledContent != null && thorEnabledContent.ImageName != null)
            {
                tasks.Add(GetSpriteTask(thorEnabledContent));
            }

            var thorDisabledContent = EventRewardPopupData.DisabledThorChainContent;
            if (thorDisabledContent != null && thorDisabledContent.ImageName != null)
            {
                tasks.Add(GetSpriteTask(thorDisabledContent));
            }

            await UniTask.WhenAll(tasks);

            return;
            UniTask<Sprite> GetSpriteTask(EventRewardPopupData.Content content)
            {
                var uri = $"{_endpoint.ImageRootUrl}/EventRewardPopup/{content.ImageName}.png";
                return GetTexture(uri).ContinueWith(sprite => content.Image = sprite);
            }
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
                    Description = banner.Description,
                    EnableKeys = banner.EnableKeys
                };
                _bannerData.Add(newData);

                var bannerTask = GetNoticeTexture("Banner", banner.BannerImageName)
                    .ContinueWith(sprite => newData.BannerImage = sprite);
                var popupTask = GetNoticeTexture("Notice", banner.PopupImageName)
                    .ContinueWith(sprite => newData.PopupImage = sprite);
                tasks.Add(bannerTask);
                tasks.Add(popupTask);
            }

            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await UniTask.WaitUntil(() =>
                        tasks.TrueForAll(task =>
                            task.Status == UniTaskStatus.Succeeded) && _notices is not null,
                    cancellationToken: cancellation.Token);
            }
            catch (OperationCanceledException e)
            {
                if (e.CancellationToken == cancellation.Token)
                {
                    _state = InitializingState.NeedInitialize;
                    NcDebug.Log($"[{nameof(LiveAssetManager)}] NoticeData making failed by timeout.");
                    return;
                }
            }

            _state = InitializingState.Initialized;

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

        private UniTask<Sprite> GetNoticeTexture(string textureType, string imageName)
        {
            var postfix = L10nManager.CurrentLanguage switch
            {
                LanguageType.Korean => Platform.IsMobilePlatform()
                    ? KoreanImagePostfix
                    : string.Empty,
                LanguageType.Japanese => JapaneseImagePostfix,
                _ => string.Empty
            };
            return GetTexture($"{_endpoint.ImageRootUrl}/{textureType}/{imageName}{postfix}.png");
        }

        private static async UniTask<Sprite> GetTexture(string uri)
        {
            Sprite result = null;
            // 최대 3번까지 이미지를 다시 받길 시도한다.
            var retryCount = 3;
            while (retryCount-- > 0)
            {
                result = await Helper.Util.DownloadTexture(uri);
                if (result != null)
                {
                    return result;
                }

                // 다운로드 실패시 1초 대기 후 재시도
                await UniTask.Delay(1000);
            }

            return result;
        }

        private async UniTaskVoid InitializeStakingResource()
        {
            StakingLevelSprite = await Helper.Util.DownloadTexture($"{_endpoint.ImageRootUrl}/{StakingLevelImageUrl}");
            StakingRewardSprite = await Helper.Util.DownloadTexture($"{_endpoint.ImageRootUrl}/{StakingRewardImageUrl}");
            RequestManager.instance
                .GetJson(StakingArenaBonusUrl, response => { StakingArenaBonusValues = response.Split(",").Select(int.Parse).ToArray(); })
                .ToUniTask()
                .Forget();
        }
    }
}
