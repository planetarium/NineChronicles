using System.Collections;
using System.Collections.Generic;
using Nekoyume.Model;
using Nekoyume.Network.Agent;
using Nekoyume.Move;
using Planetarium.Crypto.Extension;
using Planetarium.Crypto.Keys;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System;

namespace Nekoyume.Game
{
    public class Stage : MonoBehaviour
    {
        public FollowCamera followCam = null;
        public GameObject avatar;
        public GameObject background = null;

        private string zone;
        public User User { get; private set; }

        public void Awake()
        {
            var serverUrl = "http://localhost:4000";
            var privateKeyHex = PlayerPrefs.GetString("private_key", "");

            PrivateKey privateKey = null;
            if (string.IsNullOrEmpty(privateKeyHex))
            {
                privateKey = PrivateKey.Generate();
                PlayerPrefs.SetString("private_key", privateKey.Bytes.Hex());
            }
            else
            {
                privateKey = PrivateKey.FromBytes(privateKeyHex.ParseHex());
            }
            var agent = new Agent(serverUrl, privateKey);
            Debug.Log(string.Format("User Adress: 0x{0}", agent.UserAddress.Hex()));
            User = new User(agent);
            User.DidAvatarLoaded += OnAvatarLoaded;
            User.DidSleep += OnSleep;
        }

        public void Start()
        {
            InitCamera();
            LoadBackground("nest");
            StartCoroutine(User.Sync());
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
            string resName = string.Format("Prefab/Background/{0}", prefabName);
            GameObject prefab = Resources.Load<GameObject>(resName);
            if (prefab != null)
            {
                background = Instantiate(prefab, transform);
                background.name = prefabName;
            }
            var camPosition = followCam.transform.position;
            camPosition.x = 0;
            followCam.transform.position = camPosition;
        }

        public void Move()
        {
            var character = avatar.GetComponent<Character>();
            StartCoroutine(character.Walk());
            followCam.target = avatar.transform;
            LoadBackground(zone);
        }

        public void OnHackAndSlash(Network.Response.LastStatus response)
        {
            var character = avatar.GetComponent<Character>();
            StartCoroutine(character.Stop());
            StartCoroutine(ProcessStatus(response));
        }

        private IEnumerator ProcessStatus(Network.Response.LastStatus lastStatus)
        {
            foreach (var battleStatus in lastStatus.status)
            {
                var statusType = battleStatus.GetStatusType();
                if (statusType != null)
                {
                    var status = (Status.Base)System.Activator.CreateInstance(statusType);
                    if (status != null)
                    {
                        yield return status.Execute(this, battleStatus);
                    }
                }
            }
        }

        public void Home()
        {

        }

        private void OnSleep(object sender, Model.Avatar avatar)
        {
            LoadBackground("nest");
        }

        private void OnAvatarLoaded(object sender, Model.Avatar avatar)
        {
            LoadBackground("room");
            zone = avatar.zone;
            var character = this.avatar.GetComponent<Character>();
            StartCoroutine(character.Load(this.avatar, avatar.class_));
            UI.Widget.Create<UI.Move>().Show();
        }
    }
}
