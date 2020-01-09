using System.Collections;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Data;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Pattern;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Nekoyume.Game
{
    public static class GameExtensionMethods
    {
        private static Transform _root;

        public static T GetRootComponent<T>(this MonoBehaviour behaviour)
        {
            if (_root == null)
                _root = behaviour.transform.root;

            return _root.GetComponent<T>();
        }

        public static T GetOrAddComponent<T>(this MonoBehaviour behaviour) where T : MonoBehaviour
        {
            var t = behaviour.GetComponent<T>();
            return t ? t : behaviour.gameObject.AddComponent<T>();
        }
    }

    [RequireComponent(typeof(Agent))]
    public class Game : MonoSingleton<Game>
    {
        public LocalizationManager.LanguageType languageType = LocalizationManager.LanguageType.English;

        private Agent _agent;
        
        [SerializeField] private Stage stage = null;
        
        public States States { get; private set; }

        public LocalStateSettings LocalStateSettings { get; private set; }

        public Agent Agent => _agent;

        public Stage Stage => stage;

        public TableSheets TableSheets { get; private set; }
        
        public bool IsInitialized { get; private set; }

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
            MainCanvas.instance.InitializeFirst();
        }

        private IEnumerator Start()
        {
            // Table 초기화.
            Tables.instance.Initialize();
            yield return Addressables.InitializeAsync();
            TableSheets = new TableSheets();
            yield return StartCoroutine(TableSheets.CoInitialize());
            AudioController.instance.Initialize();
            yield return null;
            // Agent 초기화.
            // Agent를 초기화하기 전에 반드시 Table과 TableSheets를 초기화 함.
            // Agent가 Table과 TableSheets에 약한 의존성을 갖고 있음.(Deserialize 단계 때문)
            var agentInitialized = false;
            var agentInitializeSucceed = false;
            _agent.Initialize(succeed =>
            {
                agentInitialized = true;
                agentInitializeSucceed = succeed;
            });
            yield return new WaitUntil(() => agentInitialized);
            TableSheets.InitializeWithTableSheetsState();
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

        private void ShowNext(bool succeed)
        {
            IsInitialized = true;
            if (succeed)
            {
                Widget.Find<PreloadingScreen>().Close();
            }
            else
            {
                if (_agent.BlockDownloadFailed)
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
                if (result == ConfirmResult.No)
                    return;
                
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            };
            confirm.Set("UI_CONFIRM_QUIT_TITLE", "UI_CONFIRM_QUIT_CONTENT");
        }
        
        private void PlayMouseOnClickVFX(Vector3 position)
        {
            position = ActionCamera.instance.Cam.ScreenToWorldPoint(position);
            var vfx = VFXController.instance.CreateAndChaseCam<MouseClickVFX>(position);
            vfx.Play();
        }

        #region PlaymodeTest

        public void Init()
        {
            _agent.Initialize(ShowNext);
        }

        public IEnumerator TearDown()
        {
            Destroy(GetComponent<Agent>());
            yield return new WaitForEndOfFrame();
        }

        #endregion
    }
}
