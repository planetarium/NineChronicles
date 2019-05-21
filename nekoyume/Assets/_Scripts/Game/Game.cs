using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.Data;
using Nekoyume.Game.Controller;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game
{
    public static class GameExtensionMethods
    {
        static Transform root = null;

        public static T GetRootComponent<T>(this MonoBehaviour behaviour)
        {
            if (root == null)
                root = behaviour.transform.root;

            return root.GetComponent<T>();
        }

        public static T GetOrAddComponent<T>(this MonoBehaviour behaviour) where T : MonoBehaviour
        {
            T t = behaviour.GetComponent<T>();
            if (t)
                return t;
            return behaviour.gameObject.AddComponent<T>();
        }
    }

    public class Game : MonoBehaviour
    {
        public static readonly int PixelPerUnit = 160;

        private void Awake()
        {
            Screen.SetResolution(GameConfig.ScreenSize.x, GameConfig.ScreenSize.y, FullScreenMode.Windowed);
            Tables.instance.EmptyMethod();
            ActionManager.instance.InitAgent();
            ActionManager.instance.StartSystemCoroutines();
            
            LocalizationManager.Read();
            AudioController.instance.Initialize();
        }
    }
}
