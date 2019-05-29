using Assets.SimpleLocalization;
using Nekoyume.Data;
using Nekoyume.Game.Controller;
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
        public Stage stage;
        
        protected override void Awake()
        {
            base.Awake();
            
            Screen.SetResolution(GameConfig.ScreenSize.x, GameConfig.ScreenSize.y, FullScreenMode.Windowed);
            Tables.instance.EmptyMethod();
            AgentController.Initialize();
            
            LocalizationManager.Read();
            AudioController.instance.Initialize();
            
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        }
    }
}
