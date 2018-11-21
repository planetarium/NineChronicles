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
    }

    public class Game : MonoBehaviour
    {
        public Model.Avatar Avatar { get; private set; }

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

        private void OnUpdateAvatar(Model.Avatar avatar)
        {
            Avatar = avatar;
        }

        private void OnCreateAvatarRequired(object sender, EventArgs e)
        {
            MoveManager.Instance.CreateNovice(new Dictionary<string, string> {
                {"name", "tester"}
            });
        }

        private void OnAvatarLoaded(object sender, Model.Avatar avatar)
        {
            Avatar = avatar;
            Event.OnRoomEnter.Invoke();
        }

        private void OnSleep(object sender, Model.Avatar avatar)
        {
            Avatar = avatar;
        }

        private void OnHackAndSlash(object sender, Model.Avatar avatar)
        {
            Avatar = avatar;
        }
    }
}
