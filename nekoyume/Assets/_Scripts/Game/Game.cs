#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#define RUN_ON_MOBILE
#define ENABLE_FIREBASE
#endif
#if !UNITY_EDITOR && UNITY_STANDALONE
#define RUN_ON_STANDALONE
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using Nekoyume.Planet;
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

        protected override void Awake()
        {
            Debug.Log("[Game] Awake() invoked");
            GL.Clear(true, true, Color.black);
            Application.runInBackground = true;
#if UNITY_IOS && !UNITY_IOS_SIMULATOR && !UNITY_EDITOR
            // DevCra - iOS Build
            //string prefix = Path.Combine(Platform.DataPath.Replace("Data", ""), "Frameworks");
            ////Load dynamic library of rocksdb
            //string RocksdbLibPath = Path.Combine(prefix, "rocksdb.framework", "librocksdb");
            //Native.LoadLibrary(RocksdbLibPath);

            ////Set the path of secp256k1's dynamic library
            //string secp256k1LibPath = Path.Combine(prefix, "secp256k1.framework", "libsecp256k1");
            //Secp256k1Net.UnityPathHelper.SetSpecificPath(secp256k1LibPath);
#elif UNITY_IOS_SIMULATOR && !UNITY_EDITOR
            string rocksdbLibPath = Platform.GetStreamingAssetsPath("librocksdb.dylib");
            Native.LoadLibrary(rocksdbLibPath);

            string secp256LibPath = Platform.GetStreamingAssetsPath("libsecp256k1.dylib");
            Secp256k1Net.UnityPathHelper.SetSpecificPath(secp256LibPath);
#elif UNITY_ANDROID
            // string loadPath = Application.dataPath.Split("/base.apk")[0];
            // loadPath = Path.Combine(loadPath, "lib");
            // loadPath = Path.Combine(loadPath, Environment.Is64BitOperatingSystem ? "arm64" : "arm");
            // loadPath = Path.Combine(loadPath, "librocksdb.so");
            // Debug.LogWarning($"native load path = {loadPath}");
            // RocksDbSharp.Native.LoadLibrary(loadPath);
#endif
            Application.targetFrameRate = 60;
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

            // Initialize LiveAssetManager, Create RequestManager
            gameObject.AddComponent<RequestManager>();
            var liveAssetManager = gameObject.AddComponent<LiveAssetManager>();
            liveAssetManager.InitializeData();
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            yield return liveAssetManager.InitializeApplicationCLO();

            _commandLineOptions = liveAssetManager.CommandLineOptions;
            OnLoadCommandlineOptions();
#endif

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
                    : PrivateKey.FromString(_commandLineOptions.PrivateKey).ToAddress(),
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
            
            // NOTE: Apply l10n to IntroScreen after L10nManager initialized.
            Widget.Find<IntroScreen>().ApplyL10n();

            // Initialize MainCanvas first
            MainCanvas.instance.InitializeFirst();

            var grayLoadingScreen = Widget.Find<GrayLoadingScreen>();

            // Initialize TableSheets. This should be done before initialize the Agent.
            yield return StartCoroutine(CoInitializeTableSheets());
            Debug.Log("[Game] Start()... TableSheets initialized");
            ResourcesHelper.Initialize();
            Debug.Log("[Game] Start()... ResourcesHelper initialized");
            AudioController.instance.Initialize();
            Debug.Log("[Game] Start()... AudioController initialized");

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

            Analyzer.SetPlanetId(planetContext.SelectedPlanetInfo.ID.ToString());
#endif
            if (agentInitializeSucceed)
            {
                Analyzer.SetAgentAddress(Agent.Address.ToString());
                Analyzer.Instance.Track("Unity/Intro/Start/AgentInitialized");
            }
            else
            {
                Analyzer.Instance.Track("Unity/Intro/Start/AgentInitializeFailed");
                QuitWithAgentConnectionError(null);
                yield break;
            }

            yield return UpdateCurrentPlanetIdAsync(planetContext).ToCoroutine();
            Debug.Log($"[Game] Start()... CurrentPlanetId updated. {CurrentPlanetId?.ToString()}");

            // NOTE: Create ActionManager after Agent initialized.
            ActionManager = new ActionManager(Agent);

            var sw = new Stopwatch();
            sw.Reset();
            var createSecondWidgetCoroutine = StartCoroutine(MainCanvas.instance.CreateSecondWidgets());
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
            PortalConnect.CheckTokens(States.AgentState.address);

            if (planetContext.NeedToPledge.HasValue && planetContext.NeedToPledge.Value)
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

            StartCoroutine(CoUpdate());
            ReservePushNotifications();

            yield return new WaitForSeconds(GrayLoadingScreen.SliderAnimationDuration);
            IsInitialized = true;
            Widget.Find<IntroScreen>().Close();
            EnterNext();
            yield break;

            IEnumerator InitializeIAP()
            {
                grayLoadingScreen.ShowProgress(GameInitProgress.InitIAP);
                var innerSw = new Stopwatch();
                innerSw.Reset();
                innerSw.Start();
#if UNITY_ANDROID
                IAPServiceManager = new IAPServiceManager(_commandLineOptions.IAPServiceHost, Store.Google);
#elif UNITY_IOS
                IAPServiceManager = new IAPServiceManager(_commandLineOptions.IAPServiceHost, Store.Apple);
#endif
                yield return IAPServiceManager.InitializeAsync().AsCoroutine();
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
                Debug.Log($"[Game] Start()... TableSheets synced in {innerSw.ElapsedMilliseconds}ms.(elapsed)");
                Analyzer.Instance.Track("Unity/Intro/Start/TableSheetsInitialized");
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
                        var patronAddress = States.PatronAddress!.Value;
                        ActionManager.Instance.ApprovePledge(patronAddress).Subscribe();

                        yield return SetTimeOut(() => States.PledgeApproved);
                    }

                    swForRenderingApprovePledge.Stop();
                    Debug.Log("[Game] CoCheckPledge()... Rendering ApprovePledge" +
                              $" finished in {swForRenderingApprovePledge.ElapsedMilliseconds}ms.(elapsed)");
                    Analyzer.Instance.Track("Unity/Intro/Pledge/Approve");
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
                    popup.SubmitCallback = () => clickRetry = true;
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
            var container = await Resources
                .LoadAsync<AddressableAssetsContainer>(AddressableAssetsContainerPath)
                .ToUniTask();
            if (container is not AddressableAssetsContainer addressableAssetsContainer)
            {
                throw new FailedToLoadResourceException<AddressableAssetsContainer>(
                    AddressableAssetsContainerPath);
            }

            var csvAssets = addressableAssetsContainer.tableCsvAssets;
            var map = csvAssets.ToDictionary(
                asset => Addresses.TableSheet.Derive(asset.name),
                asset => asset.name);
            var dict = await Agent.GetStateBulkAsync(map.Keys);
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
                    States.Instance.AvatarStates.Any(x => x.Value.level > 49) &&
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

            var deletableWidgets = Widget.FindWidgets().Where(widget =>
                widget is not SystemWidget &&
                widget is not MessageCatTooltip &&
                widget.IsActive());
            foreach (var widget in deletableWidgets)
            {
                widget.Close(true);
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
                    Application.OpenURL(GameConfig.DiscordLink);
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
                        Application.OpenURL(GameConfig.DiscordLink);
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

            var settings = Widget.Find<SettingPopup>();
            settings.UpdateSoundSettings();
            settings.UpdatePrivateKey(_commandLineOptions.PrivateKey);

            var introScreen = Widget.Find<IntroScreen>();
            var loginSystem = Widget.Find<LoginSystem>();

            var sw = new Stopwatch();
            if (Application.isBatchMode)
            {
                loginSystem.Show(_commandLineOptions.KeyStorePath, _commandLineOptions.PrivateKey);
                sw.Start();
                yield return Agent.Initialize(
                    _commandLineOptions,
                    loginSystem.GetPrivateKey(),
                    callback);
                sw.Stop();
                planetContext.ElapsedTuples.Add((
                    "Unity_Elapsed_Initialize_Agent",
                    sw.ElapsedMilliseconds,
                    string.Empty));
                Debug.Log($"[Game] CoLogin()... Agent initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
                yield break;
            }

            // NOTE: planetContext is null when the game is launched from the non-mobile platform.
            if (planetContext is null)
            {
                Debug.Log("[Game] CoLogin()... planetContext is null.");
                if (loginSystem.CheckLocalPassphrase())
                {
                    Debug.Log("[Game] CoLogin()... CheckLocalPassphrase() is true.");
                }
                else
                {
                    Debug.Log("[Game] CoLogin()... CheckLocalPassphrase() is false.");
                    introScreen.Show(
                        _commandLineOptions.KeyStorePath,
                        _commandLineOptions.PrivateKey,
                        planetContext: null);
                }

                Debug.Log("[Game] CoLogin()... WaitUntil loginPopup.Login.");
                yield return new WaitUntil(() => loginSystem.Login);
                Debug.Log("[Game] CoLogin()... WaitUntil loginPopup.Login. Done.");

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

            // NOTE: Initialize current planet info.
            sw.Reset();
            sw.Start();
            planetContext = PlanetSelector.InitializeSelectedPlanetInfo(planetContext);
            sw.Stop();
            planetContext.ElapsedTuples.Add((
                "Unity_Elapsed_Select_Planet",
                sw.ElapsedMilliseconds,
                string.Empty));
            Debug.Log($"[Game] CoLogin()... PlanetInfo selected in {sw.ElapsedMilliseconds}ms.(elapsed)");
            if (planetContext.HasError)
            {
                callback?.Invoke(false);
                yield break;
            }

            // NOTE: Set the auto login flag to false. Player cannot login automatically on mobile platform.
            //       It should be set after the PlanetSelector.InitializeSelectedPlanetInfo().
            planetContext.NeedToAutoLogin = false;
            Debug.Log("[Game] CoLogin()... set planetContext.NeedToAutoLogin to false on mobile platform.");

            // NOTE: Check local passphrase.
            if (loginSystem.CheckLocalPassphrase())
            {
                Debug.Log("[Game] CoLogin()... CheckLocalPassphrase() is true.");
                if (!PlanetSelector.HasCachedPlanetIdString)
                {
                    Debug.Log("[Game] CoLogin()... HasCachedPlanetIdString is false." +
                              " Show planet selector.");
                    planetContext.NeedToAutoLogin = false;
                }

                // NOTE: Invoke the IntroScreen.SetDate() instead of IntroScreen.Show().
                //
                // NOTE: IntroScreen.SetDate() was invoked within the
                //       `!PlanetSelector.HasCachedPlanetIdString` condition. Because if there is
                //       a cached planet id string, we can expect the player already selected
                //       the planet and it means the player can login automatically.
                //       But now we don't login automatically on mobile platform. So we need to show the
                //       IntroScreen UI to the player.
                introScreen.SetData(
                    _commandLineOptions.KeyStorePath,
                    ByteUtil.Hex(loginSystem.GetPrivateKey().ByteArray),
                    planetContext);
            }
            else
            {
                Debug.Log("[Game] CoLogin()... CheckLocalPassphrase() is false.");
                planetContext.NeedToAutoLogin = false;
                // NOTE: We can expect the LoginSystem.Login updated to true if the
                //       IntroScreen.Show(pkPath, pk, planetContext) is called on non-mobile
                //       platform. Because the LoginSystem.Show(pkPath, pk) will be called
                //       inside the IntroScreen.Show(pkPath, pk, planetContext) on non-mobile
                //       platform.
                //
                // NOTE: If we need to cover the Multiplanetary context on non-mobile platform,
                //       we need to reconsider the invoking the IntroScreen.Show(pkPath, pk, planetContext)
                //       in here.
                //
                // NOTE: Invoke the IntroScreen.SetDate() instead of IntroScreen.Show().
                introScreen.SetData(
                    _commandLineOptions.KeyStorePath,
                    _commandLineOptions.PrivateKey,
                    planetContext);
            }

            // NOTE: Show IntroScreen's tab to start button.
            //       It should be called after the PlanetSelector.InitializeSelectedPlanetInfo().
            //       Because the IntroScreen uses the PlanetContext.SelectedPlanetInfo.
            //       And it should be called after the IntroScreen.SetData().
            introScreen.ShowTabToStart();
            Debug.Log("[Game] CoLogin()... WaitUntil introScreen.OnClickTabToStart.");
            yield return introScreen.OnClickTabToStart.AsObservable().First().StartAsCoroutine();
            Debug.Log("[Game] CoLogin()... WaitUntil introScreen.OnClickTabToStart. Done.");

            // NOTE: Check auto login!
            if (planetContext.NeedToAutoLogin.HasValue && planetContext.NeedToAutoLogin.Value)
            {
                var pk = loginSystem.GetPrivateKey();
                if (pk is not null)
                {
                    Debug.Log("[Game] CoLogin()... planetContext.NeedToAutoLogin is true." +
                              " And loginSystem.GetPrivateKey() is not null." +
                              " Try to auto login.");
                    sw.Reset();
                    sw.Start();
                    yield return Agent.Initialize(
                        _commandLineOptions,
                        pk,
                        callback);
                    sw.Stop();
                    planetContext.ElapsedTuples.Add((
                        "Unity_Elapsed_Initialize_Agent",
                        sw.ElapsedMilliseconds,
                        string.Empty));
                    Debug.Log($"[Game] CoLogin()... Agent initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
                    yield break;
                }

                // NOTE: Not expected to reach here.
                Debug.LogError("[Game] CoLogin()... planetContext.NeedToAutoLogin is true." +
                               " But loginSystem.GetPrivateKey() is null." +
                               " We don't quit here but show intro screen UI again.");
                introScreen.Show(
                    _commandLineOptions.KeyStorePath,
                    _commandLineOptions.PrivateKey,
                    planetContext);
            }

            var loadingScreen = Widget.Find<DimmedLoadingScreen>();
            string email = null;
            Address? agentAddr = null;
            // NOTE: Wait until social logged in if intro screen is active.
            if (introScreen.IsActive())
            {
                Debug.Log("[Game] CoLogin()... IntroScreen is active. Go to social login flow.");
                string idToken = null;
                bool isGoogle = false;
                introScreen.OnGoogleSignedIn.AsObservable()
                    .First()
                    .Subscribe(value => {
                        email = value.email;
                        idToken = value.idToken;
                        isGoogle = true;
                    });
                introScreen.OnAppleSignedIn.AsObservable()
                    .First()
                    .Subscribe(value => {
                        email = value.email;
                        idToken = value.idToken;
                        isGoogle = false;
                    });

                Debug.Log("[Game] CoLogin()... WaitUntil introScreen.OnGoogleSignedIn or introScreen.OnAppleSignedIn.");
                yield return new WaitUntil(() => idToken is not null);
                Debug.Log("[Game] CoLogin()... WaitUntil introScreen.OnGoogleSignedIn or introScreen.OnAppleSignedIn. Done.");

                loadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForPortalAuthenticating);

                Debug.Log("[Game] CoLogin()... WaitUntil PortalConnect.SendGoogleIdTokenAsync.");
                sw.Reset();
                sw.Start();
                var portalSigninTask = isGoogle ? PortalConnect.SendGoogleIdTokenAsync(idToken) : PortalConnect.SendAppleIdTokenAsync(idToken);
                yield return new WaitUntil(() => portalSigninTask.IsCompleted);
                sw.Stop();
                planetContext.ElapsedTuples.Add((
                    "Unity_Elapsed_Signin_Portal",
                    sw.ElapsedMilliseconds,
                    string.Empty));
                Debug.Log($"[Game] CoLogin()... Portal signed in in {sw.ElapsedMilliseconds}ms.(elapsed)");
                Debug.Log("[Game] CoLogin()... WaitUntil PortalConnect.SendGoogleIdTokenAsync. Done.");

                var agentAddress = portalSigninTask.Result;
                if (agentAddress is null)
                {
                    loginSystem.Show(connectedAddress: null);
                    // NOTE: Don't set the autoGeneratedAgentAddress to agentAddr.
                    var autoGeneratedAgentAddress = loginSystem.GetPrivateKey().ToAddress();
                    Debug.Log("[Game] CoLogin()... signinBehaviour.AgentAddress is null." +
                              $"And auto generated agent address: {autoGeneratedAgentAddress}");
                }
                else
                {
                    Debug.Log($"[Game] CoLogin()... signinBehaviour.AgentAddress is not null. {agentAddress.Value}");
                    agentAddr = agentAddress.Value;
                    // NOTE: Don't show login popup when google or apple signed in.
                    //       Because introScreen.ShowForQrCodeGuide() will be called
                    //       when IntroScreen.AgentInfo.accountImportKeyButton is clicked.
                    // loginSystem.Show(connectedAddress: agentAddr);
                }
            }
            else
            {
                Debug.Log("[Game] CoLogin()... IntroScreen is inactive.");
            }

            if (agentAddr.HasValue)
            {
                Debug.Log("[Game] CoLogin()... agentAddr.HasValue is true." +
                          " Try to update planet account infos.");
                loadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForPlanetAccountInfoSyncing);
                yield return PlanetSelector.UpdatePlanetAccountInfosAsync(
                    planetContext,
                    agentAddr.Value).ToCoroutine();
                if (planetContext.HasError)
                {
                    callback?.Invoke(false);
                    yield break;
                }
            }
            else
            {
                Debug.Log("[Game] CoLogin()... agentAddr.HasValue is false." +
                          " Try to update planet account infos w/ empty agent address.");
                // NOTE: Initialize planet account infos as default(empty) value
                //       when agent address is not set.
                planetContext.PlanetAccountInfos = planetContext.PlanetRegistry?.PlanetInfos
                    .Select(planetInfo => new PlanetAccountInfo(
                        planetInfo.ID,
                        agentAddress: null))
                    .ToArray();
            }

            if (loadingScreen.IsActive())
            {
                loadingScreen.Close();
            }

            // NOTE: Check if the planets have at least one agent.
            if (planetContext.PlanetAccountInfos!.Any(e => e.AgentAddress is not null))
            {
                Debug.Log("[Game] CoLogin()... account exists. Show planet account infos popup.");
                introScreen.ShowPlanetAccountInfosPopup(planetContext, !loginSystem.Login);

                Debug.Log("[Game] CoLogin()... WaitUntil planetContext.SelectedPlanetAccountInfo" +
                          " is not null.");
                yield return new WaitUntil(() => planetContext.SelectedPlanetAccountInfo is not null);
                Debug.Log("[Game] CoLogin()... WaitUntil planetContext.SelectedPlanetAccountInfo" +
                          $" is not null. Done. {planetContext.SelectedPlanetAccountInfo!.PlanetId}");

                var info = planetContext.SelectedPlanetAccountInfo!;
                if (info.AgentAddress is null)
                {
                    // NOTE: Player selected the planet that has no agent.
                    Debug.Log("[Game] CoLogin()... Try to create a new agent." +
                              " Player may have to make a pledge.");
                    planetContext.NeedToPledge = true;

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
                else
                {
                    // NOTE: Player selected the planet that has agent.
                    Debug.Log("[Game] CoLogin()... Try to import key w/ QR code." +
                              " Player don't have to make a pledge.");
                    planetContext.NeedToPledge = false;

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
            }
            else
            {
                Debug.Log("[Game] CoLogin()... account does not exist.");
                if (!loginSystem.Login)
                {
                    // NOTE: Complex logic here...
                    //       - Portal has agent address which connected with social account.
                    //       - No agent states in the all planets.
                    //       - LoginSystem.Login is false.
                    //
                    //       Client cannot know the agent private key of the portal's agent address.
                    //       So, we quit the application with kind message.
                    var msg = "Portal has agent address which connected with social account." +
                              " But no agent states in the all planets." +
                              $"\n Portal: {PortalConnect.PortalUrl}" +
                              $"\n Social Account: {email}" +
                              $"\n Agent Address: {agentAddr}";
                    Debug.LogError(msg);
                    planetContext.Error = msg;
                    callback?.Invoke(false);
                    yield break;
                }

                Debug.Log("[Game] CoLogin()... Player have to make a pledge.");
                planetContext.NeedToPledge = true;
                Debug.Log("[Game] CoLogin()... Set planetContext.SelectedPlanetAccountInfo" +
                          " w/ planetContext.SelectedPlanetInfo.ID.");
                planetContext.SelectedPlanetAccountInfo = planetContext.PlanetAccountInfos.First(e =>
                    e.PlanetId.Equals(planetContext.SelectedPlanetInfo!.ID));
            }

            Debug.Log("[Game] CoLogin()... WaitUntil loginPopup.Login.");
            yield return new WaitUntil(() => loginSystem.Login);
            Debug.Log("[Game] CoLogin()... WaitUntil loginPopup.Login. Done.");

            sw.Reset();
            sw.Start();
            yield return Agent.Initialize(
                _commandLineOptions,
                loginSystem.GetPrivateKey(),
                callback);
            sw.Stop();
            planetContext.ElapsedTuples.Add((
                "Unity_Elapsed_Initialize_Agent",
                sw.ElapsedMilliseconds,
                string.Empty));
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
                });
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

                var gameConfigState = States.Instance.GameConfigState;
                var targetBlockIndex = nextRoundData.StartBlockIndex +
                                       Mathf.RoundToInt(gameConfigState.DailyArenaInterval * 0.15f);
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

            var gameConfigState = States.Instance.GameConfigState;
            var targetBlockIndex = row.StartedBlockIndex +
                                   Mathf.RoundToInt(gameConfigState.DailyWorldBossInterval * 0.15f);
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
