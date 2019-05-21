using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.Data;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Util;
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
        public static readonly int PixelPerUnit = 160;

        public Stage stage;

        protected override void Awake()
        {
            base.Awake();
            
            Screen.SetResolution(GameConfig.ScreenSize.x, GameConfig.ScreenSize.y, FullScreenMode.Windowed);
            Tables.instance.EmptyMethod();
            ActionManager.instance.InitAgent();
            ActionManager.instance.StartSystemCoroutines();
            
            LocalizationManager.Read();
            AudioController.instance.Initialize();
        }
    }
}
