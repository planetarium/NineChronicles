using System;
using Nekoyume.Game.Character;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Lobby : MonoBehaviour
    {
        [SerializeField]
        private LobbyCharacter character;

        [SerializeField]
        private FriendCharacter friendCharacter;

        // [SerializeField]
        // private Avatar.Avatar avatar;

        public LobbyCharacter Character => character;

        public FriendCharacter FriendCharacter => friendCharacter;
        // public Avatar.Avatar Avatar => avatar;

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                StartCoroutine(RequestManager.instance.GetJson(
                    "https://m8g3g296a5.execute-api.us-east-2.amazonaws.com/Prod/v1/9c/avatars/all",
                    (s) => { Debug.Log($"{s}"); }));
            }
            else if (Input.GetKeyDown(KeyCode.X))
            {
                StartCoroutine(RequestManager.instance.GetJson(
                    "https://raw.githubusercontent.com/planetarium/NineChronicles.LiveAssets/main/Assets/Json/Event.json",
                    (s) => { Debug.Log($"{s}"); }));
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                var result = new SignatureResult();
                var url = "https://m8g3g296a5.execute-api.us-east-2.amazonaws.com/Prod/v1/9c/verify-agent";
                StartCoroutine(RequestManager.instance.Post(url, JsonUtility.ToJson(result), s =>
                {
                    Debug.Log($"[hash] : end");
                }));
            }
        }


        [Serializable]
        public class SignatureResult
        {
            public string agentAddress;
            public string agentSignTimestamp;
            public string agentSignature;
        }
#endif
    }
}
