using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c.Formatters;
using Libplanet;
using Libplanet.Assets;
using LruCacheNet;
using MessagePack;
using MessagePack.Resolvers;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Notice;
using Nekoyume.Game.VFX;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.Pattern;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.UI.Module.WorldBoss;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.Playables;
using Menu = Nekoyume.UI.Menu;
using RocksDbSharp;
using UnityEngine.Android;

namespace Nekoyume.Game
{
    using Nekoyume.GraphQL;
    using UniRx;

    [RequireComponent(typeof(Agent), typeof(RPCAgent))]
    public class Game : MonoSingleton<Game>
    {
        [SerializeField] private Stage stage = null;

        [SerializeField] private Arena arena = null;

        [SerializeField] private RaidStage raidStage = null;

        [SerializeField] private Lobby lobby;

        [SerializeField] private bool useSystemLanguage = true;

        [SerializeField] private LanguageTypeReactiveProperty languageType = default;

        [SerializeField] private Prologue prologue = null;

        public States States { get; private set; }

        public LocalLayer LocalLayer { get; private set; }

        public LocalLayerActions LocalLayerActions { get; private set; }

        public IAgent Agent { get; private set; }

        public Analyzer Analyzer { get; private set; }

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

        public Prologue Prologue => prologue;

        public const string AddressableAssetsContainerPath = nameof(AddressableAssetsContainer);

        public NineChroniclesAPIClient ApiClient => _apiClient;
        public NineChroniclesAPIClient RpcClient => _rpcClient;

        public readonly LruCache<Address, IValue> CachedStates = new LruCache<Address, IValue>();

        public readonly LruCache<Address, FungibleAssetValue> CachedBalance =
            new LruCache<Address, FungibleAssetValue>(2);

        public readonly Dictionary<Address, bool> CachedAddresses = new Dictionary<Address, bool>();

        private CommandLineOptions _options;

        private AmazonCloudWatchLogsClient _logsClient;

        private NineChroniclesAPIClient _apiClient;

        private NineChroniclesAPIClient _rpcClient;

        private PlayableDirector _activeDirector;

        private string _msg;

        public static string CommandLineOptionsJsonPath =
            Platform.GetStreamingAssetsPath("clo.json");

        #region Mono & Initialization

        protected override void Awake()
        {
            Debug.Log("[Game] Awake() invoked");
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
            LoadRocksDBNative();
            bool HasStoragePermission() =>
                Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite)
                && Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead);
        
            String[] permission = new String[]
            {
                Permission.ExternalStorageRead,
                Permission.ExternalStorageWrite
            };
        
            while (!HasStoragePermission())
            {
                Permission.RequestUserPermissions(permission);
            }
#endif
            Application.targetFrameRate = 60;
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            base.Awake();

#if UNITY_IOS
            _options = CommandLineOptions.Load(Platform.GetStreamingAssetsPath("clo.json"));
#else
            _options = CommandLineOptions.Load(
                CommandLineOptionsJsonPath
            );
#endif
            Debug.Log("[Game] Awake() CommandLineOptions loaded");

            if (_options.RpcClient)
            {
                Agent = GetComponent<RPCAgent>();
                SubscribeRPCAgent();
            }
            else
            {
                Agent = GetComponent<Agent>();
            }

            States = new States();
            LocalLayer = new LocalLayer();
            LocalLayerActions = new LocalLayerActions();
            MainCanvas.instance.InitializeIntro();
        }

        private void LoadRocksDBNativeLib()
        {
            string Path_RocksDB = default;
            if (Application.platform == RuntimePlatform.Android)
            {
                Path_RocksDB = Application.dataPath.Split("/base.apk")[0];
                Path_RocksDB = Path.Combine(Path_RocksDB, "lib");
                Path_RocksDB = Path.Combine(Path_RocksDB, Environment.Is64BitOperatingSystem? "arm64": "arm");
                Path_RocksDB = Path.Combine(Path_RocksDB, "librocksdb.so");
                Debug.LogWarning($"native load path = {Path_RocksDB}");
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                //D:\NineChronicles\NineChro\nekoyume
                Path_RocksDB = Environment.CurrentDirectory;
                Path_RocksDB = Path.Combine(Path_RocksDB, "Assets", "Packages", "runtimes");
                Path_RocksDB = Path.Combine(Path_RocksDB, "win-x64", "native", "rocksdb.dll");
            }
            else if(Application.platform == RuntimePlatform.WindowsPlayer)
            {
                // pc standalone
                Path_RocksDB = Path.Combine(Application.dataPath, "Plugins");
                Path_RocksDB = Path.Combine(Path_RocksDB, "x86_64", "rocksdb.dll");
            }
            // Load native library for rocksdb 
            RocksDbSharp.Native.LoadLibrary(Path_RocksDB);    
        }

        private IEnumerator Start()
        {
            // fix android can't use www in sub thread
#if LIB9C_DEV_EXTENSIONS
            Lib9c.DevExtensions.TestbedHelper.LoadTestbedCreateAvatarForQA();
#endif
            Debug.Log("[Game] Start() invoked");

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
            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
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
            yield return L10nManager.Initialize(LanguageTypeMapper.ISO396(_options.Language)).ToYieldInstruction();
#endif
            Debug.Log("[Game] Start() L10nManager initialized");

            // Initialize MainCanvas first
            MainCanvas.instance.InitializeFirst();
            // Initialize TableSheets. This should be done before initialize the Agent.
            yield return StartCoroutine(CoInitializeTableSheets());
            Debug.Log("[Game] Start() TableSheets initialized");
            yield return StartCoroutine(ResourcesHelper.CoInitialize());
            Debug.Log("[Game] Start() ResourcesHelper initialized");
            AudioController.instance.Initialize();
            Debug.Log("[Game] Start() AudioController initialized");
            yield return null;
            // Initialize Agent
            var agentInitialized = false;
            var agentInitializeSucceed = false;
            yield return StartCoroutine(
                CoLogin(
                    succeed =>
                    {
                        Debug.Log($"Agent initialized. {succeed}");
                        agentInitialized = true;
                        agentInitializeSucceed = succeed;
                    }
                )
            );

            yield return new WaitUntil(() => agentInitialized);
            InitializeAnalyzer();
            Analyzer.Track("Unity/Started");
            // NOTE: Create ActionManager after Agent initialized.
            ActionManager = new ActionManager(Agent);
            yield return SyncTableSheetsAsync().ToCoroutine();
            Debug.Log("[Game] Start() TableSheets synchronized");
            RxProps.Start(Agent, States, TableSheets);
            // Initialize RequestManager and NoticeManager
            gameObject.AddComponent<RequestManager>();
            var noticeManager = gameObject.AddComponent<NoticeManager>();
            noticeManager.InitializeData();
            yield return new WaitUntil(() => noticeManager.IsInitialized);
            Debug.Log("[Game] Start() RequestManager & NoticeManager initialized");
            // Initialize MainCanvas second
            yield return StartCoroutine(MainCanvas.instance.InitializeSecond());
            // Initialize NineChroniclesAPIClient.
            _apiClient = new NineChroniclesAPIClient(_options.ApiServerHost);
            if (!string.IsNullOrEmpty(_options.RpcServerHost))
            {
                _rpcClient = new NineChroniclesAPIClient($"http://{_options.RpcServerHost}/graphql");
            }

            WorldBossQuery.SetUrl(_options.OnBoardingHost);

            // Initialize Rank.SharedModel
            RankPopup.UpdateSharedModel();
            // Initialize Stage
            Stage.Initialize();
            Arena.Initialize();
            RaidStage.Initialize();


            Widget.Find<VersionSystem>().SetVersion(Agent.AppProtocolVersion);

            ShowNext(agentInitializeSucceed);
            StartCoroutine(CoUpdate());
        }

        protected override void OnDestroy()
        {
            ActionManager?.Dispose();
            base.OnDestroy();
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
                Widget.Find<PreloadingScreen>().IsActive() ||
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
                popup.SetCancelCallbackToExit();

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
            popup.SetCancelCallbackToExit();
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
            var dict = await Agent.GetStateBulk(map.Keys);
            var csv = dict.ToDictionary(
                pair => map[pair.Key],
                // NOTE: `pair.Value` is `null` when the chain not contains the `pair.Key`.
                pair => pair.Value is null
                    ? null
                    : pair.Value.ToDotnetString());

            TableSheets = new TableSheets(csv);
        }

        public static IDictionary<string, string> GetTableCsvAssets()
        {
            var container =
                Resources.Load<AddressableAssetsContainer>(AddressableAssetsContainerPath);
            return container.tableCsvAssets.ToDictionary(asset => asset.name, asset => asset.text);
        }

        private void ShowNext(bool succeed)
        {
            Debug.Log($"[Game]ShowNext({succeed}) invoked");
            if (succeed)
            {
                IsInitialized = true;
                var intro = Widget.Find<IntroScreen>();
                intro.Close();
                Widget.Find<PreloadingScreen>().Show();
                StartCoroutine(ClosePreloadingScene(4));
            }
            else
            {
                QuitWithAgentConnectionError(null);
            }
        }

        private IEnumerator ClosePreloadingScene(float time)
        {
            yield return new WaitForSeconds(time);
            Widget.Find<PreloadingScreen>().Close();
        }

        #endregion

        protected override void OnApplicationQuit()
        {
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

        public static async UniTaskVoid BackToMainAsync(Exception exc, bool showLoadingScreen = false)
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
            if (IsInWorld)
            {
                NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System,
                    L10nManager.Localize("UI_BLOCK_EXIT"),
                    NotificationCell.NotificationType.Information);
                return;
            }

            Event.OnNestEnter.Invoke();

            var deletableWidgets = Widget.FindWidgets().Where(widget =>
                !(widget is SystemWidget) &&
                !(widget is MessageCatTooltip) && widget.IsActive());
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
            popup.Show(L10nManager.Localize("UI_ERROR"), errorMsg,
                L10nManager.Localize("UI_OK"), false);
            popup.SetCancelCallbackToExit();
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

        private IEnumerator CoLogin(Action<bool> callback)
        {
            if (_options.Maintenance)
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

            if (_options.TestEnd)
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

            var settings = Widget.Find<UI.SettingPopup>();
            settings.UpdateSoundSettings();
            settings.UpdatePrivateKey(_options.PrivateKey);

            var loginPopup = Widget.Find<LoginSystem>();

            if (Application.isBatchMode)
            {
                loginPopup.Show(_options.KeyStorePath, _options.PrivateKey);
            }
            else
            {
                var intro = Widget.Find<IntroScreen>();
                intro.Show(_options.KeyStorePath, _options.PrivateKey);
                yield return new WaitUntil(() => loginPopup.Login);
            }

            yield return Agent.Initialize(
                _options,
                loginPopup.GetPrivateKey(),
                callback
            );
        }

        public void ResetStore()
        {
            var confirm = Widget.Find<ConfirmPopup>();
            var storagePath = _options.StoragePath ?? BlockChain.Agent.DefaultStoragePath;
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

                var keyPath = _options.KeyStorePath;
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
            if (Agent.PrivateKey == default)
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
                var streamName = _options.AwsSinkGuid;
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
                var token = resp.LogStreams.FirstOrDefault(s => s.LogStreamName == streamName)?.UploadSequenceToken;
                var ie = new InputLogEvent
                {
                    Message = msg,
                    Timestamp = DateTime.UtcNow
                };
                var request = new PutLogEventsRequest(groupName, streamName, new List<InputLogEvent> { ie });
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

        public void PauseTimeline(PlayableDirector whichOne)
        {
            _activeDirector = whichOne;
            _activeDirector.playableGraph.GetRootPlayable(0).SetSpeed(0);
        }

        public void ResumeTimeline()
        {
            _activeDirector.playableGraph.GetRootPlayable(0).SetSpeed(1);
        }

        private void InitializeAnalyzer()
        {
            var uniqueId = Agent.Address.ToString();
            var rpcServerHost = _options.RpcClient
                ? _options.RpcServerHost
                : null;

#if UNITY_EDITOR
            Debug.Log("This is editor mode.");
            Analyzer = new Analyzer(uniqueId, rpcServerHost);
            return;
#endif
            var isTrackable = true;
            if (Debug.isDebugBuild)
            {
                Debug.Log("This is debug build.");
                isTrackable = false;
            }

            if (_options.Development)
            {
                Debug.Log("This is development mode.");
                isTrackable = false;
            }

            Analyzer = new Analyzer(
                uniqueId,
                rpcServerHost,
                isTrackable);
        }
        void Update()
        {
#if UNITY_ANDROID
            if (Platform.IsMobilePlatform())
            {
                int width = Screen.resolutions[0].width;
                int height = Screen.resolutions[0].height;
                if (Screen.currentResolution.width != height || Screen.currentResolution.height != width)
                {
                    Debug.LogWarning($"fix Resolution to w={width} h={height}");
                    Screen.SetResolution(height, width, true);
                }
            }
#endif
        }
        private void LoadRocksDBNative()
        {
            string loadPath = default;
            if (Application.platform == RuntimePlatform.Android)
            {
                loadPath = Application.dataPath.Split("/base.apk")[0];
                loadPath = Path.Combine(loadPath, "lib");
                loadPath = Path.Combine(loadPath, Environment.Is64BitOperatingSystem ? "arm64" : "arm");
                loadPath = Path.Combine(loadPath, "librocksdb.so");
                Debug.LogWarning($"native load path = {loadPath}");
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                //D:\NineChronicles\NineChro\nekoyume
                loadPath = Environment.CurrentDirectory;
                loadPath = Path.Combine(loadPath, "Assets", "Packages", "runtimes");
                loadPath = Path.Combine(loadPath, "win-x64", "native", "rocksdb.dll");
            }
            else if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                // pc standalone
                loadPath = Path.Combine(Application.dataPath, "Plugins");
                loadPath = Path.Combine(loadPath, "x86_64", "rocksdb.dll");
            }

            RocksDbSharp.Native.LoadLibrary(loadPath);
        }

    }
}
