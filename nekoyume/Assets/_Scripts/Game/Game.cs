using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Extension;
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
    [RequireComponent(typeof(Agent))]
    public class Game : MonoSingleton<Game>
    {
        public LocalizationManager.LanguageType languageType = LocalizationManager.LanguageType.English;

        private IAgent _agent;
        
        [SerializeField] private Stage stage = null;
        
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

#if UNITY_EDITOR
        private static readonly string WebCommandLineOptionsPathInit = string.Empty;
        private static readonly string WebCommandLineOptionsPathLogin = string.Empty;
#else
        private const string WebCommandLineOptionsPathInit = "https://planetarium.dev/9c-alpha-clo.json";
        private const string WebCommandLineOptionsPathLogin = "https://planetarium.dev/9c-alpha-clo.json";
#endif

        private static readonly string CommandLineOptionsJsonPath =
            Path.Combine(Application.streamingAssetsPath, "clo.json");

        #region Mono & Initialization

        protected override void Awake()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            base.Awake();
            _agent = GetComponent<Agent>();
#if UNITY_EDITOR
            LocalizationManager.Initialize(languageType);
#else
            LocalizationManager.Initialize();
#endif
            States = new States();
            LocalStateSettings = new LocalStateSettings();
            ActionManager = new ActionManager(_agent);
            MainCanvas.instance.InitializeFirst();
        }

        private IEnumerator Start()
        {
            yield return Addressables.InitializeAsync();
            yield return StartCoroutine(CoInitializeTableSheets());
            AudioController.instance.Initialize();
            yield return null;
            // Agent 초기화.
            // Agent를 초기화하기 전에 반드시 Table과 TableSheets를 초기화 함.
            // Agent가 Table과 TableSheets에 약한 의존성을 갖고 있음.(Deserialize 단계 때문)
            var agentInitialized = false;
            var agentInitializeSucceed = false;
            _options = CommandLineOptions.Load(
                CommandLineOptionsJsonPath,
                WebCommandLineOptionsPathInit
            );
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
            Stage.objectPool.Initialize();
            yield return null;
            Stage.dropItemFactory.Initialize();
            yield return null;

            Observable.EveryUpdate()
                .Where(_ => Input.GetMouseButtonUp(0))
                .Select(_ => Input.mousePosition)
                .Subscribe(PlayMouseOnClickVFX)
                .AddTo(gameObject);
            
            ShowNext(agentInitializeSucceed);
        }

        private IEnumerator CoInitializeTableSheets()
        {
            TableSheets = new TableSheets();
            //어드레서블어셋에 새로운 테이블을 추가하면 AddressableAssetsContainer.asset에도 해당 csv파일을 추가해줘야합니다.
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
                else
                {
                    Widget.Find<UpdatePopup>().Show();
                }
            }
        }

        #endregion

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
            confirm.Show("UI_CONFIRM_QUIT_TITLE", "UI_CONFIRM_QUIT_CONTENT", "UI_QUIT", "UI_CHARACTER_SELECT", blurRadius: 2);
        }
        
        private void PlayMouseOnClickVFX(Vector3 position)
        {
            position = ActionCamera.instance.Cam.ScreenToWorldPoint(position);
            var vfx = VFXController.instance.CreateAndChaseCam<MouseClickVFX>(position);
            vfx.Play();
        }

        private IEnumerator CoLogin(Action<bool> callback)
        {
            if (_options.maintenance)
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
                    LocalizationManager.Localize("UI_MAINTENANCE"),
                    LocalizationManager.Localize("UI_MAINTENANCE_CONTENT"),
                    LocalizationManager.Localize("UI_OK"),
                    false
                );
                yield break;
            }
            if (_options.testEnd)
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
                title.Show(_options.keyStorePath, _options.privateKey);
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
            var prevStoragePath = Path.Combine(BlockChain.Agent.PrevStorageDirectoryPath, $"{storagePath}_{UnityEngine.Random.Range(0, 100)}");
            confirm.CloseCallback = result =>
            {
                if (result == ConfirmResult.No)
                    return;
                if (Directory.Exists(storagePath))
                {
                    Directory.Move(storagePath, prevStoragePath);
                }
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
                var keyPath = _options.keyStorePath;
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
