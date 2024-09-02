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
using Menu = Nekoyume.UI.Menu;
using Random = UnityEngine.Random;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using Nekoyume.Model.Mail;
using Debug = UnityEngine.Debug;
#if ENABLE_FIREBASE
using NineChronicles.GoogleServices.Firebase.Runtime;
#endif

namespace Nekoyume.Game
{
    using Arena;
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

        public PlanetId? CurrentPlanetId { get; set; }

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

        public string GuildBucketUrl => _guildBucketUrl;

        public GuildServiceClient.GuildModel[] GuildModels { get; private set; } = { };

        private CommandLineOptions _commandLineOptions;

        public CommandLineOptions CommandLineOptions => _commandLineOptions;

        private PlayableDirector _activeDirector;

        private string _msg;

        public static readonly string CommandLineOptionsJsonPath =
            Platform.GetStreamingAssetsPath("clo.json");

        private Thread _headlessThread;
        private Thread _marketThread;

        private string _guildBucketUrl;

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
            var liveAssetManager = gameObject.AddComponent<LiveAssetManager>();
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

            var tableSheetsInitializedEvt = new AirbridgeEvent("Intro_Start_TableSheetsInitialized");
            AirbridgeUnity.TrackEvent(tableSheetsInitializedEvt);

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

            if (Widget.TryFind<IntroScreen>(out var introscreen))
            {
                introscreen.GetGuestPrivateKey();
            }

            NcDebug.Log("[Game] CommandLineOptions loaded");
            NcDebug.Log($"APV: {_commandLineOptions.AppProtocolVersion}");
            NcDebug.Log($"RPC: {_commandLineOptions.RpcServerHost}:{_commandLineOptions.RpcServerPort}");
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
                NcDebug.Log(
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

                        var requestEvt = new AirbridgeEvent("Intro_Pledge_Request");
                        AirbridgeUnity.TrackEvent(requestEvt);

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
                    NcDebug.Log("[Game] CoCheckPledge()... Rendering ApprovePledge" +
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
            var map = csvAssets.ToDictionary(
                asset => Addresses.TableSheet.Derive(asset.name),
                asset => asset.name);
            var dict = await Agent.GetSheetsAsync(map.Keys);
            sw.Stop();
            NcDebug.Log($"[{nameof(SyncTableSheetsAsync)}] get state: {sw.Elapsed}");
            sw.Restart();
            var csv = dict.ToDictionary(
                pair => map[pair.Key],
                // NOTE: `pair.Value` is `null` when the chain not contains the `pair.Key`.
                pair => pair.Value is Text ? pair.Value.ToDotnetString() : null);

            TableSheets = await TableSheets.MakeTableSheetsAsync(csv);
            sw.Stop();
            NcDebug.Log($"[{nameof(SyncTableSheetsAsync)}] TableSheets Constructor: {sw.Elapsed}");
        }

        private async UniTask InitializeStakeStateAsync()
        {
            // NOTE: Initialize staking states after setting GameConfigState.
            var stakeAddr = Model.Stake.StakeStateV2.DeriveAddress(Agent.Address);
            var stakeStateIValue = await Agent.GetStateAsync(ReservedAddresses.LegacyAccount, stakeAddr);
            var goldCurrency = States.GoldBalanceState.Gold.Currency;
            var balance = goldCurrency * 0;
            var stakeRegularFixedRewardSheet = new StakeRegularFixedRewardSheet();
            var stakeRegularRewardSheet = new StakeRegularRewardSheet();
            var policySheet = TableSheets.StakePolicySheet;
            Address[] sheetAddr;
            Model.Stake.StakeStateV2? stakeState = null;
            if (!StakeStateUtilsForClient.TryMigrate(
                stakeStateIValue,
                States.Instance.GameConfigState,
                out var stakeStateV2))
            {
                if (Agent is RPCAgent)
                {
                    sheetAddr = new[]
                    {
                        Addresses.GetSheetAddress(policySheet.StakeRegularFixedRewardSheetValue),
                        Addresses.GetSheetAddress(policySheet.StakeRegularRewardSheetValue)
                    };
                }
                // It is local play. local genesis block not has Stake***Sheet_V*.
                // 로컬에서 제네시스 블록을 직접 생성하는 경우엔 스테이킹 보상-V* 시트가 없기 때문에, 오리지널 시트로 대체합니다.
                else
                {
                    sheetAddr = new[]
                    {
                        Addresses.GetSheetAddress(nameof(StakeRegularFixedRewardSheet)),
                        Addresses.GetSheetAddress(nameof(StakeRegularRewardSheet))
                    };
                }
            }
            else
            {
                stakeState = stakeStateV2;
                balance = await Agent.GetBalanceAsync(stakeAddr, goldCurrency);
                if (Agent is RPCAgent)
                {
                    sheetAddr = new[]
                    {
                        Addresses.GetSheetAddress(
                            stakeStateV2.Contract.StakeRegularFixedRewardSheetTableName),
                        Addresses.GetSheetAddress(
                            stakeStateV2.Contract.StakeRegularRewardSheetTableName)
                    };
                }
                // It is local play. local genesis block not has Stake***Sheet_V*.
                // 로컬에서 제네시스 블록을 직접 생성하는 경우엔 스테이킹 보상-V* 시트가 없기 때문에, 오리지널 시트로 대체합니다.
                else
                {
                    sheetAddr = new[]
                    {
                        Addresses.GetSheetAddress(nameof(StakeRegularFixedRewardSheet)),
                        Addresses.GetSheetAddress(nameof(StakeRegularRewardSheet))
                    };
                }
            }

            var sheets = await Agent.GetSheetsAsync(sheetAddr);
            stakeRegularFixedRewardSheet.Set(sheets[sheetAddr[0]].ToDotnetString());
            stakeRegularRewardSheet.Set(sheets[sheetAddr[1]].ToDotnetString());
            var level = balance.RawValue > 0
                ? stakeRegularFixedRewardSheet.FindLevelByStakedAmount(
                    Agent.Address,
                    balance)
                : 0;
            States.Instance.SetStakeState(
                stakeState,
                new GoldBalanceState(stakeAddr, balance),
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

                var evt = new AirbridgeEvent("Intro_Player_Quit");
                AirbridgeUnity.TrackEvent(evt);
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
            NcDebug.LogException(exc);

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
            vfx.Play();
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
            AudioController.instance.Initialize();
            NcDebug.Log("[Game] Start()... AudioController initialized");
        }

        public void OnAgentInitializeSucceed()
        {
            Analyzer.SetAgentAddress(Agent.Address.ToString());
            Analyzer.Instance.Track("Unity/Intro/Start/AgentInitialized");

            var evt = new AirbridgeEvent("Intro_Start_AgentInitialized");
            evt.AddCustomAttribute("agent-address", Agent.Address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            var settingPopup = Widget.Find<SettingPopup>();
            settingPopup.UpdatePrivateKey(_commandLineOptions.PrivateKey);
        }

        public void OnAgentInitializeFailed()
        {
            Analyzer.Instance.Track("Unity/Intro/Start/AgentInitializeFailed");

            var evt = new AirbridgeEvent("Intro_Start_AgentInitializeFailed");
            evt.AddCustomAttribute("agent-address", Agent.Address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            QuitWithAgentConnectionError(null);
        }

        public async UniTask InitializeStage()
        {
            var sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            await Stage.InitializeAsync();
            sw.Stop();
            NcDebug.Log($"[Game] Start()... Stage initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
            sw.Reset();
            sw.Start();
            await Arena.InitializeAsync();
            sw.Stop();
            NcDebug.Log($"[Game] Start()... Arena initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
            sw.Reset();
            sw.Start();
            await RaidStage.InitializeAsync();
            sw.Stop();
            NcDebug.Log($"[Game] Start()... RaidStage initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
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
    }
}
