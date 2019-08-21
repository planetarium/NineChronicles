using System.Collections;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Data;
using Nekoyume.Game.Controller;
using Nekoyume.TableData;
using Nekoyume.UI;
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
        
        public TableSheets TableSheets { get; private set; }

        protected override void Awake()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

            base.Awake();

            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Screen.SetResolution(GameConfig.ScreenSize.x, GameConfig.ScreenSize.y, FullScreenMode.Windowed);
            MainCanvas.instance.Initialize();
            Widget.Find<LoadingScreen>().Show(false);
            Tables.instance.Initialize();
            stage.objectPool.Initialize();
#if UNITY_EDITOR
            LocalizationManager.Read(languageType);
#else
            LocalizationManager.Read();
#endif
        }

        private IEnumerator Start()
        {
            yield return Addressables.InitializeAsync();
            TableSheets = new TableSheets();
            yield return StartCoroutine(TableSheets.CoInitialize());
            AgentController.Initialize(AgentInitialized);
            AudioController.instance.Initialize();
        }

        private void AgentInitialized(bool succeed)
        {
            Debug.LogWarning(succeed);
            if (succeed)
            {
                Widget.Find<LoadingScreen>()?.Close();
                Widget.Find<Title>()?.Show();
            }
            else
            {
                Widget.Find<UpdatePopup>()?.Show();
            }
        }
    }
}
