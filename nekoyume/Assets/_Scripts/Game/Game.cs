using System.Collections;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Data;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Pattern;
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

    public class Game : MonoSingleton<Game>
    {
        public LocalizationManager.LanguageType languageType = LocalizationManager.LanguageType.English;
        public Stage stage;
        public Agent agent;

        public TableSheets TableSheets { get; private set; }
        public bool initialized;

        protected override void Awake()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            base.Awake();
#if UNITY_EDITOR
            LocalizationManager.Read(languageType);
#else
            LocalizationManager.Read();
#endif
            MainCanvas.instance.InitializeFirst();
            Widget.Find<LoadingScreen>().Show();
        }

        private IEnumerator Start()
        {
            Tables.instance.Initialize();
            yield return Addressables.InitializeAsync();
            TableSheets = new TableSheets();
            yield return StartCoroutine(TableSheets.CoInitialize());
            yield return StartCoroutine(MainCanvas.instance.InitializeSecond());
            stage.objectPool.Initialize();
            yield return null;
            stage.dropItemFactory.Initialize();
            yield return null;
            AudioController.instance.Initialize();
            yield return null;
            agent.Initialize(AgentInitialized);

            Observable.EveryUpdate()
                .Where(_ => Input.GetMouseButtonUp(0))
                .Select(_ => Input.mousePosition)
                .Subscribe(PlayMouseOnClickVFX)
                .AddTo(gameObject);
        }

        private void AgentInitialized(bool succeed)
        {
            initialized = true;
            Debug.LogWarning(succeed);
            if (succeed)
            {
                Widget.Find<LoadingScreen>().Close();
                Widget.Find<Title>().Show();
            }
            else
            {
                Widget.Find<UpdatePopup>().Show();
            }
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
            if (GetComponent<Agent>() is null)
            {
                agent = gameObject.AddComponent<Agent>();
                agent.Initialize(AgentInitialized);
            }
        }

        public IEnumerator TearDown()
        {
            Destroy(GetComponent<Agent>());
            yield return new WaitForEndOfFrame();
        }

        #endregion

    }
}
