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
using Nekoyume.UI.Module;
using UnityEngine;
using System.Collections;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading;
using JetBrains.Annotations;
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
        private const string AlreadyReadNcuKey = "AlreadyReadNcuList";
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

        // TODO: this is temporary url and file.
        private const string StakingLevelImageUrl = "Etc/NcgStaking.png";
        private const string StakingRewardImageUrl = "Etc/StakingReward.png";
        private const string StakingArenaBonusUrl = "https://assets.nine-chronicles.com/live-assets/Json/arena-bonus-values";

        private readonly List<EventNoticeData> _bannerData = new();
        private readonly List<EventNoticeData> _ncuData = new();
        private readonly ReactiveCollection<string> _alreadyReadNotices = new();
        private readonly ReactiveProperty<bool> _observableHasUnreadNcu = new ();
        private InitializingState _state = InitializingState.NeedInitialize;
        private Notices _notices;
        private LiveAssetEndpointScriptableObject _endpoint;
        private ApiClient.ThorSchedules _cachedThorSchedules;

        public bool HasUnreadEvent => _bannerData.Any(d => !_alreadyReadNotices.Contains(d.Description));

        public bool HasUnreadNotice => _notices.NoticeData.Any(d => !_alreadyReadNotices.Contains(d.Header));

        public bool HasUnreadNcu
        {
            get
            {
                var notReadAtToday = true;
                if (PlayerPrefs.HasKey(AlreadyReadNcuKey) &&
                    DateTime.TryParseExact(PlayerPrefs.GetString(AlreadyReadNcuKey),
                        DateTimeFormat, null, DateTimeStyles.None, out var result))
                {
                    notReadAtToday = DateTime.Today != result.Date;
                }

                return notReadAtToday;
            }
        }

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

        public ReactiveProperty<bool> ObservableHasUnreadNcu => _observableHasUnreadNcu;
        public IReadOnlyList<EventNoticeData> BannerData => _bannerData;
        public IReadOnlyList<NoticeData> NoticeData => _notices.NoticeData;
        public IReadOnlyList<EventNoticeData> NcuData => _ncuData;
        public GameConfig GameConfig { get; private set; }
        public CommandLineOptions CommandLineOptions { get; private set; }
        [CanBeNull]
        public ApiClient.ThorSchedule ThorSchedule { get; private set; }
        public EventRewardPopupData EventRewardPopupData { get; private set; }
        public Sprite StakingLevelSprite { get; private set; }
        public Sprite StakingRewardSprite { get; private set; }
        public int[] StakingArenaBonusValues { get; private set; }
        public bool IsInitialized => _state == InitializingState.Initialized;

        public Action<ApiClient.ThorSchedule> OnChangedThorSchedule;

        public void InitializeData()
        {
            _endpoint = Resources.Load<LiveAssetEndpointScriptableObject>("ScriptableObject/LiveAssetEndpoint");
            StartCoroutine(RequestManager.instance.GetJson(_endpoint.GameConfigJsonUrl, SetLiveAssetData));
            StartCoroutine(InitializeThorSchedule());
            InitializeStakingResource().Forget();

            StartCoroutine(RequestManager.instance.GetJson(
                _endpoint.EventRewardPopupDataJsonUrl,
                value => SetEventRewardPopupData(value).Forget()));
            _observableHasUnreadNcu.SetValueAndForceNotify(HasUnreadNcu);
        }

        private IEnumerator InitializeThorSchedule()
        {
            yield return StartCoroutine(
                RequestManager.instance.GetJson(
                    _endpoint.ThorScheduleUrl,
                    SetThorScheduleUrl));
        }

        public async UniTask InitializeEventAsync()
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

            await UniTask.WhenAll(
                RequestManager.instance.GetJson(_endpoint.EventJsonUrl, SetEventData).ToUniTask(),
                RequestManager.instance.GetJson(noticeUrl, SetNotices).ToUniTask(),
                RequestManager.instance.GetJson(_endpoint.NcuJsonUrl, SetNcuData).ToUniTask());
            _state = InitializingState.Initialized;
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

        public void ReadNcu()
        {
            PlayerPrefs.SetString(AlreadyReadNcuKey, DateTime.Today.ToString(DateTimeFormat));
            _observableHasUnreadNcu.SetValueAndForceNotify(false);
        }

        private void SetNotices(string response)
        {
            _notices = JsonSerializer.Deserialize<Notices>(response);
        }

        private void SetEventData(string response)
        {
            var responseData = JsonSerializer.Deserialize<EventBanners>(response);
            if (responseData?.Banners == null)
            {
                NcDebug.LogWarning($"[{nameof(LiveAssetManager)}] EventBanners data is null or empty");
                return;
            }

            MakeNoticeData(responseData.Banners.OrderBy(x => x.Priority)).Forget();
        }

        private void SetNcuData(string response)
        {
            var responseData = JsonSerializer.Deserialize<EventBanners>(response);
            if (responseData?.Banners == null)
            {
                NcDebug.LogWarning($"[{nameof(LiveAssetManager)}] EventBanners data is null or empty");
                return;
            }

            MakeNcuData(responseData.Banners.OrderBy(x => x.Priority)).Forget();
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
        }

        public void SetThorSchedule(Multiplanetary.PlanetId? planetId)
        {
            if (planetId == null)
            {
#if UNITY_EDITOR
                // 에디터에서는 주로 테스트 용도로 사용하므로 PlanetID가 없으면 Others로 설정한다.
                ThorSchedule = _cachedThorSchedules.Others;
#else
                // 빌드에서는 메인넷으로 설정한다.
                ThorSchedule = _cachedThorSchedules.MainNet;
#endif
                // 모바일 메인넷에서 인터널 관련 정보가 보이지 않게 OnChangedThorSchedule를 호출하지 않는다.
                return;
            }

            var isMainNet = Multiplanetary.PlanetId.IsMainNet(planetId.Value);
            var previousThorSchedule = ThorSchedule;
            ThorSchedule = isMainNet ?
                _cachedThorSchedules.MainNet :
                _cachedThorSchedules.Others;
            if (previousThorSchedule != ThorSchedule)
            {
                NcDebug.Log($"[{nameof(LiveAssetManager)}] SetThorSchedule: {planetId}, isMainNet: {isMainNet}");
            }

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
            var planetIdExist = Game.instance.CurrentPlanetId.HasValue;
            var isMainNet = planetIdExist && Multiplanetary.PlanetId.IsMainNet(Game.instance.CurrentPlanetId.Value);
            var tasks = new List<UniTask>();

            foreach (var banner in bannerData)
            {
                // 1. 날짜 필터링 (가장 먼저 체크)
                if (banner.UseDateTime && !DateTime.UtcNow.IsInTime(banner.BeginDateTime, banner.EndDateTime))
                {
                    continue;
                }

                // 2. 메인넷 필터링 (EventNoticeData 생성 전에 체크)
                if (isMainNet && !banner.IsMainnet)
                {
                    continue;
                }

                var buttonType = EventButtonType.URL;
                if (!string.IsNullOrEmpty(banner.ButtonType))
                {
                    if (Enum.TryParse<EventButtonType>(banner.ButtonType, true, out var parsedButtonType))
                    {
                        buttonType = parsedButtonType;
                    }
                    else
                    {
                        NcDebug.LogWarning($"[{nameof(LiveAssetManager)}] Invalid ButtonType: {banner.ButtonType}");
                    }
                }

                InGameNavigationData inGameNavigationData = null;
                if (buttonType == EventButtonType.IN_GAME)
                {
                    inGameNavigationData = new InGameNavigationData();

                    if (Enum.TryParse<ShortcutHelper.PlaceType>(banner.NavigationData.PlaceType, true, out var placeType))
                    {
                        inGameNavigationData.PlaceType = placeType;
                    }
                    else
                    {
                        NcDebug.LogWarning($"[{nameof(LiveAssetManager)}] Invalid PlaceType: {banner.NavigationData.PlaceType}, using default Summon");
                        inGameNavigationData.PlaceType = ShortcutHelper.PlaceType.Summon;
                    }

                    if (banner.NavigationData.WorldId is not null)
                    {
                        inGameNavigationData.WorldId = banner.NavigationData.WorldId.Value;
                    }
                    else
                    {
                        NcDebug.LogWarning($"[{nameof(LiveAssetManager)}] Invalid WorldId: {banner.NavigationData.WorldId}, using default 1");
                        inGameNavigationData.WorldId = 1;
                    }

                    if (banner.NavigationData.StageId is not null)
                    {
                        inGameNavigationData.StageId = banner.NavigationData.StageId.Value;
                    }
                    else
                    {
                        NcDebug.LogWarning($"[{nameof(LiveAssetManager)}] Invalid StageId: {banner.NavigationData.StageId}, using default 1");
                        inGameNavigationData.StageId = 1;
                    }

                    if (Enum.TryParse<Summon.SummonType>(banner.NavigationData.SummonType, true, out var summonType))
                    {
                        inGameNavigationData.SummonType = summonType;
                    }
                    else
                    {
                        NcDebug.LogWarning($"[{nameof(LiveAssetManager)}] Invalid SummonType: {banner.NavigationData.SummonType}, using default GRIMORE");
                        inGameNavigationData.SummonType = Summon.SummonType.GRIMORE;
                    }
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
                    EnableKeys = banner.EnableKeys,
                    WithSign = banner.WithSign,
                    IsMainnet = banner.IsMainnet,
                    ButtonType = buttonType,
                    InGameNavigationData = inGameNavigationData,
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

        private async UniTaskVoid MakeNcuData(IEnumerable<EventBannerData> bannerData)
        {
            var planetIdExist = Game.instance.CurrentPlanetId.HasValue;
            var isMainNet = planetIdExist && Multiplanetary.PlanetId.IsMainNet(Game.instance.CurrentPlanetId.Value);
            var tasks = new List<UniTask>();

            foreach (var banner in bannerData)
            {
                // 1. 날짜 필터링 (가장 먼저 체크)
                if (banner.UseDateTime && !DateTime.UtcNow.IsInTime(banner.BeginDateTime, banner.EndDateTime))
                {
                    continue;
                }

                // 2. 메인넷 필터링 (EventNoticeData 생성 전에 체크)
                if (isMainNet && !banner.IsMainnet)
                {
                    continue;
                }

                var buttonType = EventButtonType.URL;

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
                    EnableKeys = banner.EnableKeys,
                    WithSign = banner.WithSign,
                    IsMainnet = banner.IsMainnet,
                    ButtonType = buttonType,
                    InGameNavigationData = null,
                };

                _ncuData.Add(newData);

                var bannerTask = GetNoticeTexture("Ncu", banner.BannerImageName)
                    .ContinueWith(sprite => newData.BannerImage = sprite);
                var popupTask = GetNoticeTexture("Ncu", banner.PopupImageName)
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
        }

        private UniTask<Sprite> GetNoticeTexture(string textureType, string imageName)
        {
            return GetTexture($"{_endpoint.ImageRootUrl}/{textureType}/{imageName}.png");
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
