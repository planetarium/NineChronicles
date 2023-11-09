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
using Nekoyume.Model.Mail;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using UnityEngine.Android;
#endif
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
            string prefix = Path.Combine(Platform.DataPath.Replace("Data", ""), "Frameworks");
            //Load dynamic library of rocksdb
            string RocksdbLibPath = Path.Combine(prefix, "rocksdb.framework", "librocksdb");
            Native.LoadLibrary(RocksdbLibPath);

            //Set the path of secp256k1's dynamic library
            string secp256k1LibPath = Path.Combine(prefix, "secp256k1.framework", "libsecp256k1");
            Secp256k1Net.UnityPathHelper.SetSpecificPath(secp256k1LibPath);
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

#if !UNITY_EDITOR && UNITY_ANDROID
            // Load CommandLineOptions at Start() after init
#elif !UNITY_EDITOR && UNITY_IOS
            _commandLineOptions = CommandLineOptions.Load(Platform.GetStreamingAssetsPath("clo.json"));
            OnLoadCommandlineOptions();
#else
            _commandLineOptions = CommandLineOptions.Load(CommandLineOptionsJsonPath);
            OnLoadCommandlineOptions();
#endif
            URL = Url.Load(UrlJsonPath);

#if UNITY_EDITOR && !UNITY_ANDROID
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
#if !UNITY_EDITOR && UNITY_ANDROID
            yield return liveAssetManager.InitializeApplicationCLO();

            _commandLineOptions = liveAssetManager.CommandLineOptions;
            OnLoadCommandlineOptions();
#endif

#if RUN_ON_MOBILE
            // NOTE: Initialize planets.
            //       It should do after load CommandLineOptions.
            //       And it should do before initialize Agent.
            var planetContext = new PlanetContext(_commandLineOptions);
            yield return PlanetSelector.InitializePlanetsAsync(planetContext).ToCoroutine();
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
            // ~Initialize planets

            // NOTE: Portal url does not change for each planet.
            PortalConnect = new PortalConnect(_commandLineOptions.MeadPledgePortalUrl);

#if ENABLE_FIREBASE
            // NOTE: Initialize Firebase.
            yield return FirebaseManager.InitializeAsync().ToCoroutine();
#endif
            // NOTE: Initialize Analyzer after load CommandLineOptions, initialize States,
            //       initialize Firebase Manager.
            InitializeAnalyzer(
                agentAddr: _commandLineOptions.PrivateKey is null
                    ? null
                    : PrivateKey.FromString(_commandLineOptions.PrivateKey).ToAddress(),
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
                .Initialize(LanguageTypeMapper.ISO639(_commandLineOptions.Language))
                .ToYieldInstruction();
#endif
            Debug.Log("[Game] Start()... L10nManager initialized");
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
                        Analyzer.SetAgentAddress(Agent.Address.ToString());
                        Analyzer.Instance.Track("Unity/Intro/Start/AgentInitialized");
                    }
                )
            );
            grayLoadingScreen.ShowProgress(GameInitProgress.ProgressStart);
            yield return new WaitUntil(() => agentInitialized);
            if (!agentInitializeSucceed)
            {
                QuitWithAgentConnectionError(null);
                yield break;
            }

#if RUN_ON_MOBILE
            if (planetContext.SelectedPlanetInfo is null)
            {
                QuitWithMessage("planetContext.CurrentPlanetInfo is null in mobile.");
                yield break;
            }
#endif

            // NOTE: Create ActionManager after Agent initialized.
            ActionManager = new ActionManager(Agent);

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

#if UNITY_EDITOR || RUN_ON_MOBILE
            StartCoroutine(InitializeIAP());
#endif

            yield return StartCoroutine(InitializeWithAgent());
            Analyzer.Instance.Track("Unity/Intro/Start/TableSheetsInitialized");

            var initializeSecondWidgetsCoroutine = StartCoroutine(CoInitializeSecondWidget());

#if RUN_ON_MOBILE
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
            Stage.Initialize();
            Arena.Initialize();
            RaidStage.Initialize();
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

#if RUN_ON_MOBILE
            IEnumerator InitializeIAP()
            {
                grayLoadingScreen.ShowProgress(GameInitProgress.InitIAP);
                IAPServiceManager = new IAPServiceManager(_commandLineOptions.IAPServiceHost, Store.Google);
                yield return IAPServiceManager.InitializeAsync().AsCoroutine();
                IAPStoreManager = gameObject.AddComponent<IAPStoreManager>();
                Debug.Log("[Game] Start() IAPStoreManager initialize start");
            }
#endif

            IEnumerator InitializeWithAgent()
            {
                grayLoadingScreen.ShowProgress(GameInitProgress.InitTableSheet);
                yield return SyncTableSheetsAsync().ToCoroutine();
                Debug.Log("[Game] Start() TableSheets synchronized");
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
                yield return StartCoroutine(MainCanvas.instance.CreateSecondWidgets());
                yield return StartCoroutine(MainCanvas.instance.InitializeSecondWidgets());
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

                    while (!States.PledgeRequested)
                    {
                        Analyzer.Instance.Track("Unity/Intro/Pledge/Request");
                        yield return PortalConnect.RequestPledge(
                            planetId,
                            States.AgentState.address);

                        yield return SetTimeOut(() => States.PledgeRequested);
                        Analyzer.Instance.Track("Unity/Intro/Pledge/Requested");
                    }
                }

                if (States.PledgeRequested && !States.PledgeApproved)
                {
                    Widget.Find<GrayLoadingScreen>().ShowProgress(GameInitProgress.ApprovePledge);

                    while (!States.PledgeApproved)
                    {
                        Analyzer.Instance.Track("Unity/Intro/Pledge/ApproveAction");
                        var patronAddress = States.PatronAddress!.Value;
                        ActionManager.Instance.ApprovePledge(patronAddress).Subscribe();

                        yield return SetTimeOut(() => States.PledgeApproved);
                    }

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
                    await RxProps.SelectAvatarAsync(slotIndex);
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
                        await RxProps.SelectAvatarAsync(slotIndex);
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

            if (Application.isBatchMode)
            {
                loginSystem.Show(_commandLineOptions.KeyStorePath, _commandLineOptions.PrivateKey);
                yield return Agent.Initialize(
                    _commandLineOptions,
                    loginSystem.GetPrivateKey(),
                    callback);
                yield break;
            }

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

                yield return Agent.Initialize(
                    _commandLineOptions,
                    loginSystem.GetPrivateKey(),
                    callback);
                yield break;
            }

            // NOTE: Initialize current planet info.
            planetContext = PlanetSelector.InitializeSelectedPlanetInfo(
                planetContext,
                resetIfCachedPlanetNotFoundInPlanets: true);
            if (planetContext.HasError)
            {
                QuitWithMessage(
                    L10nManager.Localize("ERROR_INITIALIZE_FAILED"),
                    planetContext.Error);
                yield break;
            }

            // NOTE: Check local passphrase.
            if (loginSystem.CheckLocalPassphrase())
            {
                Debug.Log("[Game] CoLogin()... CheckLocalPassphrase() is true.");
                if (!PlanetSelector.HasCachedPlanetIdString)
                {
                    Debug.Log("[Game] CoLogin()... HasSelectedPlanetBefore is false." +
                              " Show planet selector.");
                    planetContext.NeedToAutoLogin = false;
                    introScreen.Show(
                        _commandLineOptions.KeyStorePath,
                        ByteUtil.Hex(loginSystem.GetPrivateKey().ByteArray),
                        planetContext);
                }
            }
            else
            {
                Debug.Log("[Game] CoLogin()... CheckLocalPassphrase() is false.");
                planetContext.NeedToAutoLogin = false;
                introScreen.Show(
                    _commandLineOptions.KeyStorePath,
                    _commandLineOptions.PrivateKey,
                    planetContext);
            }

            // NOTE: Check auto login!
            if (planetContext.NeedToAutoLogin.HasValue && planetContext.NeedToAutoLogin.Value)
            {
                var pk = loginSystem.GetPrivateKey();
                if (pk is not null)
                {
                    Debug.Log("[Game] CoLogin()... planetContext.NeedToAutoLogin is true." +
                              " And loginSystem.GetPrivateKey() is not null." +
                              " Try to auto login.");
                    yield return Agent.Initialize(
                        _commandLineOptions,
                        pk,
                        callback);
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
            Address? agentAddr = null;
            // NOTE: Wait until social logged in if intro screen is active.
            if (introScreen.IsActive())
            {
                introScreen.OnClickGoogleSignIn.AsObservable()
                    .First()
                    .Subscribe(_ => loadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForSocialAuthenticating));

                (IntroScreen introScreen, GoogleSigninBehaviour googleSigninBehaviour)?
                    onGoogleSignInTuple = null;
                introScreen.OnGoogleSignedIn.AsObservable()
                    .First()
                    .Subscribe(tuple => onGoogleSignInTuple = tuple);

                Debug.Log("[Game] CoLogin()... WaitUntil introScreen.OnGoogleSignedIn.");
                yield return new WaitUntil(() => onGoogleSignInTuple.HasValue);
                Debug.Log("[Game] CoLogin()... WaitUntil introScreen.OnGoogleSignedIn. Done.");

                var (_, googleSigninBehaviour) = onGoogleSignInTuple!.Value;

                Debug.Log("[Game] CoLogin()... WaitUntil googleSigninBehaviour.CoSendGoogleIdToken.");
                loadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForPortalAuthenticating);
                yield return StartCoroutine(googleSigninBehaviour.CoSendGoogleIdToken());
                Debug.Log("[Game] CoLogin()... WaitUntil googleSigninBehaviour.CoSendGoogleIdToken. Done.");

                if (googleSigninBehaviour.AgentAddress is null)
                {
                    loginSystem.Show(connectedAddress: null);
                    Debug.Log("[Game] CoLogin()... googleSigninBehaviour.AgentAddress is null." +
                              $"auto generated agent address: {loginSystem.GetPrivateKey().ToAddress()}");
                }
                else
                {
                    Debug.Log("[Game] CoLogin()... googleSigninBehaviour.AgentAddress is not null." +
                              $" {googleSigninBehaviour.AgentAddress.Value}");
                    agentAddr = googleSigninBehaviour.AgentAddress.Value;
                    // NOTE: Don't show login popup when google signed in.
                    //       Because introScreen.ShowForQrCodeGuide() will be called
                    //       when IntroScreen.AgentInfo.accountImportKeyButton is clicked.
                    // loginSystem.Show(connectedAddress: agentAddr);
                }
            }

            if (agentAddr.HasValue)
            {
                loadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForPlanetAccountInfoSyncing);
                yield return PlanetSelector.UpdatePlanetAccountInfosAsync(
                    planetContext,
                    agentAddr.Value).ToCoroutine();
                if (planetContext.HasError)
                {
                    QuitWithMessage(
                        L10nManager.Localize("ERROR_INITIALIZE_FAILED"),
                        planetContext.Error);
                    yield break;
                }
            }
            else
            {
                // NOTE: Initialize planet account infos as default(empty) value
                //       when agent address is not set.
                planetContext.PlanetAccountInfos = planetContext.Planets?.PlanetInfos
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
                introScreen.ShowPlanetAccountInfosPopup(planetContext);

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

                    // NOTE: QR code import sets loginSystem.Login to true.
                    introScreen.ShowForQrCodeGuide();
                }
            }
            else
            {
                Debug.Log("[Game] CoLogin()... account does not exist." +
                          " Player have to make a pledge.");
                planetContext.NeedToPledge = true;
                Debug.Log("[Game] CoLogin()... Set planetContext.SelectedPlanetAccountInfo" +
                          " w/ planetContext.SelectedPlanetInfo.ID.");
                planetContext.SelectedPlanetAccountInfo = planetContext.PlanetAccountInfos.First(e =>
                    e.PlanetId.Equals(planetContext.SelectedPlanetInfo!.ID));
            }

            Debug.Log("[Game] CoLogin()... WaitUntil loginPopup.Login.");
            yield return new WaitUntil(() => loginSystem.Login);
            Debug.Log("[Game] CoLogin()... WaitUntil loginPopup.Login. Done.");

            yield return Agent.Initialize(
                _commandLineOptions,
                loginSystem.GetPrivateKey(),
                callback);
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
            return RequestManager.instance.GetJson(
                URL.DccAvatars,
                URL.DccEthChainHeaderName,
                URL.DccEthChainHeaderValue,
                json =>
                {
                    var responseData = DccAvatars.FromJson(json);
                    Dcc.instance.Init(responseData.Avatars);
                });
        }

        private IEnumerator CoInitDccConnecting()
        {
            return RequestManager.instance.GetJson(
                $"{URL.DccMileageAPI}{Agent.Address}",
                URL.DccEthChainHeaderName,
                URL.DccEthChainHeaderValue,
                _ => { Dcc.instance.IsConnected = true; },
                _ => { Dcc.instance.IsConnected = false; });
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
            string rpcServerHost = null)
        {
            Debug.Log("[Game] InitializeAnalyzer() invoked");
            var uniqueId = agentAddr?.ToString();
#if UNITY_EDITOR
            Debug.Log("[Game] InitializeAnalyzer()... Analyze is disabled in editor mode.");
            Analyzer = new Analyzer(uniqueId, rpcServerHost, isTrackable: false);
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
                uniqueId,
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
    }
}
