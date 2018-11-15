using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Move;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Stage : MonoBehaviour
    {
        private FollowCamera followCam = null;
        private GameObject background = null;
        private GameObject characters = null;

        public void Awake()
        {
            MoveManager.Instance.CreateAvatarRequried += OnCreateAvatarRequired;
            MoveManager.Instance.DidAvatarLoaded += OnAvatarLoaded;
            MoveManager.Instance.DidSleep += OnSleep;
        }

        public void Start()
        {
            InitCamera();
            LoadBackground("nest");
        }

        public void InitCamera()
        {
            followCam = Camera.main.gameObject.GetComponent<FollowCamera>();
            if (followCam == null)
            {
                followCam = Camera.main.gameObject.AddComponent<FollowCamera>();
            }
        }

        public void LoadBackground(string prefabName)
        {
            if (background != null)
            {
                if (background.name.Equals(prefabName))
                {
                    return;
                }
                Destroy(background);
                background = null;
            }
            var resName = $"Prefab/Background/{prefabName}";
            var prefab = Resources.Load<GameObject>(resName);
            if (prefab != null)
            {
                background = Instantiate(prefab, transform);
                background.name = prefabName;
            }
            var camPosition = followCam.transform.position;
            camPosition.x = 0;
            followCam.transform.position = camPosition;
        }

        private void LoadCharacter()
        {
            if (characters == null)
            {
                characters = new GameObject("characters");
                characters.transform.parent = transform;
            }
            var go = Instantiate(Resources.Load<GameObject>("Prefab/Character"), characters.transform);
            var character = go.GetComponent<Character>();
            character._Load(go, MoveManager.Instance.Avatar.class_);
        }

        private void OnCreateAvatarRequired(object sender, EventArgs e)
        {
            MoveManager.Instance.CreateNovice(new Dictionary<string, string> {
                {"name", "tester"}
            });
        }

        private void OnAvatarLoaded(object sender, Model.Avatar avatar)
        {
            LoadBackground("room");
            LoadCharacter();
            UI.Widget.Create<UI.Move>().Show();
        }

        private void OnSleep(object sender, Model.Avatar avatar)
        {
            Debug.Log("OnSleep");
            LoadBackground("room");
        }
    }
}
