using Nekoyume.Move;
using System;
using System.Collections.Generic;
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
            Assets.SimpleLocalization.LocalizationManager.Read();
        }

        private void Start()
        {
            MoveManager.Instance.CreateAvatarRequired += OnCreateAvatarRequired;
            MoveManager.Instance.DidAvatarLoaded += OnAvatarLoaded;
            MoveManager.Instance.DidSleep += OnSleep;
        }

        private void OnCreateAvatarRequired(object sender, EventArgs e)
        {
            MoveManager.Instance.CreateNovice(new Dictionary<string, string> {
                {"name", "tester"}
            });
        }

        private void OnAvatarLoaded(object sender, Model.Avatar avatar)
        {
            Event.OnRoomEnter.Invoke();
        }

        private void OnSleep(object sender, Model.Avatar avatar)
        {
        }

    }
}
