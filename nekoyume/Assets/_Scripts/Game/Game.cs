using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using Assets.SimpleLocalization;
using mixpanel;
using Nekoyume.BlockChain;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Helper;
using Nekoyume.Pattern;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Nekoyume.Game
{
    [RequireComponent(typeof(Agent), typeof(RPCAgent))]
    public class Game : MonoSingleton<Game>
    {
        public LocalizationManager.LanguageType languageType = LocalizationManager.LanguageType.English;

        private IAgent _agent;

        [SerializeField]
        private Stage stage = null;

        public Prologue prologue;

        public States States { get; private set; }

        public LocalStateSettings LocalStateSettings { get; private set; }

        public IAgent Agent => _agent;

        public Stage Stage => stage;

        // FIXME Action.PatchTableSheet.Execute()에 의해서만 갱신됩니다.
        // 액션 실행 여부와 상관 없이 최신 상태를 반영하게끔 수정해야합니다.
        public TableSheets TableSheets { get; private set; }

        public ActionManager ActionManager { get; private set; }

        public bool IsInitialized { get; private set; }

        public const string AddressableAssetsContainerPath = nameof(AddressableAssetsContainer);

        private CommandLineOptions _options;

        private static readonly string CommandLineOptionsJsonPath =
            Path.Combine(Application.streamingAssetsPath, "clo.json");

        #region Mono & Initialization

        protected override void Awake()
        {
            // FIXME 이후 사용자가 원치 않으면 정보를 보내지 않게끔 해야 합니다.
            Mixpanel.SetToken("80a1e14b57d050536185c7459d45195a");
            Mixpanel.Identify(NetworkInterface.GetAllNetworkInterfaces().First().GetPhysicalAddress().ToString());
            Mixpanel.Init();
            Mixpanel.Track("Unity/Started");

            Application.targetFrameRate = 60;
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            base.Awake();
            _options = CommandLineOptions.Load(
               CommandLineOptionsJsonPath
            );

            if (_options.RpcClient)
            {
                _agent = GetComponent<RPCAgent>();
            }
            else
            {
                _agent = GetComponent<Agent>();
            }
#if UNITY_EDITOR
            LocalizationManager.Initialize(languageType);
#else
            LocalizationManager.Initialize();
#endif
            States = new States();
            LocalStateSettings = new LocalStateSettings();
            prologue = GetComponent<Prologue>();
            MainCanvas.instance.InitializeFirst();
        }

        private IEnumerator Start()
        {
            yield return Addressables.InitializeAsync();
            yield return StartCoroutine(CoInitializeTableSheets());
            AudioController.instance.Initialize();
            yield return null;
            ActionManager = new ActionManager(_agent);
            // Agent 초기화.
            // Agent를 초기화하기 전에 반드시 Table과 TableSheets를 초기화 함.
            // Agent가 Table과 TableSheets에 약한 의존성을 갖고 있음.(Deserialize 단계 때문)
            var agentInitialized = false;
            var agentInitializeSucceed = false;
            yield return StartCoroutine(
                CoLogin(
                    succeed =>
                    {
                        agentInitialized = true;
                        agentInitializeSucceed = succeed;
                    }
                )
            );
            yield return new WaitUntil(() => agentInitialized);
            // UI 초기화 2차.
            yield return StartCoroutine(MainCanvas.instance.InitializeSecond());
            Stage.Initialize();
            yield return null;

            Observable.EveryUpdate()
                .Where(_ => Input.GetMouseButtonUp(0))
                .Select(_ => Input.mousePosition)
                .Subscribe(PlayMouseOnClickVFX)
                .AddTo(gameObject);

            ShowNext(agentInitializeSucceed);

            if (Agent is RPCAgent rpcAgent)
            {
                rpcAgent.OnDisconnected
                    .AsObservable()
                    .ObserveOnMainThread()
                    .Subscribe(_ =>
                    {
                        Widget.Find<SystemPopup>().Show(
                            "UI_ERROR",
                            "UI_ERROR_RPC_CONNECTION",
                            "UI_QUIT"
                        );
                    });
            }
        }

        private IEnumerator CoInitializeTableSheets()
        {
            TableSheets = new TableSheets();
            var request = Resources.LoadAsync<AddressableAssetsContainer>(AddressableAssetsContainerPath);
            yield return request;
            if (!(request.asset is AddressableAssetsContainer addressableAssetsContainer))
                throw new FailedToLoadResourceException<AddressableAssetsContainer>(AddressableAssetsContainerPath);

            List<TextAsset> csvAssets = addressableAssetsContainer.tableCsvAssets;
            foreach (var asset in csvAssets)
            {
                TableSheets.SetToSheet(asset.name, asset.text);
            }

            TableSheets.ItemSheetInitialize();
            TableSheets.QuestSheetInitialize();
        }

        public static IDictionary<string, string> GetTableCsvAssets()
        {
            var container = Resources.Load<AddressableAssetsContainer>(AddressableAssetsContainerPath);
            return container.tableCsvAssets.ToDictionary(asset => asset.name, asset => asset.text);
        }

        private void ShowNext(bool succeed)
        {
            IsInitialized = true;
            if (succeed)
            {
                Widget.Find<PreloadingScreen>().Close();
            }
            else
            {
                // FIXME 콜백 인자를 구조화 하면 타입 쿼리 없앨 수 있을 것 같네요.
                if (_agent is Agent agent && agent.BlockDownloadFailed)
                {
                    var errorMsg = string.Format(LocalizationManager.Localize("UI_ERROR_FORMAT"),
                        LocalizationManager.Localize("BLOCK_DOWNLOAD_FAIL"));

                    Widget.Find<SystemPopup>().Show(
                        LocalizationManager.Localize("UI_ERROR"),
                        errorMsg,
                        LocalizationManager.Localize("UI_QUIT"),
                        false
                    );
                }
                else if (_agent is RPCAgent rpcAgent && !rpcAgent.Connected)
                {
                    Widget.Find<SystemPopup>().Show(
                        "UI_ERROR",
                        "UI_ERROR_RPC_CONNECTION",
                        "UI_QUIT"
                    );
                }
                else
                {
                    // FIXME: 최신 버전이 뭔지는 Agent.EncounrtedHighestVersion 속성에 들어있으니, 그걸 UI에서 표시해줘야 할 듯?
                    // AppProtocolVersion? newVersion = _agent is Agent agent ? agent.EncounteredHighestVersion : null;
                    Widget.Find<UpdatePopup>().Show();
                }
            }
        }

        #endregion

        protected override void OnApplicationQuit()
        {
            if (Mixpanel.IsInitialized())
            {
                Mixpanel.Flush();
            }
        }

        public static void Quit()
        {
            var confirm = Widget.Find<Confirm>();
            confirm.CloseCallback = result =>
            {
                if (result == ConfirmResult.Yes)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    return;
                }

                confirm.CloseCallback = null;

                Event.OnNestEnter.Invoke();
                Widget.Find<Login>().Show();
                Widget.Find<Menu>().Close();
            };
            confirm.Show("UI_CONFIRM_QUIT_TITLE", "UI_CONFIRM_QUIT_CONTENT", "UI_QUIT", "UI_CHARACTER_SELECT",
                blurRadius: 2, submittable: false);
        }

        private static void PlayMouseOnClickVFX(Vector3 position)
        {
            position = ActionCamera.instance.Cam.ScreenToWorldPoint(position);
            var vfx = VFXController.instance.CreateAndChaseCam<MouseClickVFX>(position);
            vfx.Play();
        }

        private IEnumerator CoLogin(Action<bool> callback)
        {
            if (_options.Maintenance)
            {
                var w = Widget.Create<Alert>();
                w.CloseCallback = () =>
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
                    "UI_OK"
                );
                yield break;
            }

            if (_options.TestEnd)
            {
                var w = Widget.Find<Confirm>();
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

            var loginPopup = Widget.Find<LoginPopup>();

            if (Application.isBatchMode)
            {
                loginPopup.Show(_options.KeyStorePath, _options.PrivateKey);
            }
            else
            {
                Widget.Find<UI.Settings>().UpdateSoundSettings();
                var title = Widget.Find<Title>();
                title.Show(_options.KeyStorePath, _options.PrivateKey);
                yield return new WaitUntil(() => loginPopup.Login);
                title.Close();
            }

            _agent.Initialize(
                _options,
                loginPopup.GetPrivateKey(),
                callback
            );
        }

        public void ResetStore()
        {
            var confirm = Widget.Find<Confirm>();
            var storagePath = _options.StoragePath ?? BlockChain.Agent.DefaultStoragePath;
            confirm.CloseCallback = result =>
            {
                if (result == ConfirmResult.No)
                    return;
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
            var confirm = Widget.Find<Confirm>();
            confirm.CloseCallback = result =>
            {
                if (result == ConfirmResult.No)
                    return;
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
    }
}
