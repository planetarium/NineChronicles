#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#define RUN_ON_MOBILE
#define ENABLE_FIREBASE
#endif
#if !UNITY_EDITOR && UNITY_STANDALONE
#define RUN_ON_STANDALONE
#endif

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Lib9c.Formatters;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using LruCacheNet;
using MessagePack;
using MessagePack.Resolvers;
using Nekoyume.Action;
using Nekoyume.Blockchain;
using Nekoyume.Multiplanetary;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Factory;
using Nekoyume.Game.LiveAsset;
using Nekoyume.Game.OAuth;
using Nekoyume.Game.VFX;
using Nekoyume.Helper;
using Nekoyume.IAPStore;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.Pattern;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module.WorldBoss;
using Nekoyume.UI.Scroller;
using NineChronicles.ExternalServices.IAPService.Runtime;
using UnityEngine;
using UnityEngine.Playables;
using Currency = Libplanet.Types.Assets.Currency;
using Menu = Nekoyume.UI.Menu;
using Random = UnityEngine.Random;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using Nekoyume.Model.Mail;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using Debug = UnityEngine.Debug;
#if ENABLE_FIREBASE
using NineChronicles.GoogleServices.Firebase.Runtime;
#endif

namespace Nekoyume.Game
{
    using GraphQL;
    using Nekoyume.Arena;
    using Nekoyume.Model.EnumType;
    using Nekoyume.TableData;
    using UniRx;

    [RequireComponent(typeof(Agent), typeof(RPCAgent))]
    public class Game : MonoSingleton<Game>
    {
        [SerializeField]
        private Stage stage;

        [SerializeField]
        private Arena arena;

        [SerializeField]
        private RaidStage raidStage;

        [SerializeField]
        private Lobby lobby;

        [SerializeField]
        private bool useSystemLanguage = true;

        [SerializeField]
        private bool useLocalHeadless;

        [SerializeField]
        private bool useLocalMarketService;

        [SerializeField]
        private string marketDbConnectionString =
            "Host=localhost;Username=postgres;Database=market";

        [SerializeField]
        private LanguageTypeReactiveProperty languageType;

        [SerializeField]
        private Prologue prologue;

        [SerializeField]
        private GameObject debugConsolePrefab;

        public PlanetId? CurrentPlanetId { get; private set; }

        public States States { get; private set; }

        public LocalLayer LocalLayer { get; private set; }

        public LocalLayerActions LocalLayerActions { get; private set; }

        public IAgent Agent { get; private set; }

        public Analyzer Analyzer { get; private set; }

        public IAPStoreManager IAPStoreManager { get; private set; }

        public IAPServiceManager IAPServiceManager { get; private set; }

        public SeasonPassServiceManager SeasonPassServiceManager { get; private set; }

        public Stage Stage => stage;
        public Arena Arena => arena;
        public RaidStage RaidStage => raidStage;
        public Lobby Lobby => lobby;

        // FIXME Action.PatchTableSheet.Execute()에 의해서만 갱신됩니다.
        // 액션 실행 여부와 상관 없이 최신 상태를 반영하게끔 수정해야합니다.
        public TableSheets TableSheets { get; private set; }

        public ActionManager ActionManager { get; private set; }

        public bool IsInitialized { get; private set; }
        public bool IsInWorld { get; set; }

        public int? SavedPetId { get; set; }

        public Prologue Prologue => prologue;

        public const string AddressableAssetsContainerPath = nameof(AddressableAssetsContainer);

        public NineChroniclesAPIClient ApiClient { get; private set; }

        public NineChroniclesAPIClient RpcGraphQLClient { get; private set; }

        public MarketServiceClient MarketServiceClient { get; private set; }

        public NineChroniclesAPIClient PatrolRewardServiceClient { get; private set; }

        public PortalConnect PortalConnect { get; private set; }

        public Url URL { get; private set; }

        public readonly LruCache<Address, IValue> CachedStates = new();

        public readonly Dictionary<Address, bool> CachedStateAddresses = new();

        public readonly Dictionary<Currency, LruCache<Address, FungibleAssetValue>>
            CachedBalance = new();

        public string CurrentSocialEmail { get; private set; }

        private CommandLineOptions _commandLineOptions;

        private AmazonCloudWatchLogsClient _logsClient;

        private PlayableDirector _activeDirector;

        private string _msg;

        public static readonly string CommandLineOptionsJsonPath =
            Platform.GetStreamingAssetsPath("clo.json");

        private static readonly string UrlJsonPath =
            Platform.GetStreamingAssetsPath("url.json");

        private Thread _headlessThread;
        private Thread _marketThread;

        private const string ArenaSeasonPushIdentifierKey = "ARENA_SEASON_PUSH_IDENTIFIER";
        private const string ArenaTicketPushIdentifierKey = "ARENA_TICKET_PUSH_IDENTIFIER";
        private const string WorldbossSeasonPushIdentifierKey = "WORLDBOSS_SEASON_PUSH_IDENTIFIER";
        private const string WorldbossTicketPushIdentifierKey = "WORLDBOSS_TICKET_PUSH_IDENTIFIER";
        private const int TicketPushBlockCountThreshold = 300;

        #region Mono & Initialization

#if !UNITY_EDITOR && UNITY_IOS
        void OnAuthorizationStatusReceived(AppTrackingTransparency.AuthorizationStatus status)
        {
            AppTrackingTransparency.OnAuthorizationStatusReceived -= OnAuthorizationStatusReceived;
        }
#endif

        protected override void Awake()
        {
            CurrentSocialEmail = string.Empty;

            Debug.Log("[Game] Awake() invoked");
            GL.Clear(true, true, Color.black);
            Application.runInBackground = true;

#if !UNITY_EDITOR && UNITY_IOS
            AppTrackingTransparency.OnAuthorizationStatusReceived += OnAuthorizationStatusReceived;
            AppTrackingTransparency.AuthorizationStatus status = AppTrackingTransparency.TrackingAuthorizationStatus();
            if (status == AppTrackingTransparency.AuthorizationStatus.NotDetermined)
            {
                AppTrackingTransparency.RequestTrackingAuthorization();
            }
#endif
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.ScriptOnly);
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            base.Awake();

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            // Load CommandLineOptions at Start() after init
#else
            _commandLineOptions = CommandLineOptions.Load(CommandLineOptionsJsonPath);
            OnLoadCommandlineOptions();
#endif
            URL = Url.Load(UrlJsonPath);

#if UNITY_EDITOR && !(UNITY_ANDROID || UNITY_IOS)
            // Local Headless
            if (useLocalHeadless && HeadlessHelper.CheckHeadlessSettings())
            {
                _headlessThread = new Thread(() => HeadlessHelper.RunLocalHeadless());
                _headlessThread.Start();
            }

            if (useLocalHeadless || _commandLineOptions.RpcClient)
            {
                Agent = GetComponent<RPCAgent>();
                SubscribeRPCAgent();
            }
            else
            {
                Agent = GetComponent<Agent>();
            }
#else
            Agent = GetComponent<RPCAgent>();
            SubscribeRPCAgent();
#endif

            States = new States();
            LocalLayer = new LocalLayer();
            LocalLayerActions = new LocalLayerActions();
            MainCanvas.instance.InitializeIntro();
        }

        private IEnumerator Start()
        {
#if LIB9C_DEV_EXTENSIONS && UNITY_ANDROID
            Lib9c.DevExtensions.TestbedHelper.LoadTestbedCreateAvatarForQA();
#endif
            Debug.Log("[Game] Start() invoked");
            var totalSw = new Stopwatch();
            totalSw.Start();

            // Initialize LiveAssetManager, Create RequestManager
            gameObject.AddComponent<RequestManager>();
            var liveAssetManager = gameObject.AddComponent<LiveAssetManager>();
            liveAssetManager.InitializeData();
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            Application.targetFrameRate = 30;
            yield return liveAssetManager.InitializeApplicationCLO();

            _commandLineOptions = liveAssetManager.CommandLineOptions;
            OnLoadCommandlineOptions();
#endif

#if UNITY_EDITOR
            if (useSystemLanguage)
            {
                yield return L10nManager.Initialize().ToYieldInstruction();
            }
            else
            {
                yield return L10nManager.Initialize(languageType.Value).ToYieldInstruction();
                languageType.Subscribe(value => L10nManager.SetLanguage(value)).AddTo(gameObject);
            }
#else
            yield return L10nManager
                .Initialize(string.IsNullOrWhiteSpace(_commandLineOptions.Language)
                    ? L10nManager.CurrentLanguage
                    : LanguageTypeMapper.ISO639(_commandLineOptions.Language))
                .ToYieldInstruction();
#endif
            Debug.Log("[Game] Start()... L10nManager initialized");

#if RUN_ON_MOBILE
            // NOTE: Initialize planet registry.
            //       It should do after load CommandLineOptions.
            //       And it should do before initialize Agent.
            var planetContext = new PlanetContext(_commandLineOptions);
            yield return PlanetSelector.InitializePlanetRegistryAsync(planetContext).ToCoroutine();
            if (planetContext.HasError)
            {
                QuitWithMessage(
                    L10nManager.Localize("ERROR_INITIALIZE_FAILED"),
                    planetContext.Error);
                yield break;
            }
#else
            // NOTE: We expect that the _commandLineOptions is contains
            //       the endpoints(hosts, urls) of a specific planet
            //       when run on standalone.
            PlanetContext planetContext = null;
            Debug.Log("PlanetContext is null on non-mobile platform.");
#endif

            OnLoadCommandlineOptions();
            // ~Initialize planet registry

            // NOTE: Portal url does not change for each planet.
            PortalConnect = new PortalConnect(_commandLineOptions.MeadPledgePortalUrl);

#if ENABLE_FIREBASE
            // NOTE: Initialize Firebase.
            yield return FirebaseManager.InitializeAsync().ToCoroutine();
#endif
            // NOTE: Initialize Analyzer after load CommandLineOptions, initialize States,
            //       initialize Firebase Manager.
            //       The planetId is null because it is not initialized yet. It will be
            //       updated after initialize Agent.
            InitializeAnalyzer(
                agentAddr: _commandLineOptions.PrivateKey is null
                    ? null
                    : PrivateKey.FromString(_commandLineOptions.PrivateKey).Address,
                planetId: null,
                rpcServerHost: _commandLineOptions.RpcServerHost);
            Analyzer.Track("Unity/Started");

#if ENABLE_IL2CPP
            // Because of strict AOT environments, use StaticCompositeResolver for IL2CPP.
            StaticCompositeResolver.Instance.Register(
                    MagicOnion.Resolvers.MagicOnionResolver.Instance,
                    Lib9c.Formatters.NineChroniclesResolver.Instance,
                    MessagePack.Resolvers.GeneratedResolver.Instance,
                    MessagePack.Resolvers.StandardResolver.Instance
                );
            var resolver = StaticCompositeResolver.Instance;
#else
            var resolver = CompositeResolver.Create(
                NineChroniclesResolver.Instance,
                StandardResolver.Instance
            );
#endif
            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            MessagePackSerializer.DefaultOptions = options;

            if (_commandLineOptions.RequiredUpdate)
            {
                var popup = Widget.Find<IconAndButtonSystem>();
                popup.Show(
                        "UI_REQUIRED_UPDATE_TITLE",
                        "UI_REQUIRED_UPDATE_CONTENT",
                        "UI_OK",
                        true,
                        IconAndButtonSystem.SystemType.Information);
                popup.ConfirmCallback = popup.CancelCallback =  () =>
                {
#if UNITY_ANDROID
                    Application.OpenURL(_commandLineOptions.GoogleMarketUrl);
#elif UNITY_IOS
                    Application.OpenURL(_commandLineOptions.AppleMarketUrl);
#endif
                };
                yield break;
            }

            // NOTE: Apply l10n to IntroScreen after L10nManager initialized.
            Widget.Find<IntroScreen>().ApplyL10n();

            // Initialize MainCanvas first
            MainCanvas.instance.InitializeFirst();
#if RUN_ON_MOBILE
            // NOTE: Invoke LoginSystem.TryLoginWithLocalPpk() after MainCanvas initialized.
            //       Because the _commandLineOptions.PrivateKey is empty when run on mobile.
            if (string.IsNullOrEmpty(_commandLineOptions.PrivateKey))
            {
                var loginSystem = Widget.Find<LoginSystem>();
                if (loginSystem.TryLoginWithLocalPpk())
                {
                    Debug.Log("[Game] Start()... CommandLineOptions.PrivateKey is empty." +
                              " Set local private key instead.");
                    _commandLineOptions.PrivateKey = loginSystem.GetPrivateKey().ToHexWithZeroPaddings();
                }
            }
#endif
            var settingPopup = Widget.Find<SettingPopup>();
            settingPopup.UpdateSoundSettings();

            // Initialize TableSheets. This should be done before initialize the Agent.
            yield return StartCoroutine(CoInitializeTableSheets());
            Debug.Log("[Game] Start()... TableSheets initialized");
            ResourcesHelper.Initialize();
            Debug.Log("[Game] Start()... ResourcesHelper initialized");
            AudioController.instance.Initialize();
            Debug.Log("[Game] Start()... AudioController initialized");

            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);

            // NOTE: Initialize IAgent.
            var agentInitialized = false;
            var agentInitializeSucceed = false;
            yield return StartCoroutine(CoLogin(planetContext, succeed =>
                    {
                        Debug.Log($"[Game] Agent initialized. {succeed}");
                        agentInitialized = true;
                        agentInitializeSucceed = succeed;
                    }
                )
            );
            var grayLoadingScreen = Widget.Find<GrayLoadingScreen>();
            grayLoadingScreen.ShowProgress(GameInitProgress.ProgressStart);
            yield return new WaitUntil(() => agentInitialized);
#if RUN_ON_MOBILE
            if (planetContext?.HasError ?? false)
            {
                QuitWithMessage(
                    L10nManager.Localize("ERROR_INITIALIZE_FAILED"),
                    planetContext.Error);
                yield break;
            }

            if (planetContext.SelectedPlanetInfo is null)
            {
                QuitWithMessage("planetContext.CurrentPlanetInfo is null in mobile.");
                yield break;
            }
#endif

            yield return UpdateCurrentPlanetIdAsync(planetContext).ToCoroutine();
            Analyzer.SetPlanetId(CurrentPlanetId?.ToString());
            Debug.Log($"[Game] Start()... CurrentPlanetId updated. {CurrentPlanetId?.ToString()}");

            if (agentInitializeSucceed)
            {
                Analyzer.SetAgentAddress(Agent.Address.ToString());
                Analyzer.Instance.Track("Unity/Intro/Start/AgentInitialized");

                var evt = new AirbridgeEvent("Intro_Start_AgentInitialized");
                evt.AddCustomAttribute("agent-address", Agent.Address.ToString());
                AirbridgeUnity.TrackEvent(evt);

                settingPopup.UpdatePrivateKey(_commandLineOptions.PrivateKey);
            }
            else
            {
                Analyzer.Instance.Track("Unity/Intro/Start/AgentInitializeFailed");

                var evt = new AirbridgeEvent("Intro_Start_AgentInitializeFailed");
                evt.AddCustomAttribute("agent-address", Agent.Address.ToString());
                AirbridgeUnity.TrackEvent(evt);

                QuitWithAgentConnectionError(null);
                yield break;
            }

            // NOTE: Create ActionManager after Agent initialized.
            ActionManager = new ActionManager(Agent);

            var createSecondWidgetCoroutine = StartCoroutine(MainCanvas.instance.CreateSecondWidgets());
            var sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            // NOTE: planetContext.CommandLineOptions and _commandLineOptions are same.
            // NOTE: Initialize several services after Agent initialized.
            // NOTE: Initialize api client.
            if (string.IsNullOrEmpty(_commandLineOptions.ApiServerHost))
            {
                ApiClient = new NineChroniclesAPIClient(string.Empty);
                Debug.Log("[Game] Start()... ApiClient initialized with empty host url" +
                          " because of no ApiServerHost");
            }
            else
            {
                ApiClient = new NineChroniclesAPIClient(_commandLineOptions.ApiServerHost);
                Debug.Log("[Game] Start()... ApiClient initialized." +
                          $" host: {_commandLineOptions.ApiServerHost}");
            }

            // NOTE: Initialize graphql client which is targeting to RPC server.
            if (string.IsNullOrEmpty(_commandLineOptions.RpcServerHost))
            {
                RpcGraphQLClient = new NineChroniclesAPIClient(string.Empty);
                Debug.Log("[Game] Start()... RpcGraphQLClient initialized with empty host url" +
                          " because of no RpcServerHost");
            }
            else
            {
                RpcGraphQLClient = new NineChroniclesAPIClient(
                    $"http://{_commandLineOptions.RpcServerHost}/graphql");
            }

            // NOTE: Initialize world boss query.
            if (string.IsNullOrEmpty(_commandLineOptions.OnBoardingHost))
            {
                WorldBossQuery.SetUrl(string.Empty);
                Debug.Log($"[Game] Start()... WorldBossQuery initialized with empty host url" +
                          " because of no OnBoardingHost." +
                          $" url: {WorldBossQuery.Url}");
            }
            else
            {
                WorldBossQuery.SetUrl(_commandLineOptions.OnBoardingHost);
                Debug.Log("[Game] Start()... WorldBossQuery initialized." +
                          $" host: {_commandLineOptions.OnBoardingHost}" +
                          $" url: {WorldBossQuery.Url}");
            }

            // NOTE: Initialize market service.
            if (string.IsNullOrEmpty(_commandLineOptions.MarketServiceHost))
            {
                MarketServiceClient = new MarketServiceClient(string.Empty);
                Debug.Log("[Game] Start()... MarketServiceClient initialized with empty host url" +
                          " because of no MarketServiceHost");
            }
            else
            {
                MarketServiceClient = new MarketServiceClient(_commandLineOptions.MarketServiceHost);
                Debug.Log("[Game] Start()... MarketServiceClient initialized." +
                          $" host: {_commandLineOptions.MarketServiceHost}");
            }

            // NOTE: Initialize patrol reward service.
            if (string.IsNullOrEmpty(_commandLineOptions.PatrolRewardServiceHost))
            {
                PatrolRewardServiceClient = new NineChroniclesAPIClient(string.Empty);
                Debug.Log("[Game] Start()... PatrolRewardServiceClient initialized with empty host url" +
                          " because of no PatrolRewardServiceHost");
            }
            else
            {
                PatrolRewardServiceClient = new NineChroniclesAPIClient(
                    _commandLineOptions.PatrolRewardServiceHost);
                Debug.Log("[Game] Start()... PatrolRewardServiceClient initialized." +
                          $" host: {_commandLineOptions.PatrolRewardServiceHost}");
            }

            // NOTE: Initialize season pass service.
            if (string.IsNullOrEmpty(_commandLineOptions.SeasonPassServiceHost))
            {
                Debug.Log("[Game] Start()... SeasonPassServiceManager not initialized" +
                          " because of no SeasonPassServiceHost");
                SeasonPassServiceManager = new SeasonPassServiceManager(_commandLineOptions.SeasonPassServiceHost);
            }
            else
            {
                SeasonPassServiceManager = new SeasonPassServiceManager(_commandLineOptions.SeasonPassServiceHost);
                if (!string.IsNullOrEmpty(_commandLineOptions.GoogleMarketUrl))
                {
                    SeasonPassServiceManager.GoogleMarketURL = _commandLineOptions.GoogleMarketUrl;
                }

                if (!string.IsNullOrEmpty(_commandLineOptions.AppleMarketUrl))
                {
                    SeasonPassServiceManager.AppleMarketURL = _commandLineOptions.AppleMarketUrl;
                }

                Debug.Log("[Game] Start()... SeasonPassServiceManager initialized." +
                          $" host: {_commandLineOptions.SeasonPassServiceHost}" +
                          $", google: {SeasonPassServiceManager.GoogleMarketURL}" +
                          $", apple: {SeasonPassServiceManager.AppleMarketURL}");
            }

            sw.Stop();
            Debug.Log("[Game] Start()... Services(w/o IAPService) initialized in" +
                      $" {sw.ElapsedMilliseconds}ms.(elapsed)");

            StartCoroutine(InitializeIAP());

            yield return StartCoroutine(InitializeWithAgent());
            yield return createSecondWidgetCoroutine;

            var initializeSecondWidgetsCoroutine = StartCoroutine(CoInitializeSecondWidget());

#if RUN_ON_MOBILE
            var checkTokensTask = PortalConnect.CheckTokensAsync(States.AgentState.address);
            yield return checkTokensTask.AsCoroutine();
            if (!checkTokensTask.Result)
            {
                QuitWithMessage(L10nManager.Localize("ERROR_INITIALIZE_FAILED"),"Failed to Get Tokens.");
                yield break;
            }

            if (!planetContext.IsSelectedPlanetAccountPledged)
            {
                yield return StartCoroutine(CoCheckPledge(planetContext.SelectedPlanetInfo.ID));
            }
#endif

#if UNITY_EDITOR_WIN
            // wait for headless connect.
            if (useLocalMarketService && MarketHelper.CheckPath())
            {
                _marketThread = new Thread(() =>
                    MarketHelper.RunLocalMarketService(marketDbConnectionString));
                _marketThread.Start();
            }
#endif
            // Initialize D:CC NFT data
            StartCoroutine(CoInitDccAvatar());
            StartCoroutine(CoInitDccConnecting());
            yield return initializeSecondWidgetsCoroutine;
            grayLoadingScreen.ShowProgress(GameInitProgress.ProgressCompleted);
            Analyzer.Instance.Track("Unity/Intro/Start/SecondWidgetCompleted");

            var secondWidgetCompletedEvt = new AirbridgeEvent("Intro_Start_SecondWidgetCompleted");
            AirbridgeUnity.TrackEvent(secondWidgetCompletedEvt);

            // Initialize Stage
            sw.Reset();
            sw.Start();
            Stage.Initialize();
            sw.Stop();
            Debug.Log($"[Game] Start()... Stage initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
            sw.Reset();
            sw.Start();
            Arena.Initialize();
            sw.Stop();
            Debug.Log($"[Game] Start()... Arena initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
            sw.Reset();
            sw.Start();
            RaidStage.Initialize();
            sw.Stop();
            Debug.Log($"[Game] Start()... RaidStage initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
            // Initialize Rank.SharedModel
            RankPopup.UpdateSharedModel();
            Helper.Util.TryGetAppProtocolVersionFromToken(
                _commandLineOptions.AppProtocolVersion,
                out var appProtocolVersion);
            Widget.Find<VersionSystem>().SetVersion(appProtocolVersion);
            Analyzer.Instance.Track("Unity/Intro/Start/ShowNext");

            var showNextEvt = new AirbridgeEvent("Intro_Start_ShowNext");
            AirbridgeUnity.TrackEvent(showNextEvt);

            StartCoroutine(CoUpdate());
            ReservePushNotifications();

            yield return new WaitForSeconds(GrayLoadingScreen.SliderAnimationDuration);
            IsInitialized = true;
            Widget.Find<IntroScreen>().Close();
            EnterNext();
            totalSw.Stop();
            Debug.Log($"[Game] Game Start End. {totalSw.ElapsedMilliseconds}ms.");
            yield break;

            IEnumerator InitializeIAP()
            {
                grayLoadingScreen.ShowProgress(GameInitProgress.InitIAP);
                var innerSw = new Stopwatch();
                innerSw.Reset();
                innerSw.Start();
#if UNITY_IOS
                IAPServiceManager = new IAPServiceManager(_commandLineOptions.IAPServiceHost, Store.Apple);
#else
                //pc has to find iap product for mail box system
                IAPServiceManager = new IAPServiceManager(_commandLineOptions.IAPServiceHost, Store.Google);
#endif
                yield return IAPServiceManager.InitializeAsync().AsCoroutine();

                Task.Run(async () =>
                {
                    await MobileShop.LoadL10Ns();

                    var categorySchemas = await MobileShop.GetCategorySchemas();
                    foreach (var category in categorySchemas)
                    {
                        if (category.Name == "NoShow")
                        {
                            continue;
                        }

                        await Helper.Util.DownloadTextureRaw($"{MobileShop.MOBILE_L10N_SCHEMA.Host}/{category.Path}");

                        foreach (var product in category.ProductList)
                        {
                            await Helper.Util.DownloadTextureRaw($"{MobileShop.MOBILE_L10N_SCHEMA.Host}/{product.BgPath}");
                            await Helper.Util.DownloadTextureRaw($"{MobileShop.MOBILE_L10N_SCHEMA.Host}/{product.Path}");
                            await Helper.Util.DownloadTextureRaw($"{MobileShop.MOBILE_L10N_SCHEMA.Host}/{L10nManager.Localize(product.PopupPathKey)}");
                        }
                    }
                });

                innerSw.Stop();
                Debug.Log("[Game] Start()... IAPServiceManager initialized in" +
                          $" {innerSw.ElapsedMilliseconds}ms.(elapsed)");
                IAPStoreManager = gameObject.AddComponent<IAPStoreManager>();
            }

            IEnumerator InitializeWithAgent()
            {
                grayLoadingScreen.ShowProgress(GameInitProgress.InitTableSheet);
                var innerSw = new Stopwatch();
                innerSw.Reset();
                innerSw.Start();
                yield return SyncTableSheetsAsync().ToCoroutine();
                innerSw.Stop();
                Debug.Log($"[Game/SyncTableSheets] Start()... TableSheets synced in {innerSw.ElapsedMilliseconds}ms.(elapsed)");
                Analyzer.Instance.Track("Unity/Intro/Start/TableSheetsInitialized");

                var tableSheetsInitializedEvt = new AirbridgeEvent("Intro_Start_TableSheetsInitialized");
                AirbridgeUnity.TrackEvent(tableSheetsInitializedEvt);

                RxProps.Start(Agent, States, TableSheets);

                Event.OnUpdateAddresses.AsObservable().Subscribe(_ =>
                {
                    var petList = States.Instance.PetStates.GetPetStatesAll()
                        .Where(petState => petState != null)
                        .Select(petState => petState.PetId)
                        .ToList();
                    SavedPetId = !petList.Any() ? null : petList[Random.Range(0, petList.Count)];
                }).AddTo(gameObject);
            }

            IEnumerator CoInitializeSecondWidget()
            {
                grayLoadingScreen.ShowProgress(GameInitProgress.InitCanvas);
                var innerSw = new Stopwatch();
                innerSw.Reset();
                innerSw.Start();
                yield return StartCoroutine(MainCanvas.instance.InitializeSecondWidgets());
                innerSw.Stop();
                Debug.Log($"[Game] Start()... SecondWidgets initialized in {innerSw.ElapsedMilliseconds}ms.(elapsed)");
            }
        }

        protected override void OnDestroy()
        {
            if (_headlessThread is not null && _headlessThread.IsAlive)
            {
                _headlessThread.Interrupt();
            }

            if (_marketThread is not null && _marketThread.IsAlive)
            {
                _marketThread.Interrupt();
            }

            ActionManager?.Dispose();
            base.OnDestroy();
        }

        private void OnLoadCommandlineOptions()
        {
            if (_commandLineOptions is null)
            {
                const string message = "CommandLineOptions is null.";
                Debug.LogError(message);
                QuitWithMessage(
                    L10nManager.Localize("ERROR_INITIALIZE_FAILED"),
                    debugMessage: message);
                return;
            }

            if (debugConsolePrefab != null && _commandLineOptions.IngameDebugConsole)
            {
                Debug.Log("[Game] InGameDebugConsole enabled");
                Util.IngameDebugConsoleCommands.IngameDebugConsoleObj = Instantiate(debugConsolePrefab);
                Util.IngameDebugConsoleCommands.Initailize();
            }

            if (_commandLineOptions.RpcClient)
            {
                if (string.IsNullOrEmpty(_commandLineOptions.RpcServerHost))
                {
                    if (_commandLineOptions.RpcServerHosts == null ||
                        !_commandLineOptions.RpcServerHosts.Any())
                    {
                        const string message =
                            "RPC client mode requires RPC server host(s).";
                        Debug.LogError(message);
                        QuitWithMessage(
                            L10nManager.Localize("ERROR_INITIALIZE_FAILED"),
                            debugMessage: message);
                        return;
                    }

                    _commandLineOptions.RpcServerHost = _commandLineOptions.RpcServerHosts
                        .OrderBy(_ => Guid.NewGuid())
                        .First();
                }
            }

            Debug.Log("[Game] CommandLineOptions loaded");
            Debug.Log($"APV: {_commandLineOptions.AppProtocolVersion}");
            Debug.Log($"RPC: {_commandLineOptions.RpcServerHost}:{_commandLineOptions.RpcServerPort}");
        }

        private void SubscribeRPCAgent()
        {
            if (!(Agent is RPCAgent rpcAgent))
            {
                return;
            }

            Debug.Log("[Game]Subscribe RPCAgent");

            rpcAgent.OnRetryStarted
                .ObserveOnMainThread()
                .Subscribe(agent =>
                {
                    Debug.Log($"[Game]RPCAgent OnRetryStarted. {rpcAgent.Address.ToHex()}");
                    OnRPCAgentRetryStarted(agent);
                })
                .AddTo(gameObject);

            rpcAgent.OnRetryEnded
                .ObserveOnMainThread()
                .Subscribe(agent =>
                {
                    Debug.Log($"[Game]RPCAgent OnRetryEnded. {rpcAgent.Address.ToHex()}");
                    OnRPCAgentRetryEnded(agent);
                })
                .AddTo(gameObject);

            rpcAgent.OnPreloadStarted
                .ObserveOnMainThread()
                .Subscribe(agent =>
                {
                    Debug.Log($"[Game]RPCAgent OnPreloadStarted. {rpcAgent.Address.ToHex()}");
                    OnRPCAgentPreloadStarted(agent);
                })
                .AddTo(gameObject);

            rpcAgent.OnPreloadEnded
                .ObserveOnMainThread()
                .Subscribe(agent =>
                {
                    Debug.Log($"[Game]RPCAgent OnPreloadEnded. {rpcAgent.Address.ToHex()}");
                    OnRPCAgentPreloadEnded(agent);
                })
                .AddTo(gameObject);

            rpcAgent.OnDisconnected
                .ObserveOnMainThread()
                .Subscribe(agent =>
                {
                    Debug.Log($"[Game]RPCAgent OnDisconnected. {rpcAgent.Address.ToHex()}");
                    QuitWithAgentConnectionError(agent);
                })
                .AddTo(gameObject);
        }

        private static void OnRPCAgentRetryStarted(RPCAgent rpcAgent)
        {
            Widget.Find<DimmedLoadingScreen>().Show();
        }

        private static void OnRPCAgentRetryEnded(RPCAgent rpcAgent)
        {
            var widget = (Widget)Widget.Find<DimmedLoadingScreen>();
            if (widget.IsActive())
            {
                widget.Close();
            }
        }

        private static void OnRPCAgentPreloadStarted(RPCAgent rpcAgent)
        {
            // ignore.
        }

        private static void OnRPCAgentPreloadEnded(RPCAgent rpcAgent)
        {
            if (Widget.Find<IntroScreen>().IsActive() ||
                Widget.Find<GrayLoadingScreen>().IsActive() ||
                Widget.Find<Synopsis>().IsActive())
            {
                // NOTE: 타이틀 화면에서 리트라이와 프리로드가 완료된 상황입니다.
                // FIXME: 이 경우에는 메인 로비가 아니라 기존 초기화 로직이 흐르도록 처리해야 합니다.
                return;
            }

            var needToBackToMain = false;
            var showLoadingScreen = false;
            var widget = (Widget)Widget.Find<DimmedLoadingScreen>();
            if (widget.IsActive())
            {
                widget.Close();
            }

            if (Widget.Find<LoadingScreen>().IsActive())
            {
                Widget.Find<LoadingScreen>().Close();
                widget = Widget.Find<BattlePreparation>();
                if (widget.IsActive())
                {
                    widget.Close(true);
                    needToBackToMain = true;
                }

                widget = Widget.Find<Menu>();
                if (widget.IsActive())
                {
                    widget.Close(true);
                    needToBackToMain = true;
                }
            }
            else if (Widget.Find<StageLoadingEffect>().IsActive())
            {
                Widget.Find<StageLoadingEffect>().Close();

                if (Widget.Find<BattleResultPopup>().IsActive())
                {
                    Widget.Find<BattleResultPopup>().Close(true);
                }

                needToBackToMain = true;
                showLoadingScreen = true;
            }
            else if (Widget.Find<ArenaBattleLoadingScreen>().IsActive())
            {
                Widget.Find<ArenaBattleLoadingScreen>().Close();
                needToBackToMain = true;
            }

            if (!needToBackToMain)
            {
                return;
            }

            BackToMainAsync(new UnableToRenderWhenSyncingBlocksException(), showLoadingScreen)
                .Forget();
        }

        private void QuitWithAgentConnectionError(RPCAgent rpcAgent)
        {
            var screen = Widget.Find<DimmedLoadingScreen>();
            if (screen.IsActive())
            {
                screen.Close();
            }

            // FIXME 콜백 인자를 구조화 하면 타입 쿼리 없앨 수 있을 것 같네요.
            IconAndButtonSystem popup;
            if (Agent is Agent _)
            {
                var errorMsg = string.Format(L10nManager.Localize("UI_ERROR_FORMAT"),
                    L10nManager.Localize("BLOCK_DOWNLOAD_FAIL"));

                popup = Widget.Find<IconAndButtonSystem>();
                popup.Show(L10nManager.Localize("UI_ERROR"),
                    errorMsg,
                    L10nManager.Localize("UI_QUIT"),
                    false,
                    IconAndButtonSystem.SystemType.BlockChainError);
                popup.SetConfirmCallbackToExit();

                return;
            }

            if (rpcAgent is null)
            {
                // FIXME: 최신 버전이 뭔지는 Agent.EncounrtedHighestVersion 속성에 들어있으니, 그걸 UI에서 표시해줘야 할 듯?
                // AppProtocolVersion? newVersion = _agent is Agent agent ? agent.EncounteredHighestVersion : null;
                return;
            }

            if (rpcAgent.Connected)
            {
                // 무슨 상황이지?
                Debug.Log(
                    $"{nameof(QuitWithAgentConnectionError)}() called. But {nameof(RPCAgent)}.Connected is {rpcAgent.Connected}.");
                return;
            }

            popup = Widget.Find<IconAndButtonSystem>();
            popup.Show("UI_ERROR", "UI_ERROR_RPC_CONNECTION", "UI_QUIT");
            popup.SetConfirmCallbackToExit();
        }

        /// <summary>
        /// This method must be called after <see cref="Game.Agent"/> initialized.
        /// </summary>
        private IEnumerator CoCheckPledge(PlanetId planetId)
        {
            Debug.Log("[Game] CoCheckPledge() invoked.");
            if (!States.PledgeRequested || !States.PledgeApproved)
            {
                if (!States.PledgeRequested)
                {
                    Widget.Find<GrayLoadingScreen>().ShowProgress(GameInitProgress.RequestPledge);
                    var swForRequestPledge = new Stopwatch();
                    var swForRenderingRequestPledge = new Stopwatch();
                    while (!States.PledgeRequested)
                    {
                        Analyzer.Instance.Track("Unity/Intro/Pledge/Request");

                        var requestEvt = new AirbridgeEvent("Intro_Pledge_Request");
                        AirbridgeUnity.TrackEvent(requestEvt);

                        swForRequestPledge.Reset();
                        swForRequestPledge.Start();
                        yield return PortalConnect.RequestPledge(
                            planetId,
                            States.AgentState.address);
                        swForRequestPledge.Stop();
                        Debug.Log("[Game] CoCheckPledge()... PortalConnect.RequestPledge()" +
                                  $" finished in {swForRequestPledge.ElapsedMilliseconds}ms.(elapsed)");

                        if (!swForRenderingRequestPledge.IsRunning)
                        {
                            swForRenderingRequestPledge.Reset();
                            swForRenderingRequestPledge.Start();
                        }

                        yield return SetTimeOut(() => States.PledgeRequested);
                        if (States.PledgeRequested)
                        {
                            swForRenderingRequestPledge.Stop();
                            Debug.Log("[Game] CoCheckPledge()... Rendering RequestPledge" +
                                      $" finished in {swForRenderingRequestPledge.ElapsedMilliseconds}ms.(elapsed)");
                        }

                        Analyzer.Instance.Track("Unity/Intro/Pledge/Requested");

                        var requestedEvt = new AirbridgeEvent("Intro_Pledge_Requested");
                        AirbridgeUnity.TrackEvent(requestedEvt);
                    }
                }

                if (States.PledgeRequested && !States.PledgeApproved)
                {
                    Widget.Find<GrayLoadingScreen>().ShowProgress(GameInitProgress.ApprovePledge);
                    var swForRenderingApprovePledge = new Stopwatch();
                    swForRenderingApprovePledge.Reset();
                    swForRenderingApprovePledge.Start();
                    while (!States.PledgeApproved)
                    {
                        Analyzer.Instance.Track("Unity/Intro/Pledge/ApproveAction");

                        var approveActionEvt = new AirbridgeEvent("Intro_Start_ApproveAction");
                        AirbridgeUnity.TrackEvent(approveActionEvt);

                        var patronAddress = States.PatronAddress!.Value;
                        ActionManager.Instance.ApprovePledge(patronAddress).Subscribe();

                        yield return SetTimeOut(() => States.PledgeApproved);
                    }

                    swForRenderingApprovePledge.Stop();
                    Debug.Log("[Game] CoCheckPledge()... Rendering ApprovePledge" +
                              $" finished in {swForRenderingApprovePledge.ElapsedMilliseconds}ms.(elapsed)");

                    Analyzer.Instance.Track("Unity/Intro/Pledge/Approve");

                    var approveEvt = new AirbridgeEvent("Intro_Start_Approve");
                    AirbridgeUnity.TrackEvent(approveEvt);
                }

                Widget.Find<GrayLoadingScreen>().ShowProgress(GameInitProgress.EndPledge);
            }
            yield break;

            IEnumerator SetTimeOut(Func<bool> condition)
            {
                const int timeLimit = 180;

                // Wait 180 minutes
                for (int second = 0; second < timeLimit; second++)
                {
                    yield return new WaitForSeconds(1f);
                    if (condition.Invoke())
                    {
                        break;
                    }
                }

                if (!condition.Invoke())
                {
                    // Update Pledge States
                    var task = Task.Run(async () =>
                    {
                        var pledgeAddress = Agent.Address.GetPledgeAddress();
                        Address? patronAddress = null;
                        var approved = false;

                        if (await Agent.GetStateAsync(pledgeAddress) is List list)
                        {
                            patronAddress = list[0].ToAddress();
                            approved = list[1].ToBoolean();
                        }

                        States.Instance.SetPledgeStates(patronAddress, approved);
                    });
                    yield return new WaitUntil(() => task.IsCompleted);
                }

                if (!States.PledgeRequested)
                {
                    var clickRetry = false;
                    var popup = Widget.Find<TitleOneButtonSystem>();
                    popup.Show("Time Out", "Please try again", "Retry", false);
                    popup.SubmitCallback = () =>
                    {
                        clickRetry = true;
                        popup.Close();
                    };
                    yield return new WaitUntil(() => clickRetry);
                }
            }
        }

        // FIXME: Leave one between this or CoSyncTableSheets()
        private IEnumerator CoInitializeTableSheets()
        {
            yield return null;
            var request =
                Resources.LoadAsync<AddressableAssetsContainer>(AddressableAssetsContainerPath);
            yield return request;
            if (!(request.asset is AddressableAssetsContainer addressableAssetsContainer))
            {
                throw new FailedToLoadResourceException<AddressableAssetsContainer>(
                    AddressableAssetsContainerPath);
            }

            var csvAssets = addressableAssetsContainer.tableCsvAssets;
            var csv = new Dictionary<string, string>();
            foreach (var asset in csvAssets)
            {
                csv[asset.name] = asset.text;
            }

            TableSheets = new TableSheets(csv);
        }

        // FIXME: Return some of exceptions when table csv is `Null` in the chain.
        //        And if it is `Null` in the chain, then it should be handled in the caller.
        //        Show a popup with error message and quit the application.
        private async UniTask SyncTableSheetsAsync()
        {
            var sw = new Stopwatch();
            sw.Start();
            var container = await Resources
                .LoadAsync<AddressableAssetsContainer>(AddressableAssetsContainerPath)
                .ToUniTask();
            if (container is not AddressableAssetsContainer addressableAssetsContainer)
            {
                throw new FailedToLoadResourceException<AddressableAssetsContainer>(
                    AddressableAssetsContainerPath);
            }
            sw.Stop();
            Debug.Log($"[SyncTableSheets] load container: {sw.Elapsed}");
            sw.Restart();

            var csvAssets = addressableAssetsContainer.tableCsvAssets;
            var map = csvAssets.ToDictionary(
                asset => Addresses.TableSheet.Derive(asset.name),
                asset => asset.name);
            var dict = await Agent.GetSheetsAsync(map.Keys);
            sw.Stop();
            Debug.Log($"[SyncTableSheets] get state: {sw.Elapsed}");
            sw.Restart();
            var csv = dict.ToDictionary(
                pair => map[pair.Key],
                // NOTE: `pair.Value` is `null` when the chain not contains the `pair.Key`.
                pair =>
                {
                    if (pair.Value is Text)
                    {
                        return pair.Value.ToDotnetString();
                    }

                    return null;
                });

            TableSheets = new TableSheets(csv);
            sw.Stop();
            Debug.Log($"[SyncTableSheets] TableSheets cosntructor: {sw.Elapsed}");
        }

        public static IDictionary<string, string> GetTableCsvAssets()
        {
            var container =
                Resources.Load<AddressableAssetsContainer>(AddressableAssetsContainerPath);
            return container.tableCsvAssets.ToDictionary(asset => asset.name, asset => asset.text);
        }

        private static async void EnterNext()
        {
            Debug.Log("[Game] EnterNext() invoked");
            if (!GameConfig.IsEditor)
            {
                if (States.Instance.AgentState.avatarAddresses.Any() &&
                    Helper.Util.TryGetStoredAvatarSlotIndex(out var slotIndex) &&
                    States.Instance.AvatarStates.ContainsKey(slotIndex))
                {
                    var loadingScreen = Widget.Find<LoadingScreen>();
                    loadingScreen.Show(
                        LoadingScreen.LoadingType.Entering,
                        L10nManager.Localize("UI_LOADING_BOOTSTRAP_START"));
                    var sw = new Stopwatch();
                    sw.Reset();
                    sw.Start();
                    await RxProps.SelectAvatarAsync(slotIndex);
                    sw.Stop();
                    Debug.Log("[Game] EnterNext()... SelectAvatarAsync() finished in" +
                              $" {sw.ElapsedMilliseconds}ms.(elapsed)");
                    loadingScreen.Close();
                    Event.OnRoomEnter.Invoke(false);
                    Event.OnUpdateAddresses.Invoke();
                }
                else
                {
                    Widget.Find<Synopsis>().Show();
                }
            }
            else
            {
                PlayerFactory.Create();

                if (Helper.Util.TryGetStoredAvatarSlotIndex(out var slotIndex) &&
                    States.Instance.AvatarStates.ContainsKey(slotIndex))
                {
                    var avatarState = States.Instance.AvatarStates[slotIndex];
                    if (avatarState?.inventory == null ||
                        avatarState.questList == null ||
                        avatarState.worldInformation == null)
                    {
                        EnterLogin();
                    }
                    else
                    {
                        var sw = new Stopwatch();
                        sw.Reset();
                        sw.Start();
                        await RxProps.SelectAvatarAsync(slotIndex);
                        sw.Stop();
                        Debug.Log("[Game] EnterNext()... SelectAvatarAsync() finished in" +
                                  $" {sw.ElapsedMilliseconds}ms.(elapsed)");
                        Event.OnRoomEnter.Invoke(false);
                        Event.OnUpdateAddresses.Invoke();
                    }
                }
                else
                {
                    EnterLogin();
                }
            }

            Widget.Find<GrayLoadingScreen>().Close();
        }

        private static void EnterLogin()
        {
            Debug.Log("[Game] EnterLogin() invoked");
            Widget.Find<Login>().Show();
            Event.OnNestEnter.Invoke();
        }

        #endregion

        protected override void OnApplicationQuit()
        {
            ReservePushNotifications();

            if (Analyzer.Instance != null)
            {
                Analyzer.Instance.Track("Unity/Player Quit");
                Analyzer.Instance.Flush();

                var evt = new AirbridgeEvent("Intro_Player_Quit");
                AirbridgeUnity.TrackEvent(evt);
            }

            _logsClient?.Dispose();
        }

        private IEnumerator CoUpdate()
        {
            while (enabled)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    PlayMouseOnClickVFX(Input.mousePosition);
                }

                if (Input.GetKeyDown(KeyCode.Escape) &&
                    !Widget.IsOpenAnyPopup())
                {
                    Quit();
                }

                yield return null;
            }
        }

        public static async UniTaskVoid BackToMainAsync(Exception exc,
            bool showLoadingScreen = false)
        {
            Debug.LogException(exc);

            var (key, code, errorMsg) = await ErrorCode.GetErrorCodeAsync(exc);
            Event.OnRoomEnter.Invoke(showLoadingScreen);
            instance.Stage.OnRoomEnterEnd
                .First()
                .Subscribe(_ => PopupError(key, code, errorMsg));
            instance.Arena.OnRoomEnterEnd
                .First()
                .Subscribe(_ => PopupError(key, code, errorMsg));
            MainCanvas.instance.InitWidgetInMain();
        }

        public void BackToNest()
        {
            StartCoroutine(CoBackToNest());
        }

        private IEnumerator CoBackToNest()
        {
            yield return StartCoroutine(CoInitDccAvatar());
            yield return StartCoroutine(CoInitDccConnecting());

            if (IsInWorld)
            {
                NotificationSystem.Push(
                    Model.Mail.MailType.System,
                    L10nManager.Localize("UI_BLOCK_EXIT"),
                    NotificationCell.NotificationType.Information);
                yield break;
            }

            Event.OnNestEnter.Invoke();

            try
            {
                var deletableWidgets = Widget.FindWidgets().Where(widget =>
                    widget is not SystemWidget &&
                    widget is not MessageCatTooltip &&
                    widget.IsActive()).ToList();

                for (var i = deletableWidgets.Count - 1; i >= 0; i--)
                {
                    var widget = deletableWidgets[i];
                    if (widget)
                    {
                        widget.Close(true);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Widget.Find<Login>().Show();
        }

        public static async UniTaskVoid PopupError(Exception exc)
        {
            Debug.LogException(exc);
            var (key, code, errorMsg) = await ErrorCode.GetErrorCodeAsync(exc);
            PopupError(key, code, errorMsg);
        }

        private static void PopupError(string key, string code, string errorMsg)
        {
            errorMsg = errorMsg == string.Empty
                ? string.Format(
                    L10nManager.Localize("UI_ERROR_RETRY_FORMAT"),
                    L10nManager.Localize(key),
                    code)
                : errorMsg;
            var popup = Widget.Find<IconAndButtonSystem>();
            popup.Show(
                L10nManager.Localize("UI_ERROR"),
                errorMsg,
                L10nManager.Localize("UI_OK"),
                false);
            popup.SetConfirmCallbackToExit();
        }

        public static void Quit()
        {
            var popup = Widget.Find<QuitSystem>();
            if (popup.gameObject.activeSelf)
            {
                popup.Close();
                return;
            }

            popup.Show();
        }

        private static void PlayMouseOnClickVFX(Vector3 position)
        {
            position = MainCanvas.instance.Canvas.worldCamera.ScreenToWorldPoint(position);
            var vfx = VFXController.instance.CreateAndChaseCam<MouseClickVFX>(position);
            vfx.Play();
        }

        private IEnumerator CoLogin(PlanetContext planetContext, Action<bool> callback)
        {
            Debug.Log("[Game] CoLogin() invoked");
            if (_commandLineOptions.Maintenance)
            {
                var w = Widget.Create<IconAndButtonSystem>();
                w.CancelCallback = () =>
                {
                    Application.OpenURL(LiveAsset.GameConfig.DiscordLink);
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.ExitPlaymode();
#else
                    Application.Quit();
#endif
                };
                w.Show(
                    "UI_MAINTENANCE",
                    "UI_MAINTENANCE_CONTENT",
                    "UI_OK",
                    true,
                    IconAndButtonSystem.SystemType.Information
                );
                yield break;
            }

            if (_commandLineOptions.TestEnd)
            {
                var w = Widget.Find<ConfirmPopup>();
                w.CloseCallback = result =>
                {
                    if (result == ConfirmResult.Yes)
                    {
                        Application.OpenURL(LiveAsset.GameConfig.DiscordLink);
                    }

#if UNITY_EDITOR
                    UnityEditor.EditorApplication.ExitPlaymode();
#else
                    Application.Quit();
#endif
                };
                w.Show("UI_TEST_END", "UI_TEST_END_CONTENT", "UI_GO_DISCORD", "UI_QUIT");

                yield break;
            }

            var introScreen = Widget.Find<IntroScreen>();
            var loginSystem = Widget.Find<LoginSystem>();
            var dimmedLoadingScreen = Widget.Find<DimmedLoadingScreen>();
            var sw = new Stopwatch();
            if (Application.isBatchMode)
            {
                loginSystem.Show(_commandLineOptions.KeyStorePath, _commandLineOptions.PrivateKey);
                sw.Reset();
                sw.Start();
                yield return Agent.Initialize(
                    _commandLineOptions,
                    loginSystem.GetPrivateKey(),
                    callback);
                sw.Stop();
                Debug.Log($"[Game] CoLogin()... Agent initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
                yield break;
            }

            // NOTE: planetContext is null when the game is launched from the non-mobile platform.
            if (planetContext is null)
            {
                Debug.Log("[Game] CoLogin()... PlanetContext is null.");
                if (!loginSystem.Login)
                {
                    Debug.Log("[Game] CoLogin()... LoginSystem.Login is false");
                    if (!loginSystem.TryLoginWithLocalPpk())
                    {
                        Debug.Log("[Game] CoLogin()... LoginSystem.TryLoginWithLocalPpk() is false.");
                        introScreen.Show(
                            _commandLineOptions.KeyStorePath,
                            _commandLineOptions.PrivateKey,
                            planetContext: null);
                    }

                    Debug.Log("[Game] CoLogin()... WaitUntil LoginPopup.Login.");
                    yield return new WaitUntil(() => loginSystem.Login);
                    Debug.Log("[Game] CoLogin()... WaitUntil LoginPopup.Login. Done.");

                    // NOTE: Update CommandlineOptions.PrivateKey finally.
                    _commandLineOptions.PrivateKey = loginSystem.GetPrivateKey().ToHexWithZeroPaddings();
                    Debug.Log("[Game] CoLogin()... CommandLineOptions.PrivateKey finally updated" +
                              $" to ({loginSystem.GetPrivateKey().Address}).");
                }

                dimmedLoadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForConnectingToPlanet);
                sw.Reset();
                sw.Start();
                yield return Agent.Initialize(
                    _commandLineOptions,
                    loginSystem.GetPrivateKey(),
                    callback);
                sw.Stop();
                Debug.Log($"[Game] CoLogin()... Agent initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
                dimmedLoadingScreen.Close();
                yield break;
            }

            // NOTE: Initialize current planet info.
            sw.Reset();
            sw.Start();
            planetContext = PlanetSelector.InitializeSelectedPlanetInfo(planetContext);
            sw.Stop();
            Debug.Log($"[Game] CoLogin()... PlanetInfo selected in {sw.ElapsedMilliseconds}ms.(elapsed)");
            if (planetContext.HasError)
            {
                callback?.Invoke(false);
                yield break;
            }

            // NOTE: Check already logged in or local passphrase.
            if (loginSystem.Login ||
                loginSystem.TryLoginWithLocalPpk())
            {
                Debug.Log("[Game] CoLogin()... LocalSystem.Login is true or" +
                          " LoginSystem.TryLoginWithLocalPpk() is true.");
                var pk = loginSystem.GetPrivateKey();

                // NOTE: Update CommandlineOptions.PrivateKey.
                _commandLineOptions.PrivateKey = pk.ToHexWithZeroPaddings();
                Debug.Log("[Game] CoLogin()... CommandLineOptions.PrivateKey updated" +
                          $" to ({pk.Address}).");

                // NOTE: Check PlanetContext.CanSkipPlanetSelection.
                //       If true, then update planet account infos for IntroScreen.
                if (planetContext.CanSkipPlanetSelection.HasValue && planetContext.CanSkipPlanetSelection.Value)
                {
                    Debug.Log("[Game] CoLogin()... PlanetContext.CanSkipPlanetSelection is true.");
                    dimmedLoadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForPlanetAccountInfoSyncing);
                    yield return PlanetSelector.UpdatePlanetAccountInfosAsync(
                        planetContext,
                        pk.Address,
                        updateSelectedPlanetAccountInfo: true).ToCoroutine();
                    dimmedLoadingScreen.Close();
                    if (planetContext.HasError)
                    {
                        callback?.Invoke(false);
                        yield break;
                    }
                }

                introScreen.SetData(
                    _commandLineOptions.KeyStorePath,
                    pk.ToHexWithZeroPaddings(),
                    planetContext);
            }
            else
            {
                Debug.Log("[Game] CoLogin()... LocalSystem.Login is false.");
                // NOTE: If we need to cover the Multiplanetary context on non-mobile platform,
                //       we need to reconsider the invoking the IntroScreen.Show(pkPath, pk, planetContext)
                //       in here.
                introScreen.SetData(
                    _commandLineOptions.KeyStorePath,
                    _commandLineOptions.PrivateKey,
                    planetContext);
            }

            if (planetContext.HasPledgedAccount)
            {
                Debug.Log("[Game] CoLogin()... Has pledged account.");
                var pk = loginSystem.GetPrivateKey();
                introScreen.Show(
                    _commandLineOptions.KeyStorePath,
                    pk.ToHexWithZeroPaddings(),
                    planetContext);

                Debug.Log("[Game] CoLogin()... WaitUntil introScreen.OnClickStart.");
                yield return introScreen.OnClickStart.AsObservable().First().StartAsCoroutine();
                Debug.Log("[Game] CoLogin()... WaitUntil introScreen.OnClickStart. Done.");

                // NOTE: Update CommandlineOptions.PrivateKey finally.
                _commandLineOptions.PrivateKey = pk.ToHexWithZeroPaddings();
                Debug.Log("[Game] CoLogin()... CommandLineOptions.PrivateKey finally updated" +
                          $" to ({pk.Address}).");

                dimmedLoadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForConnectingToPlanet);
                sw.Reset();
                sw.Start();
                yield return Agent.Initialize(
                    _commandLineOptions,
                    pk,
                    callback);
                sw.Stop();
                dimmedLoadingScreen.Close();
                Debug.Log($"[Game] CoLogin()... Agent initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
                yield break;
            }

            // NOTE: Show IntroScreen's tab to start button.
            //       It should be called after the PlanetSelector.InitializeSelectedPlanetInfo().
            //       Because the IntroScreen uses the PlanetContext.SelectedPlanetInfo.
            //       And it should be called after the IntroScreen.SetData().
            introScreen.ShowTabToStart();
            Debug.Log("[Game] CoLogin()... WaitUntil introScreen.OnClickTabToStart.");
            yield return introScreen.OnClickTabToStart.AsObservable().First().StartAsCoroutine();
            Debug.Log("[Game] CoLogin()... WaitUntil introScreen.OnClickTabToStart. Done.");

            string email = null;
            Address? agentAddrInPortal;
            if (SigninContext.HasLatestSignedInSocialType)
            {
                var startClicked = false;
                introScreen.OnClickStart.AsObservable()
                    .First()
                    .Subscribe(_ => startClicked = true);
                dimmedLoadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForPortalAuthenticating);
                sw.Reset();
                sw.Start();
                var getTokensTask = PortalConnect.GetTokensSilentlyAsync();
                yield return new WaitUntil(() => getTokensTask.IsCompleted);
                sw.Stop();
                Debug.Log($"[Game] CoLogin()... Portal signed in in {sw.ElapsedMilliseconds}ms.(elapsed)");
                dimmedLoadingScreen.Close();
                (email, _, agentAddrInPortal) = getTokensTask.Result;
                if (!startClicked)
                {
                    Debug.Log("[Game] CoLogin()... WaitUntil introScreen.OnClickStart.");
                    yield return new WaitUntil(() => startClicked);
                    Debug.Log("[Game] CoLogin()... WaitUntil introScreen.OnClickStart. Done.");
                }
            }
            else
            {
                // NOTE: Social login flow.
                Debug.Log("[Game] CoLogin()... Go to social login flow.");
                var socialType = SigninContext.SocialType.Apple;
                string idToken = null;
                introScreen.OnSocialSignedIn.AsObservable()
                    .First()
                    .Subscribe(value =>
                    {
                        socialType = value.socialType;
                        email = value.email;
                        idToken = value.idToken;
                    });

                Debug.Log("[Game] CoLogin()... WaitUntil introScreen.OnSocialSignedIn.");
                yield return new WaitUntil(() => idToken is not null);
                Debug.Log("[Game] CoLogin()... WaitUntil introScreen.OnSocialSignedIn. Done.");

                // NOTE: Portal login flow.
                dimmedLoadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForPortalAuthenticating);
                Debug.Log("[Game] CoLogin()... WaitUntil PortalConnect.Send{Apple|Google}IdTokenAsync.");
                sw.Reset();
                sw.Start();
                var portalSigninTask = socialType == SigninContext.SocialType.Apple
                    ? PortalConnect.SendAppleIdTokenAsync(idToken)
                    : PortalConnect.SendGoogleIdTokenAsync(idToken);
                yield return new WaitUntil(() => portalSigninTask.IsCompleted);
                sw.Stop();
                Debug.Log($"[Game] CoLogin()... Portal signed in in {sw.ElapsedMilliseconds}ms.(elapsed)");
                Debug.Log("[Game] CoLogin()... WaitUntil PortalConnect.Send{Apple|Google}IdTokenAsync. Done.");
                dimmedLoadingScreen.Close();

                agentAddrInPortal = portalSigninTask.Result;
            }

            // NOTE: Update PlanetContext.PlanetAccountInfos.
            if (agentAddrInPortal is null)
            {
                Debug.Log("[Game] CoLogin()... AgentAddress in portal is null");
                if (!loginSystem.Login)
                {
                    Debug.Log("[Game] CoLogin()... LoginSystem.Login is false");
                    loginSystem.Show(connectedAddress: null);
                    // NOTE: Don't set the autoGeneratedAgentAddress to agentAddr.
                    var autoGeneratedAgentAddress = loginSystem.GetPrivateKey().Address;
                    Debug.Log($"[Game] CoLogin()... auto generated agent address: {autoGeneratedAgentAddress}." +
                              " And Update planet account infos w/ empty agent address.");
                }

                // NOTE: Initialize planet account infos as default(empty) value
                //       when agent address is not set.
                planetContext.PlanetAccountInfos = planetContext.PlanetRegistry?.PlanetInfos
                    .Select(planetInfo => new PlanetAccountInfo(
                        planetInfo.ID,
                        agentAddress: null,
                        isAgentPledged: null))
                    .ToArray();
            }
            else
            {
                var requiredAddress = agentAddrInPortal.Value;
                Debug.Log($"[Game] CoLogin()... AgentAddress({requiredAddress}) in portal" +
                          $" is not null. Try to update planet account infos.");
                dimmedLoadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForPlanetAccountInfoSyncing);
                yield return PlanetSelector.UpdatePlanetAccountInfosAsync(
                    planetContext,
                    requiredAddress,
                    updateSelectedPlanetAccountInfo: false).ToCoroutine();
                dimmedLoadingScreen.Close();
                if (planetContext.HasError)
                {
                    callback?.Invoke(false);
                    yield break;
                }
            }

            // NOTE: Check if the planets have at least one agent.
            if (planetContext.HasPledgedAccount)
            {
                Debug.Log("[Game] CoLogin()... Has pledged account. Show planet account infos popup.");
                introScreen.ShowPlanetAccountInfosPopup(planetContext, !loginSystem.Login);

                Debug.Log("[Game] CoLogin()... WaitUntil planetContext.SelectedPlanetAccountInfo" +
                          " is not null.");
                yield return new WaitUntil(() => planetContext.SelectedPlanetAccountInfo is not null);
                Debug.Log("[Game] CoLogin()... WaitUntil planetContext.SelectedPlanetAccountInfo" +
                          $" is not null. Done. {planetContext.SelectedPlanetAccountInfo!.PlanetId}");

                if (planetContext.IsSelectedPlanetAccountPledged)
                {
                    // NOTE: Player selected the planet that has agent.
                    Debug.Log("[Game] CoLogin()... Try to import key w/ QR code." +
                              " Player don't have to make a pledge.");

                    // NOTE: Complex logic here...
                    //       - LoginSystem.Login is false.
                    //       - Portal has player's account.
                    //       - Click the IntroScreen.AgentInfo.accountImportKeyButton.
                    //         - Import the agent key.
                    if (!loginSystem.Login)
                    {
                        // NOTE: QR code import sets loginSystem.Login to true.
                        introScreen.ShowForQrCodeGuide();
                    }
                }
                else
                {
                    // NOTE: Player selected the planet that has no agent.
                    Debug.Log("[Game] CoLogin()... Try to create a new agent." +
                              " Player may have to make a pledge.");

                    // NOTE: Complex logic here...
                    //       - LoginSystem.Login is false.
                    //       - Portal has player's account.
                    //       - Click the IntroScreen.AgentInfo.noAccountCreateButton.
                    //         - Create a new agent in a new planet.
                    if (!loginSystem.Login)
                    {
                        // NOTE: QR code import sets loginSystem.Login to true.
                        introScreen.ShowForQrCodeGuide();
                    }
                }
            }
            else
            {
                Debug.Log("[Game] CoLogin()... pledged account not exist.");
                if (!loginSystem.Login)
                {
                    Debug.Log("[Game] CoLogin()... LoginSystem.Login is false");

                    // FIXME: 이 분기의 상황
                    //        - 포탈에는 에이전트 A의 주소가 있다.
                    //        - 플래닛 레지스트리를 기준으로 에이전트 A는 아직 플렛지한 플래닛이 없다.
                    //        - 로컬에 에이전트 키가 없어서 로그인되지 않았다.
                    //
                    //        그래서 할 일.
                    //        - 로컬 키스토어를 순회하면서 에이전트 A의 주소와 같은 키를 찾는다.
                    //        - 포탈에 등록된 에이전트 A의 주소를 보여주면서, 이에 대응하는 키를 QR로 가져오라고 안내한다.
                    //        - 등...
                    //
                    // FIXME: States of here.
                    //        - Portal has agent address A which connected with social account.
                    //        - No planet which pledged by agent address A in the planet registry.
                    //        - LoginSystem.Login is false because of no agent key in the local.
                    //
                    //        So, we have to do.
                    //        - Find the agent key which has same address with agent address A in the local.
                    //        - While showing the agent address A, request to import the agent key which is
                    //          corresponding to the agent address A.
                    //        - etc...

                    Debug.LogError("Portal has agent address which connected with social account." +
                                   " But no agent states in the all planets." +
                                   $"\n Portal: {PortalConnect.PortalUrl}" +
                                   $"\n Social Account: {email}" +
                                   $"\n Agent Address in portal: {agentAddrInPortal}");
                    planetContext.SetError(
                        PlanetContext.ErrorType.UnsupportedCase01,
                        PortalConnect.PortalUrl,
                        email,
                        agentAddrInPortal?.ToString() ?? "null");
                    callback?.Invoke(false);
                    yield break;
                }

                Debug.Log("[Game] CoLogin()... Player have to make a pledge.");
                Debug.Log("[Game] CoLogin()... Set planetContext.SelectedPlanetAccountInfo" +
                          " w/ planetContext.SelectedPlanetInfo.ID.");
                planetContext.SelectedPlanetAccountInfo = planetContext.PlanetAccountInfos!.First(e =>
                    e.PlanetId.Equals(planetContext.SelectedPlanetInfo!.ID));
            }

            CurrentSocialEmail = email == null ? string.Empty : email;

            Debug.Log("[Game] CoLogin()... WaitUntil loginPopup.Login.");
            yield return new WaitUntil(() => loginSystem.Login);
            Debug.Log("[Game] CoLogin()... WaitUntil loginPopup.Login. Done.");

            // NOTE: Update CommandlineOptions.PrivateKey finally.
            _commandLineOptions.PrivateKey = loginSystem.GetPrivateKey().ToHexWithZeroPaddings();
            Debug.Log("[Game] CoLogin()... CommandLineOptions.PrivateKey finally updated" +
                      $" to ({loginSystem.GetPrivateKey().Address}).");

            dimmedLoadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForConnectingToPlanet);
            sw.Reset();
            sw.Start();
            yield return Agent.Initialize(
                _commandLineOptions,
                loginSystem.GetPrivateKey(),
                callback);
            sw.Stop();
            dimmedLoadingScreen.Close();
            Debug.Log($"[Game] CoLogin()... Agent initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
        }

        public void ResetStore()
        {
            var confirm = Widget.Find<ConfirmPopup>();
            var storagePath =
                _commandLineOptions.StoragePath ?? Blockchain.Agent.DefaultStoragePath;
            confirm.CloseCallback = result =>
            {
                if (result == ConfirmResult.No)
                {
                    return;
                }

                StoreUtils.ResetStore(storagePath);

#if UNITY_EDITOR
                UnityEditor.EditorApplication.ExitPlaymode();
#else
                Application.Quit();
#endif
            };
            confirm.Show("UI_CONFIRM_RESET_STORE_TITLE", "UI_CONFIRM_RESET_STORE_CONTENT");
        }

        public void ResetKeyStore()
        {
            var confirm = Widget.Find<ConfirmPopup>();
            confirm.CloseCallback = result =>
            {
                if (result == ConfirmResult.No)
                {
                    return;
                }

                var keyPath = _commandLineOptions.KeyStorePath;
                if (Directory.Exists(keyPath))
                {
                    Directory.Delete(keyPath, true);
                }

#if UNITY_EDITOR
                UnityEditor.EditorApplication.ExitPlaymode();
#else
                Application.Quit();
#endif
            };

            confirm.Show("UI_CONFIRM_RESET_KEYSTORE_TITLE", "UI_CONFIRM_RESET_KEYSTORE_CONTENT");
        }

        private async void UploadLog(string logString, string stackTrace, LogType type)
        {
            // Avoid NRE
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (Agent.PrivateKey is null)
            {
                _msg += logString + "\n";
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    _msg += stackTrace + "\n";
                }
            }
            else
            {
                const string groupName = "9c-player-logs";
                var streamName = _commandLineOptions.AwsSinkGuid;
                try
                {
                    var req = new CreateLogGroupRequest(groupName);
                    await _logsClient.CreateLogGroupAsync(req);
                }
                catch (ResourceAlreadyExistsException)
                {
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                try
                {
                    var req = new CreateLogStreamRequest(groupName, streamName);
                    await _logsClient.CreateLogStreamAsync(req);
                }
                catch (ResourceAlreadyExistsException)
                {
                    // ignored
                }

                PutLog(groupName, streamName, GetMessage(logString, stackTrace));
            }
        }

        private async void PutLog(string groupName, string streamName, string msg)
        {
            try
            {
                var req = new DescribeLogStreamsRequest(groupName)
                {
                    LogStreamNamePrefix = streamName
                };
                var resp = await _logsClient.DescribeLogStreamsAsync(req);
                var token = resp.LogStreams.FirstOrDefault(s =>
                    s.LogStreamName == streamName)?.UploadSequenceToken;
                var ie = new InputLogEvent
                {
                    Message = msg,
                    Timestamp = DateTime.UtcNow
                };
                var request = new PutLogEventsRequest(
                    groupName,
                    streamName,
                    new List<InputLogEvent> { ie });
                if (!string.IsNullOrEmpty(token))
                {
                    request.SequenceToken = token;
                }

                await _logsClient.PutLogEventsAsync(request);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private string GetMessage(string logString, string stackTrace)
        {
            var msg = string.Empty;
            if (!string.IsNullOrEmpty(_msg))
            {
                msg = _msg;
                _msg = string.Empty;
                return msg;
            }

            msg += logString + "\n";
            if (!string.IsNullOrEmpty(stackTrace))
            {
                msg += stackTrace;
            }

            return msg;
        }

        private IEnumerator CoInitDccAvatar()
        {
            var sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            yield return RequestManager.instance.GetJson(
                URL.DccAvatars,
                URL.DccEthChainHeaderName,
                URL.DccEthChainHeaderValue,
                json =>
                {
                    var responseData = DccAvatars.FromJson(json);
                    Dcc.instance.Init(responseData.Avatars);
                },
                timeOut: Dcc.TimeOut);
            sw.Stop();
            Debug.Log($"[Game] CoInitDccAvatar()... DCC Avatar initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
        }

        private IEnumerator CoInitDccConnecting()
        {
            var sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            yield return RequestManager.instance.GetJson(
                $"{URL.DccMileageAPI}{Agent.Address}",
                URL.DccEthChainHeaderName,
                URL.DccEthChainHeaderValue,
                _ => { Dcc.instance.IsConnected = true; },
                _ => { Dcc.instance.IsConnected = false; });
            sw.Stop();
            Debug.Log("[Game] CoInitDccConnecting()... DCC Connecting initialized in" +
                      $" {sw.ElapsedMilliseconds}ms.(elapsed)");
        }

        public void PauseTimeline(PlayableDirector whichOne)
        {
            _activeDirector = whichOne;
            _activeDirector.playableGraph.GetRootPlayable(0).SetSpeed(0);
        }

        public void ResumeTimeline()
        {
            _activeDirector.playableGraph.GetRootPlayable(0).SetSpeed(1);
        }

        private void ReservePushNotifications()
        {
            var currentBlockIndex = Agent.BlockIndex;
            var tableSheets = TableSheets.Instance;
            var roundData = tableSheets.ArenaSheet.GetRoundByBlockIndex(currentBlockIndex);
            var row = tableSheets.ArenaSheet.GetRowByBlockIndex(currentBlockIndex);
            var medalTotalCount = States.Instance.CurrentAvatarState != null
                ? ArenaHelper.GetMedalTotalCount(
                    row,
                    States.Instance.CurrentAvatarState)
                : default;
            if (medalTotalCount >= roundData.RequiredMedalCount)
            {
                ReserveArenaSeasonPush(row, roundData, currentBlockIndex);
                ReserveArenaTicketPush(roundData, currentBlockIndex);
            }

            ReserveWorldbossSeasonPush(currentBlockIndex);
            ReserveWorldbossTicketPush(currentBlockIndex);
        }

        private static void ReserveArenaSeasonPush(
            ArenaSheet.Row row,
            ArenaSheet.RoundData roundData,
            long currentBlockIndex)
        {
            var arenaSheet = TableSheets.Instance.ArenaSheet;
            if (roundData.ArenaType == ArenaType.OffSeason &&
                arenaSheet.TryGetNextRound(currentBlockIndex, out var nextRoundData))
            {
                var prevPushIdentifier =
                    PlayerPrefs.GetString(ArenaSeasonPushIdentifierKey, string.Empty);
                if (!string.IsNullOrEmpty(prevPushIdentifier))
                {
                    PushNotifier.CancelReservation(prevPushIdentifier);
                    PlayerPrefs.DeleteKey(ArenaSeasonPushIdentifierKey);
                }

                var targetBlockIndex = nextRoundData.StartBlockIndex;
                var timeSpan = (targetBlockIndex - currentBlockIndex).BlockToTimeSpan();

                var arenaTypeText = nextRoundData.ArenaType == ArenaType.Season
                    ? L10nManager.Localize("UI_SEASON")
                    : L10nManager.Localize("UI_CHAMPIONSHIP");

                var arenaSeason = nextRoundData.ArenaType == ArenaType.Championship
                    ? roundData.ChampionshipId
                    : row.GetSeasonNumber(nextRoundData.Round);

                var content = L10nManager.Localize(
                    "PUSH_ARENA_SEASON_START_CONTENT",
                    arenaTypeText,
                    arenaSeason);
                var identifier = PushNotifier.Push(content, timeSpan, PushNotifier.PushType.Arena);
                PlayerPrefs.SetString(ArenaSeasonPushIdentifierKey, identifier);
            }
        }

        private static void ReserveArenaTicketPush(
            ArenaSheet.RoundData roundData,
            long currentBlockIndex)
        {
            var prevPushIdentifier =
                PlayerPrefs.GetString(ArenaTicketPushIdentifierKey, string.Empty);
            if (RxProps.ArenaTicketsProgress.HasValue &&
                RxProps.ArenaTicketsProgress.Value.currentTickets <= 0)
            {
                if (!string.IsNullOrEmpty(prevPushIdentifier))
                {
                    PushNotifier.CancelReservation(prevPushIdentifier);
                    PlayerPrefs.DeleteKey(ArenaTicketPushIdentifierKey);
                }

                return;
            }

            var interval = States.Instance.GameConfigState.DailyArenaInterval;
            var remainingBlockCount = interval -
                                      (currentBlockIndex - roundData.StartBlockIndex) % interval;
            if (remainingBlockCount < TicketPushBlockCountThreshold)
            {
                return;
            }

            if (!string.IsNullOrEmpty(prevPushIdentifier))
            {
                PushNotifier.CancelReservation(prevPushIdentifier);
                PlayerPrefs.DeleteKey(ArenaTicketPushIdentifierKey);
            }

            var timeSpan = (remainingBlockCount - TicketPushBlockCountThreshold).BlockToTimeSpan();
            var content = L10nManager.Localize("PUSH_ARENA_TICKET_CONTENT");
            var identifier = PushNotifier.Push(content, timeSpan, PushNotifier.PushType.Arena);
            PlayerPrefs.SetString(ArenaTicketPushIdentifierKey, identifier);
        }

        private void ReserveWorldbossSeasonPush(long currentBlockIndex)
        {
            if (!WorldBossFrontHelper.TryGetNextRow(currentBlockIndex, out var row))
            {
                return;
            }

            var prevPushIdentifier =
                PlayerPrefs.GetString(WorldbossSeasonPushIdentifierKey, string.Empty);
            if (!string.IsNullOrEmpty(prevPushIdentifier))
            {
                PushNotifier.CancelReservation(prevPushIdentifier);
                PlayerPrefs.DeleteKey(WorldbossSeasonPushIdentifierKey);
            }

            var targetBlockIndex = row.StartedBlockIndex;
            var timeSpan = (targetBlockIndex - currentBlockIndex).BlockToTimeSpan();

            var content = L10nManager.Localize("PUSH_WORLDBOSS_SEASON_START_CONTENT", row.Id);
            var identifier = PushNotifier.Push(content, timeSpan, PushNotifier.PushType.Worldboss);
            PlayerPrefs.SetString(WorldbossSeasonPushIdentifierKey, identifier);
        }

        private void ReserveWorldbossTicketPush(long currentBlockIndex)
        {
            var prevPushIdentifier =
                PlayerPrefs.GetString(WorldbossTicketPushIdentifierKey, string.Empty);
            var interval = States.Instance.GameConfigState.DailyWorldBossInterval;
            var raiderState = States.Instance.CurrentAvatarState != null
                ? WorldBossStates.GetRaiderState(States.Instance.CurrentAvatarState.address)
                : null;
            var remainingTicket = raiderState != null
                ? WorldBossFrontHelper.GetRemainTicket(raiderState, currentBlockIndex, interval)
                : default;

            if (remainingTicket <= 0 ||
                !WorldBossFrontHelper.TryGetCurrentRow(currentBlockIndex, out var row))
            {
                if (!string.IsNullOrEmpty(prevPushIdentifier))
                {
                    PushNotifier.CancelReservation(prevPushIdentifier);
                    PlayerPrefs.DeleteKey(WorldbossTicketPushIdentifierKey);
                }

                return;
            }

            var remainingBlockCount =
                interval - ((currentBlockIndex - row.StartedBlockIndex) % interval);
            if (remainingBlockCount < TicketPushBlockCountThreshold)
            {
                return;
            }

            if (!string.IsNullOrEmpty(prevPushIdentifier))
            {
                PushNotifier.CancelReservation(prevPushIdentifier);
                PlayerPrefs.DeleteKey(WorldbossTicketPushIdentifierKey);
            }

            var timeSpan = (remainingBlockCount - TicketPushBlockCountThreshold).BlockToTimeSpan();
            var content = L10nManager.Localize("PUSH_WORLDBOSS_TICKET_CONTENT");
            var identifier = PushNotifier.Push(content, timeSpan, PushNotifier.PushType.Worldboss);
            PlayerPrefs.SetString(WorldbossTicketPushIdentifierKey, identifier);
        }

        private void InitializeAnalyzer(
            Address? agentAddr = null,
            PlanetId? planetId = null,
            string rpcServerHost = null)
        {
            Debug.Log("[Game] InitializeAnalyzer() invoked." +
                      $" agentAddr: {agentAddr}, planetId: {planetId}, rpcServerHost: {rpcServerHost}");
#if UNITY_EDITOR
            Debug.Log("[Game] InitializeAnalyzer()... Analyze is disabled in editor mode.");
            Analyzer = new Analyzer(
                agentAddr?.ToString(),
                planetId?.ToString(),
                rpcServerHost,
                isTrackable: false);
            return;
#endif

            var isTrackable = true;
            if (UnityEngine.Debug.isDebugBuild)
            {
                Debug.Log("This is debug build.");
                isTrackable = false;
            }

            if (_commandLineOptions.Development)
            {
                Debug.Log("This is development mode.");
                isTrackable = false;
            }

            Analyzer = new Analyzer(
                agentAddr?.ToString(),
                planetId?.ToString(),
                rpcServerHost,
                isTrackable);
        }

        public void ShowCLO()
        {
            Debug.Log(_commandLineOptions.ToString());
        }

        private static void QuitWithMessage(string message, string debugMessage = null)
        {
            message = string.IsNullOrEmpty(debugMessage)
                ? message
                : message + "\n" + debugMessage;

            if (!Widget.TryFind<OneButtonSystem>(out var widget))
            {
                widget = Widget.Create<OneButtonSystem>();
            }

            widget.Show(
                message,
                L10nManager.Localize("UI_QUIT"),
                () =>
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
        }

        private void OnLowMemory()
        {
            GC.Collect();
        }

        private async UniTask UpdateCurrentPlanetIdAsync(PlanetContext planetContext)
        {
#if RUN_ON_MOBILE
            CurrentPlanetId = planetContext.SelectedPlanetInfo.ID;
            return;
#endif
            Debug.Log("[Game] UpdateCurrentPlanetIdAsync()... Try to set current planet id.");
            if (!string.IsNullOrEmpty(_commandLineOptions.SelectedPlanetId))
            {
                Debug.Log("[Game] UpdateCurrentPlanetIdAsync()... SelectedPlanetId is not null.");
                CurrentPlanetId = new PlanetId(_commandLineOptions.SelectedPlanetId);
                return;
            }

            Debug.LogWarning("[Game] UpdateCurrentPlanetIdAsync()... SelectedPlanetId is null." +
                             " Try to get planet id from planet registry.");
            if (planetContext is null)
            {
                if (string.IsNullOrEmpty(_commandLineOptions.PlanetRegistryUrl))
                {
                    Debug.LogWarning("[Game] UpdateCurrentPlanetIdAsync()..." +
                                     " CommandLineOptions.PlanetRegistryUrl is null.");
                    return;
                }

                planetContext = new PlanetContext(_commandLineOptions);
                await PlanetSelector.InitializePlanetRegistryAsync(planetContext);
                if (planetContext.IsSkipped)
                {
                    Debug.LogWarning("[Game] UpdateCurrentPlanetIdAsync()..." +
                                     " planetContext.IsSkipped is true." +
                                     " You can consider to use CommandLineOptions.SelectedPlanetId instead.");
                    return;
                }

                if (planetContext.HasError)
                {
                    Debug.LogWarning("[Game] UpdateCurrentPlanetIdAsync()..." +
                                     " planetContext.HasError is true." +
                                     " You can consider to use CommandLineOptions.SelectedPlanetId instead.");
                    return;
                }
            }

            if (planetContext.PlanetRegistry!.TryGetPlanetInfoByHeadlessGrpc(
                    _commandLineOptions.RpcServerHost,
                    out var planetInfo))
            {
                Debug.Log("[Game] UpdateCurrentPlanetIdAsync()..." +
                          " planet id is found in planet registry.");
                CurrentPlanetId = planetInfo.ID;
            }
            else
            {
                Debug.LogWarning("[Game] UpdateCurrentPlanetIdAsync()..." +
                                 " planet id is not found in planet registry." +
                                 " Check CommandLineOptions.PlaneRegistryUrl and" +
                                 " CommandLineOptions.RpcServerHost.");
            }
        }
    }
}
