using UnityEngine;
using Nekoyume.Move;
using System;
using System.Collections.Generic;

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
        private void Start()
        {
            MoveManager.Instance.CreateAvatarRequried += OnCreateAvatarRequired;
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
            Event.OnStageEnter.Invoke(avatar);
        }

        private void OnSleep(object sender, Model.Avatar avatar)
        {
            Event.OnStageEnter.Invoke(avatar);
        }

        private void OnHackAndSlash(object sender, Model.Avatar avatar)
        {
            Event.OnStageEnter.Invoke(avatar);
        }
    }
}
