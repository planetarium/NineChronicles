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
using System.Threading;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Lib9c.Formatters;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using LruCacheNet;
using MessagePack;
using MessagePack.Resolvers;
using Nekoyume.Action;
using Nekoyume.ApiClient;
using Nekoyume.Blockchain;
using Nekoyume.Extensions;
using Nekoyume.Game.Battle;
using Nekoyume.Multiplanetary;
using Nekoyume.Game.Controller;
using Nekoyume.Game.LiveAsset;
using Nekoyume.Game.Scene;
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
using UnityEngine;
using UnityEngine.Playables;
using Currency = Libplanet.Types.Assets.Currency;
using Random = UnityEngine.Random;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using Nekoyume.Model.Mail;
using Nekoyume.Module.Guild;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
#if ENABLE_FIREBASE
using NineChronicles.GoogleServices.Firebase.Runtime;
#endif

namespace Nekoyume.Game
{
    using Arena;
    using GeneratedApiNamespace.ArenaServiceClient;
    using Nekoyume.Model.EnumType;
    using TableData;
    using UniRx;

    [RequireComponent(typeof(Agent), typeof(RPCAgent))]
    public class Game : MonoSingleton<Game>
    {
        public const float DefaultTimeScale = 1.25f;

        public const float DefaultSkillDelay = 0.6f;

        [SerializeField]
        private Stage stage;

        [SerializeField]
        private Battle.Arena arena;

        [SerializeField]
        private RaidStage raidStage;

        [SerializeField]
        private Lobby lobby;

        [SerializeField]
        private bool useSystemLanguage = true;

        [SerializeField]
        private bool useLocalHeadless;

        [field: SerializeField]
        public bool useLocalMarketService;

        [SerializeField]
        private string marketDbConnectionString =
            "Host=localhost;Username=postgres;Database=market";

        [SerializeField]
        private LanguageTypeReactiveProperty languageType;

        [SerializeField]
        private Prologue prologue;

        [SerializeField]
        private GameObject debugConsolePrefab;

        public PlanetId? CurrentPlanetId
        {
            get => _currentPlanetId;
            set
            {
                NcDebug.Log($"[{nameof(Game)}] Set CurrentPlanetId: {value}");
                _currentPlanetId = value;
                LiveAssetManager.instance.SetThorSchedule(value);
            }
        }

        public States States { get; private set; }

        public LocalLayer LocalLayer { get; private set; }

        public LocalLayerActions LocalLayerActions { get; private set; }

        public IAgent Agent { get; private set; }

        public Analyzer Analyzer { get; private set; }

        public IAPStoreManager IAPStoreManager { get; private set; }

        public AdventureBossData AdventureBossData { get; private set; }

        public Stage Stage => stage;
        public Battle.Arena Arena => arena;
        public RaidStage RaidStage => raidStage;
        public Lobby Lobby => lobby;

        // FIXME Action.PatchTableSheet.Execute()에 의해서만 갱신됩니다.
        // 액션 실행 여부와 상관 없이 최신 상태를 반영하게끔 수정해야합니다.
        public TableSheets TableSheets { get; private set; }

        public ActionManager ActionManager { get; private set; }

        public bool IsInitialized { get; set; }

        public int? SavedPetId { get; set; }

        public Prologue Prologue => prologue;

        public const string AddressableAssetsContainerPath = nameof(AddressableAssetsContainer);

        public PortalConnect PortalConnect { get; private set; }

        public readonly LruCache<Address, IValue> CachedStates = new();

        public readonly Dictionary<Address, bool> CachedStateAddresses = new();

        public readonly Dictionary<Currency, LruCache<Address, FungibleAssetValue>>
            CachedBalance = new();

        public string CurrentSocialEmail { get; set; }

        public bool IsGuestLogin { get; set; }

        private CommandLineOptions _commandLineOptions;

        public CommandLineOptions CommandLineOptions => _commandLineOptions;

        private PlayableDirector _activeDirector;

        private string _msg;

        public static readonly string CommandLineOptionsJsonPath =
            Platform.GetStreamingAssetsPath("clo.json");

        private Thread _headlessThread;
        private Thread _marketThread;

        private const string ArenaSeasonPushIdentifierKey = "ARENA_SEASON_PUSH_IDENTIFIER";
        private const string ArenaTicketPushIdentifierKey = "ARENA_TICKET_PUSH_IDENTIFIER";
        private const string WorldbossSeasonPushIdentifierKey = "WORLDBOSS_SEASON_PUSH_IDENTIFIER";
        private const string WorldbossTicketPushIdentifierKey = "WORLDBOSS_TICKET_PUSH_IDENTIFIER";
        private const int TicketPushBlockCountThreshold = 300;

        private PlanetId? _currentPlanetId;

#region Mono & Initialization

#if !UNITY_EDITOR && UNITY_IOS
        void OnAuthorizationStatusReceived(AppTrackingTransparency.AuthorizationStatus status)
        {
            AppTrackingTransparency.OnAuthorizationStatusReceived -= OnAuthorizationStatusReceived;
        }
#endif

        protected override void Awake()
        {
            PreAwake();

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

#if RUN_ON_MOBILE
            // Load CommandLineOptions at Start() after init
#else
            _commandLineOptions = CommandLineOptions.Load(CommandLineOptionsJsonPath);
            OnLoadCommandlineOptions();
#endif
            ApiClients.Instance.SetDccUrl();

            CreateAgent();
            PostAwake();
        }

        public void AddRequestManager()
        {
            gameObject.AddComponent<RequestManager>();
        }

        /// <summary>
        /// Invoke On RUN_ON_MOBILE
        /// </summary>
        [UsedImplicitly]
        public void SetTargetFrameRate()
        {
            Application.targetFrameRate = 30;
        }

        public IEnumerator InitializeLiveAssetManager()
        {
            var liveAssetManager = LiveAssetManager.instance;
            liveAssetManager.InitializeData();
#if RUN_ON_MOBILE
            yield return liveAssetManager.InitializeApplicationCLO();

            _commandLineOptions = liveAssetManager.CommandLineOptions;
            OnLoadCommandlineOptions();
#endif
            yield break;
        }

        public void SetPortalConnect()
        {
            // NOTE: Portal url does not change for each planet.
            PortalConnect = new PortalConnect(_commandLineOptions.MeadPledgePortalUrl);
        }

        public void SetActionManager()
        {
            ActionManager = new ActionManager(Agent);
        }

        private void Start()
        {
#if RUN_ON_MOBILE
            SetTargetFrameRate();
#endif
            NcSceneManager.Instance.LoadScene(SceneType.Login).Forget();
        }

        /// <summary>
        /// Invoke On Window Editor
        /// </summary>
        [UsedImplicitly]
        public void UseMarketService()
        {
#if UNITY_EDITOR_WIN
            // wait for headless connect.
            if (!useLocalMarketService || !MarketHelper.CheckPath())
            {
                return;
            }

            _marketThread = new Thread(() => MarketHelper.RunLocalMarketService(marketDbConnectionString));
            _marketThread.Start();
#endif
        }

        public IEnumerator InitializeIAP()
        {
            var grayLoadingScreen = Widget.Find<GrayLoadingScreen>();
            grayLoadingScreen.ShowProgress(GameInitProgress.InitIAP);
            var innerSw = new Stopwatch();
            innerSw.Reset();
            innerSw.Start();

            yield return ApiClients.Instance.IAPServiceManager.InitializeAsync().AsCoroutine();

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

                    await Helper.Util.DownloadTexture($"{MobileShop.MOBILE_L10N_SCHEMA.Host}/{category.Path}");

                    foreach (var product in category.ProductList)
                    {
                        await Helper.Util.DownloadTexture($"{MobileShop.MOBILE_L10N_SCHEMA.Host}/{product.BgPath}");
                        await Helper.Util.DownloadTexture($"{MobileShop.MOBILE_L10N_SCHEMA.Host}/{product.GetListImagePath()}");
                        await Helper.Util.DownloadTexture($"{MobileShop.MOBILE_L10N_SCHEMA.Host}/{product.GetDetailImagePath()}");
                    }
                }
            });

            innerSw.Stop();
            NcDebug.Log("[Game] Start()... IAPServiceManager initialized in" +
                $" {innerSw.ElapsedMilliseconds}ms.(elapsed)");
            IAPStoreManager = gameObject.AddComponent<IAPStoreManager>();
        }

        public IEnumerator InitializeWithAgent()
        {
            var grayLoadingScreen = Widget.Find<GrayLoadingScreen>();
            grayLoadingScreen.ShowProgress(GameInitProgress.InitTableSheet);
            var innerSw = new Stopwatch();
            innerSw.Reset();
            innerSw.Start();
            yield return SyncTableSheetsAsync().ToCoroutine();
            innerSw.Stop();
            NcDebug.Log($"[Game/SyncTableSheets] Start()... TableSheets synced in {innerSw.ElapsedMilliseconds}ms.(elapsed)");
            Analyzer.Instance.Track("Unity/Intro/Start/TableSheetsInitialized");

            RxProps.Start(Agent, States, TableSheets);

            AdventureBossData = new AdventureBossData();
            AdventureBossData.Initialize();

            Event.OnUpdateAddresses.AsObservable().Subscribe(_ =>
            {
                var petList = States.Instance.PetStates.GetPetStatesAll()
                    .Where(petState => petState != null)
                    .Select(petState => petState.PetId)
                    .ToList();
                SavedPetId = !petList.Any() ? null : petList[Random.Range(0, petList.Count)];
            }).AddTo(gameObject);

            yield return InitializeStakeStateAsync().ToCoroutine();
        }

        public IEnumerator CoInitializeSecondWidget()
        {
            var grayLoadingScreen = Widget.Find<GrayLoadingScreen>();
            grayLoadingScreen.ShowProgress(GameInitProgress.InitCanvas);
            var innerSw = new Stopwatch();
            innerSw.Reset();
            innerSw.Start();
            yield return StartCoroutine(MainCanvas.instance.InitializeSecondWidgets());
            innerSw.Stop();
            NcDebug.Log($"[Game] Start()... SecondWidgets initialized in {innerSw.ElapsedMilliseconds}ms.(elapsed)");
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
                NcDebug.LogError(message);
                QuitWithMessage(
                    L10nManager.Localize("ERROR_INITIALIZE_FAILED"),
                    message);
                return;
            }

            if (debugConsolePrefab != null && _commandLineOptions.IngameDebugConsole)
            {
                NcDebug.Log("[Game] InGameDebugConsole enabled");
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
                        NcDebug.LogError(message);
                        QuitWithMessage(
                            L10nManager.Localize("ERROR_INITIALIZE_FAILED"),
                            message);
                        return;
                    }

                    _commandLineOptions.RpcServerHost = _commandLineOptions.RpcServerHosts
                        .OrderBy(_ => Guid.NewGuid())
                        .First();
                }
            }

            var introScreen = FindObjectOfType<IntroScreen>();
            if (introScreen != null)
            {
                introScreen.GetGuestPrivateKey();
            }

            NcDebug.Log("[Game] CommandLineOptions loaded");
            NcDebug.Log($"APV: {_commandLineOptions.AppProtocolVersion}");
            NcDebug.Log($"RPC: {_commandLineOptions.RpcServerHost}:{_commandLineOptions.RpcServerPort}");
            NcDebug.Log($"SelectedPlanetId: {_commandLineOptions.SelectedPlanetId}");
            NcDebug.Log($"DefaultPlanetId: {_commandLineOptions.DefaultPlanetId}");
        }

#region RPCAgent

        private void SubscribeRPCAgent()
        {
            if (Agent is not RPCAgent rpcAgent)
            {
                return;
            }

            NcDebug.Log("[Game]Subscribe RPCAgent");

            rpcAgent.OnRetryStarted
                .ObserveOnMainThread()
                .Subscribe(agent =>
                {
                    NcDebug.Log($"[Game]RPCAgent OnRetryStarted. {rpcAgent.Address.ToHex()}");
                    OnRPCAgentRetryStarted(agent);
                })
                .AddTo(gameObject);

            rpcAgent.OnRetryEnded
                .ObserveOnMainThread()
                .Subscribe(agent =>
                {
                    NcDebug.Log($"[Game]RPCAgent OnRetryEnded. {rpcAgent.Address.ToHex()}");
                    OnRPCAgentRetryEnded(agent);
                })
                .AddTo(gameObject);

            rpcAgent.OnPreloadStarted
                .ObserveOnMainThread()
                .Subscribe(agent =>
                {
                    NcDebug.Log($"[Game]RPCAgent OnPreloadStarted. {rpcAgent.Address.ToHex()}");
                    OnRPCAgentPreloadStarted(agent);
                })
                .AddTo(gameObject);

            rpcAgent.OnPreloadEnded
                .ObserveOnMainThread()
                .Subscribe(agent =>
                {
                    NcDebug.Log($"[Game]RPCAgent OnPreloadEnded. {rpcAgent.Address.ToHex()}");
                    OnRPCAgentPreloadEnded(agent);
                })
                .AddTo(gameObject);

            rpcAgent.OnDisconnected
                .ObserveOnMainThread()
                .Subscribe(agent =>
                {
                    NcDebug.Log($"[Game]RPCAgent OnDisconnected. {rpcAgent.Address.ToHex()}");
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
            var widget = Widget.Find<DimmedLoadingScreen>();
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
            // TODO: 현재 씬이 LoginScene인 경우로 수정
            if (LoginScene.IsOnIntroScene)
            {
                // NOTE: 타이틀 화면에서 리트라이와 프리로드가 완료된 상황입니다.
                // FIXME: 이 경우에는 메인 로비가 아니라 기존 초기화 로직이 흐르도록 처리해야 합니다.
                return;
            }

            var needToBackToMain = false;
            var showLoadingScreen = false;
            Widget widget = Widget.Find<DimmedLoadingScreen>();
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

                widget = Widget.Find<LobbyMenu>();
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

#endregion

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
                popup.SetConfirmCallbackToExit(true);

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
                NcDebug.Log(
                    $"{nameof(QuitWithAgentConnectionError)}() called. But {nameof(RPCAgent)}.Connected is {rpcAgent.Connected}.");
                return;
            }

            popup = Widget.Find<IconAndButtonSystem>();
            popup.Show("UI_ERROR", "UI_ERROR_RPC_CONNECTION", "UI_QUIT");
            popup.SetConfirmCallbackToExit(true);
        }

        /// <summary>
        /// This method must be called after <see cref="Game.Agent"/> initialized.
        /// </summary>
        public IEnumerator CoCheckPledge(PlanetId planetId)
        {
            NcDebug.Log("[Game] CoCheckPledge() invoked.");
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
                        NcDebug.Log("[Game] CoCheckPledge()... PortalConnect.RequestPledge()" +
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
                            NcDebug.Log("[Game] CoCheckPledge()... Rendering RequestPledge" +
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
                    NcDebug.Log("[Game] CoCheckPledge()... Rendering ApprovePledge" +
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
                for (var second = 0; second < timeLimit; second++)
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

                        if (await Agent.GetStateAsync(
                                ReservedAddresses.LegacyAccount,
                                pledgeAddress) is List list)
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
            NcDebug.Log($"[{nameof(SyncTableSheetsAsync)}] load container: {sw.Elapsed}");
            sw.Restart();

            var csvAssets = addressableAssetsContainer.tableCsvAssets;
            IDictionary<string, string> csvDict;
            // TODO delete GetSheetsAsync backward compatibility
            if (string.IsNullOrEmpty(_commandLineOptions.SheetBucketUrl))
            {
                var map = csvAssets.ToDictionary(
                    asset => Addresses.TableSheet.Derive(asset.name),
                    asset => asset.name);

                var dict = await Agent.GetSheetsAsync(map.Keys);
                sw.Stop();

                NcDebug.Log($"[{nameof(SyncTableSheetsAsync)}] get state: {sw.Elapsed}");

                sw.Restart();

                // Convert dict to csv using mapping, ensuring Text values are converted to strings
                csvDict = dict.ToDictionary(
                    pair => map[pair.Key],
                    // NOTE: `pair.Value` is `null` when the chain not contains the `pair.Key`.
                    pair => pair.Value is Text ? pair.Value.ToDotnetString() : null);
            }
            else
            {
                // Prepare list of sheet names from csvAssets
                var sheetNames = csvAssets.Select(x => x.name).ToList();

                var planetId = CurrentPlanetId!.Value;

                // Download and save sheets for the current planet
                csvDict = await DownloadSheet(planetId, _commandLineOptions.SheetBucketUrl, sheetNames);

                NcDebug.Log($"[{nameof(SyncTableSheetsAsync)}] download sheet: {sw.Elapsed}");

                sw.Stop();

                sw.Restart();
            }
            TableSheets = await TableSheets.MakeTableSheetsAsync(csvDict);
            sw.Stop();
            NcDebug.Log($"[{nameof(SyncTableSheetsAsync)}] TableSheets Constructor: {sw.Elapsed}");
        }

        private async UniTask InitializeStakeStateAsync()
        {
            // NOTE: Initialize staking states after setting GameConfigState.
            var stakeAddr = Model.Stake.StakeState.DeriveAddress(Agent.Address);
            var stakeStateIValue = await Agent.GetStateAsync(ReservedAddresses.LegacyAccount, stakeAddr);
            var balance = await Agent.GetStakedByStateRootHashAsync(Agent.BlockTipStateRootHash,
                States.Instance.AgentState.address);
            StakeRegularFixedRewardSheet stakeRegularFixedRewardSheet;
            StakeRegularRewardSheet stakeRegularRewardSheet;
            Model.Stake.StakeState? stakeState = null;
            List<string> sheetNames;
            if (!StakeStateUtilsForClient.TryMigrate(
                stakeStateIValue,
                States.Instance.GameConfigState,
                out var stakeStateV2))
            {
                sheetNames = new List<string>
                {
                    TableSheets.StakePolicySheet.StakeRegularFixedRewardSheetValue,
                    TableSheets.StakePolicySheet.StakeRegularRewardSheetValue,
                };
            }
            else
            {
                sheetNames = new List<string>
                {
                    stakeStateV2.Contract.StakeRegularFixedRewardSheetTableName,
                    stakeStateV2.Contract.StakeRegularRewardSheetTableName,
                };
                stakeState = stakeStateV2;
            }

            if (Agent is RPCAgent)
            {
                stakeRegularFixedRewardSheet = new StakeRegularFixedRewardSheet();
                stakeRegularRewardSheet = new StakeRegularRewardSheet();

                IDictionary<string, string> sheets;
                if (string.IsNullOrEmpty(CommandLineOptions.SheetBucketUrl))
                {
                    var map = sheetNames.ToDictionary(i => Addresses.TableSheet.Derive(i), i => i);
                    var dict = await Agent.GetSheetsAsync(map.Keys);
                    sheets = dict.ToDictionary(
                        pair => map[pair.Key],
                        // NOTE: `pair.Value` is `null` when the chain not contains the `pair.Key`.
                        pair => pair.Value is Text ? pair.Value.ToDotnetString() : null);
                }
                else
                {
                    sheets = await DownloadSheet(CurrentPlanetId!.Value, CommandLineOptions.SheetBucketUrl, sheetNames);
                }
                stakeRegularFixedRewardSheet.Set(sheets[sheetNames[0]]);
                stakeRegularRewardSheet.Set(sheets[sheetNames[1]]);
            }
            else
            {
                // It is local play. local genesis block not has Stake***Sheet_V*.
                // 로컬에서 제네시스 블록을 직접 생성하는 경우엔 스테이킹 보상-V* 시트가 없기 때문에, 오리지널 시트로 대체합니다.
                stakeRegularFixedRewardSheet = TableSheets.StakeRegularFixedRewardSheet;
                stakeRegularRewardSheet = TableSheets.StakeRegularRewardSheet;

            }
            var level = balance.RawValue > 0
                ? stakeRegularFixedRewardSheet.FindLevelByStakedAmount(
                    Agent.Address,
                    balance)
                : 0;
            States.Instance.SetStakeState(
                stakeState,
                balance,
                level,
                stakeRegularFixedRewardSheet,
                stakeRegularRewardSheet);
        }

        public static IDictionary<string, string> GetTableCsvAssets()
        {
            var container =
                Resources.Load<AddressableAssetsContainer>(AddressableAssetsContainerPath);
            return container.tableCsvAssets.ToDictionary(asset => asset.name, asset => asset.text);
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
        }

        public IEnumerator CoUpdate()
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
            await UniTask.SwitchToMainThread();
            NcDebug.LogException(exc);
            var (key, code, errorMsg) = await ErrorCode.GetErrorCodeAsync(exc);
            Lobby.Enter(showLoadingScreen);
            instance.Lobby.OnLobbyEnterEnd
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

            if (BattleRenderer.Instance.IsOnBattle)
            {
                NotificationSystem.Push(
                    MailType.System,
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
            NcDebug.LogException(exc);
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
            vfx?.Play();
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
                ApplicationQuit();
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

                ApplicationQuit();
            };

            confirm.Show("UI_CONFIRM_RESET_KEYSTORE_TITLE", "UI_CONFIRM_RESET_KEYSTORE_CONTENT");
        }

        public IEnumerator CoInitDccAvatar()
        {
            var dccUrl = ApiClients.Instance.DccURL;
            var sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            yield return RequestManager.instance.GetJson(
                dccUrl.DccAvatars,
                dccUrl.DccEthChainHeaderName,
                dccUrl.DccEthChainHeaderValue,
                json =>
                {
                    var responseData = DccAvatars.FromJson(json);
                    Dcc.instance.Init(responseData.Avatars);
                },
                timeOut: Dcc.TimeOut);
            sw.Stop();
            NcDebug.Log($"[Game] CoInitDccAvatar()... DCC Avatar initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
        }

        public IEnumerator CoInitDccConnecting()
        {
            var dccUrl = ApiClients.Instance.DccURL;
            var sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            yield return RequestManager.instance.GetJson(
                $"{dccUrl.DccMileageAPI}{Agent.Address}",
                dccUrl.DccEthChainHeaderName,
                dccUrl.DccEthChainHeaderValue,
                _ => { Dcc.instance.IsConnected = true; },
                _ => { Dcc.instance.IsConnected = false; });
            sw.Stop();
            NcDebug.Log("[Game] CoInitDccConnecting()... DCC Connecting initialized in" +
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

        public void ReservePushNotifications()
        {
            var currentBlockIndex = Agent.BlockIndex;
            ReserveWorldbossSeasonPush(currentBlockIndex);
            ReserveWorldbossTicketPush(currentBlockIndex);

            ReserveArena(currentBlockIndex).Forget();
        }

        public async UniTask ReserveArena(long blockIndex)
        {
            try
            {
                SeasonResponse seasonResponse = null;
                SeasonResponse nextSeasonResponse = null;
                RoundResponse roundResponse = null;
                await ApiClients.Instance.Arenaservicemanager.Client.GetSeasonsClassifybychampionshipAsync(blockIndex,
                    on200: response =>
                    {
                        seasonResponse = response.Seasons.Find(item =>
                            item.StartBlockIndex <= blockIndex && item.EndBlockIndex >= blockIndex
                        );

                        nextSeasonResponse = response.Seasons
                            .Where(seasonResponse => seasonResponse.StartBlockIndex >= blockIndex)
                            .OrderBy(seasonResponse => seasonResponse.StartBlockIndex)
                            .FirstOrDefault();

                        if (seasonResponse != null)
                        {
                            seasonResponse.Rounds.Find(item =>
                                item.StartBlockIndex <= blockIndex && item.EndBlockIndex >= blockIndex
                            );
                        }
                    },
                    onError: error =>
                    {
                        // Handle error case
                        NcDebug.LogError($"Error fetching seasons: {error}");
                    });

                if (seasonResponse == null || roundResponse == null)
                {
                    return;
                }
                ReserveArenaSeasonPush(seasonResponse, nextSeasonResponse, Agent.BlockIndex);
                ReserveArenaTicketPush(roundResponse, Agent.BlockIndex); ;

            }
            catch (Exception e)
            {
                NcDebug.LogError($"Failed to register arena notification: {e.Message}");
            }
        }

        private static void ReserveArenaSeasonPush(
            SeasonResponse seasonData,
            SeasonResponse nextSeasonData,
            long currentBlockIndex)
        {
            if (seasonData.ArenaType == GeneratedApiNamespace.ArenaServiceClient.ArenaType.OFF_SEASON && nextSeasonData != null)
            {
                var prevPushIdentifier =
                    PlayerPrefs.GetString(ArenaSeasonPushIdentifierKey, string.Empty);
                if (!string.IsNullOrEmpty(prevPushIdentifier))
                {
                    PushNotifier.CancelReservation(prevPushIdentifier);
                    PlayerPrefs.DeleteKey(ArenaSeasonPushIdentifierKey);
                }

                var targetBlockIndex = nextSeasonData.StartBlockIndex;
                var timeSpan = (targetBlockIndex - currentBlockIndex).BlockToTimeSpan();

                var arenaTypeText = nextSeasonData.ArenaType == GeneratedApiNamespace.ArenaServiceClient.ArenaType.SEASON
                    ? L10nManager.Localize("UI_SEASON")
                    : L10nManager.Localize("UI_CHAMPIONSHIP");

                var arenaSeason = nextSeasonData.SeasonGroupId;

                var content = L10nManager.Localize(
                    "PUSH_ARENA_SEASON_START_CONTENT",
                    arenaTypeText,
                    arenaSeason);
                var identifier = PushNotifier.Push(content, timeSpan, PushNotifier.PushType.Arena);
                PlayerPrefs.SetString(ArenaSeasonPushIdentifierKey, identifier);
            }
        }

        private static void ReserveArenaTicketPush(
            RoundResponse roundData,
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

            var remainingBlockCount = roundData.EndBlockIndex - currentBlockIndex;
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
                interval - (currentBlockIndex - row.StartedBlockIndex) % interval;
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

        public void InitializeAnalyzer(
            Address? agentAddr = null,
            PlanetId? planetId = null,
            string rpcServerHost = null)
        {
            NcDebug.Log("[Game] InitializeAnalyzer() invoked." +
                $" agentAddr: {agentAddr}, planetId: {planetId}, rpcServerHost: {rpcServerHost}");
            if (GameConfig.IsEditor)
            {
                NcDebug.Log("[Game] InitializeAnalyzer()... Analyze is disabled in editor mode.");
                Analyzer = new Analyzer(
                    agentAddr?.ToString(),
                    planetId?.ToString(),
                    rpcServerHost,
                    isTrackable: false);
                return;
            }

            var isTrackable = true;
            if (Debug.isDebugBuild)
            {
                NcDebug.Log("This is debug build.");
                isTrackable = false;
            }

            if (_commandLineOptions.Development)
            {
                NcDebug.Log("This is development mode.");
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
            NcDebug.Log(_commandLineOptions.ToString());
        }

        public static void QuitWithMessage(string message, string debugMessage = null)
        {
            message = string.IsNullOrEmpty(debugMessage)
                ? message
                : message + "\n" + debugMessage;

            if (!Widget.TryFind<OneButtonSystem>(out var widget))
            {
                widget = Widget.Create<OneButtonSystem>();
            }

            widget.Show(message,
                L10nManager.Localize("UI_QUIT"),
                ApplicationQuit);
        }

#region Initialize On Awake

        private void PreAwake()
        {
            CurrentSocialEmail = string.Empty;

            NcDebug.Log("[Game] Awake() invoked");
            GL.Clear(true, true, Color.black);
            Application.runInBackground = true;
        }

        private void CreateAgent()
        {
#if UNITY_EDITOR && !(UNITY_ANDROID || UNITY_IOS)
            // Local Headless
            if (useLocalHeadless && HeadlessHelper.CheckHeadlessSettings())
            {
                _headlessThread = new Thread(HeadlessHelper.RunLocalHeadless);
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
        }

        private void PostAwake()
        {
            States = new States();
            LocalLayer = new LocalLayer();
            LocalLayerActions = new LocalLayerActions();
        }

#endregion Initialize On Awake

#region Initialize On Start

        public IEnumerator InitializeL10N()
        {
            if (LiveAsset.GameConfig.IsKoreanBuild)
            {
                yield return L10nManager.Initialize(LanguageType.Korean).ToYieldInstruction();
                yield break;
            }

            if (GameConfig.IsEditor)
            {
                if (useSystemLanguage)
                {
                    yield return L10nManager.Initialize().ToYieldInstruction();
                }
                else
                {
                    yield return L10nManager.Initialize(languageType.Value).ToYieldInstruction();
                    languageType.Subscribe(value => L10nManager.SetLanguage(value)).AddTo(gameObject);
                }
                yield break;
            }

            yield return L10nManager
                .Initialize(string.IsNullOrWhiteSpace(_commandLineOptions.Language)
                    ? L10nManager.CurrentLanguage
                    : LanguageTypeMapper.ISO639(_commandLineOptions.Language))
                .ToYieldInstruction();
        }

        public void InitializeMessagePackResolver()
        {
#if ENABLE_IL2CPP
            // Because of strict AOT environments, use StaticCompositeResolver for IL2CPP.
            StaticCompositeResolver.Instance.Register(
                MagicOnion.Resolvers.MagicOnionResolver.Instance,
                NineChroniclesResolver.Instance,
                GeneratedResolver.Instance,
                StandardResolver.Instance
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
        }

        public bool CheckRequiredUpdate()
        {
            if (!_commandLineOptions.RequiredUpdate)
            {
                return false;
            }

            var popup = Widget.Find<IconAndButtonSystem>();
            if (Nekoyume.Helper.Util.GetKeystoreJson() != string.Empty)
            {
                popup.ShowWithTwoButton(
                    "UI_REQUIRED_UPDATE_TITLE",
                    "UI_REQUIRED_UPDATE_CONTENT",
                    "UI_OK",
                    "UI_KEY_BACKUP",
                    true,
                    IconAndButtonSystem.SystemType.Information);
                popup.SetCancelCallbackToBackup();
            }
            else
            {
                popup.Show(
                    "UI_REQUIRED_UPDATE_TITLE",
                    "UI_REQUIRED_UPDATE_CONTENT",
                    "UI_OK",
                    true,
                    IconAndButtonSystem.SystemType.Information);
            }

            popup.ConfirmCallback = OpenUpdateURL;
            return true;
        }

        public void InitializeFirstResources()
        {
            // Initialize MainCanvas first
            MainCanvas.instance.InitializeFirst();

            var settingPopup = Widget.Find<SettingPopup>();
            settingPopup.UpdateSoundSettings();

            // Initialize TableSheets. This should be done before initialize the Agent.
            ResourcesHelper.Initialize();
            NcDebug.Log("[Game] Start()... ResourcesHelper initialized");
        }

        public async UniTask InitializeAudioControllerAsync()
        {
            await AudioController.instance.InitializeAsync();
            NcDebug.Log("[Game] Start()... AudioController initialized");

            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);
        }

        public void OnAgentInitializeSucceed()
        {
            Analyzer.SetAgentAddress(Agent.Address.ToString());
            Analyzer.Instance.Track("Unity/Intro/Start/AgentInitialized");

            var settingPopup = Widget.Find<SettingPopup>();
            settingPopup.UpdatePrivateKey(_commandLineOptions.PrivateKey);
        }

        public void OnAgentInitializeFailed()
        {
            Analyzer.Instance.Track("Unity/Intro/Start/AgentInitializeFailed");

            QuitWithAgentConnectionError(null);
        }

        public async UniTask InitializeStage()
        {
            Stage.Initialize();

            var sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            await BattleRenderer.Instance.InitializeVfxAsync();
            sw.Stop();
            NcDebug.Log($"[Game] Start()... BattleRenderer vfx initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
        }

#endregion Initialize On Start

#region Initialize On Login

        public IEnumerator AgentInitialize(bool needDimmed, Action<bool> loginCallback)
        {
            var sw = new Stopwatch();
            sw.Reset();
            sw.Start();

            if (needDimmed)
            {
                var dimmedLoadingScreen = Widget.Find<DimmedLoadingScreen>();
                dimmedLoadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForConnectingToPlanet);
            }

            yield return Agent.Initialize(
                _commandLineOptions,
                KeyManager.Instance.SignedInPrivateKey,
                loginCallback);
            sw.Stop();
            NcDebug.Log($"[Game] CoLogin()... AgentInitialized Complete in {sw.ElapsedMilliseconds}ms.(elapsed)");

            if (needDimmed)
            {
                var dimmedLoadingScreen = Widget.Find<DimmedLoadingScreen>();
                dimmedLoadingScreen.Close();
            }
        }

#endregion Initialize On Login

        private void OpenUpdateURL()
        {
            // ReSharper disable once JoinDeclarationAndInitializer
            var updateUrl = string.Empty;
#if RUN_ON_MOBILE && UNITY_ANDROID
            updateUrl = _commandLineOptions?.GoogleMarketUrl;
#elif RUN_ON_MOBILE && UNITY_IOS
            updateUrl = _commandLineOptions?.AppleMarketUrl;
#endif
            if (string.IsNullOrEmpty(updateUrl))
            {
                return;
            }

            Application.OpenURL(updateUrl);
        }

        public static void ApplicationQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Downloads sheets from a specified bucket URL for a given planet.
        /// </summary>
        /// <param name="planetId">The identifier of the planet to download sheets for.</param>
        /// <param name="sheetBuckUrl">The base URL of the sheet bucket.</param>
        /// <param name="sheetNames">A collection of sheet names to be downloaded.</param>
        /// <returns>A dictionary mapping sheet names to their downloaded content.</returns>
        public static async Task<IDictionary<string, string>> DownloadSheet(PlanetId planetId,
            string sheetBuckUrl, ICollection<string> sheetNames)
        {
            NcDebug.Log($"[DownloadSheet] {sheetBuckUrl}/{planetId}");
            var planet = planetId.ToString();
            var downloadedSheets = new ConcurrentDictionary<string, string>();
            const int maxRetries = 3;
            const float delayBetweenRetries = 2.0f;

            // Create a cancellation token source for the entire operation
            using var cts = new CancellationTokenSource();

            // Create a list to hold all download tasks
            var downloadTasks = sheetNames.Select(async sheetName =>
            {
                var csvName = $"{sheetName}.csv";
                var sheetUrl = $"{sheetBuckUrl}/{planet}/{csvName}";
                bool success = false;

                // Retry request if network request failed.
                for (int attempt = 0; attempt < maxRetries && !success && !cts.Token.IsCancellationRequested; attempt++)
                {
                    using (UnityWebRequest request = UnityWebRequest.Get(sheetUrl))
                    {
                        try
                        {
                            // UnityWebRequest를 UniTask로 변환
                            var operation = request.SendWebRequest();
                            while (!operation.isDone && !cts.Token.IsCancellationRequested)
                            {
                                await UniTask.Yield();
                            }

                            if (cts.Token.IsCancellationRequested)
                            {
                                throw new OperationCanceledException();
                            }

                            if (request.result == UnityWebRequest.Result.Success)
                            {
                                var sheetData = request.downloadHandler.text;
                                downloadedSheets.TryAdd(sheetName, sheetData);
                                success = true;
                            }
                            else
                            {
                                var errorMessage = $"Failed to download sheet {csvName} (Attempt {attempt + 1}): {request.error}";
                                Debug.LogError(errorMessage);
                                if (attempt < maxRetries - 1)
                                {
                                    await UniTask.Delay(TimeSpan.FromSeconds(delayBetweenRetries), cancellationToken: cts.Token);
                                }
                                else
                                {
                                    await UniTask.SwitchToMainThread();
                                    Game.QuitWithMessage("ERROR_DOWNLOAD_SHEET_FAILED", errorMessage);
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception e)
                        {
                            var errorMessage = $"Error downloading sheet {csvName}: {e.Message}";
                            Debug.LogError(errorMessage);
                            await UniTask.SwitchToMainThread();
                            Game.QuitWithMessage("ERROR_DOWNLOAD_SHEET_FAILED", errorMessage);
                        }
                    }
                }

                if (!success && !cts.Token.IsCancellationRequested)
                {
                    await UniTask.SwitchToMainThread();
                    var errorMessage = $"Failed to download and save sheet {csvName} after {maxRetries} attempts.";
                    Debug.LogError(errorMessage);
                    Game.QuitWithMessage("ERROR_DOWNLOAD_SHEET_FAILED", errorMessage);
                }
            });

            try
            {
                // Wait for all download tasks to complete
                await UniTask.WhenAll(downloadTasks);
            }
            catch (OperationCanceledException)
            {
                NcDebug.Log("[DownloadSheet] Operation was cancelled.");
                throw;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error during sheet download operation: {e.Message}";
                Debug.LogError(errorMessage);
                await UniTask.SwitchToMainThread();
                Game.QuitWithMessage("ERROR_DOWNLOAD_SHEET_FAILED", errorMessage);
                throw;
            }

            return downloadedSheets;
        }

        /// <summary>
        /// loads sheets into a dictionary from CSV files based on the provided planet ID and sheet names.
        /// </summary>
        /// <param name="planetId">The ID of the planet, used to identify the source directory for the CSV files.</param>
        /// <param name="sheetNames">A list of sheet names to be loaded (without the .csv extension).</param>
        /// <returns>A dictionary where each key is a sheet name and the value is its content as a string.</returns>
        private async Task<Dictionary<string, string>> LoadSheets(PlanetId planetId, List<string> sheetNames)
        {
            var csv = new Dictionary<string, string>();
            var planet = planetId.ToString();

            foreach (var sheetName in sheetNames)
            {
                var csvName = $"{sheetName}.csv";
                try
                {
                    var data = await FileHelper.ReadAllTextAsync(planet, csvName);
                    csv[sheetName] = data;
                }
                catch (Exception e)
                {
                    var errorMessage = $"Failed to read sheet {csvName}: {e.Message}";
                    Debug.LogError(errorMessage);
                    await UniTask.SwitchToMainThread();
                    Game.QuitWithMessage("ERROR_INITIALIZE_FAILED", errorMessage);
                    throw;
                }
            }

            return csv;
        }
    }
}
