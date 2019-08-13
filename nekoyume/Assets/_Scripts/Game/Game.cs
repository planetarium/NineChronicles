using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Data;
using Nekoyume.Game.Controller;
using Nekoyume.UI;
using UnityEngine;

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

        protected override void Awake()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

            base.Awake();

            Screen.SetResolution(GameConfig.ScreenSize.x, GameConfig.ScreenSize.y, FullScreenMode.Windowed);
            Tables.instance.Initialize();
#if UNITY_EDITOR
            LocalizationManager.Read(languageType);
#else
            LocalizationManager.Read();
#endif
            stage.objectPool.Initialize();
            MainCanvas.instance.Initialize();
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
