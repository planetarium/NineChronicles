using System;
using System.Collections;
using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.Game.Factory;
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
        static public readonly int PixelPerUnit = 160;

        private void Awake()
        {
            LocalizationManager.Read();
        }

        private void Start()
        {
            ActionManager.Instance.CreateAvatarRequired += OnCreateAvatarRequired;
            ActionManager.Instance.DidAvatarLoaded += OnAvatarLoaded;
        }

        private void OnCreateAvatarRequired(object sender, EventArgs e)
        {
            StartCoroutine(CreateAvatarAsync());
        }

        private IEnumerator CreateAvatarAsync()
        {
            yield return new WaitForEndOfFrame();
            var objectPool = GetComponentInChildren<Util.ObjectPool>();
            var player = objectPool.Get<Character.Player>();
            player.transform.position = new Vector2(-0.8f, 0.46f);
        }

        private void OnAvatarLoaded(object sender, Model.Avatar avatar)
        {
            Event.OnRoomEnter.Invoke();
            Widget.Find<Login>().Close();
        }
    }
}
