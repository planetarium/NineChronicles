using System.Collections.Generic;
using Nekoyume.Model;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public class Login : Widget
    {
        public GameObject joinModal;
        public GameObject btnLogin;


        private void Start()
        {
            bool hasPrivateKey = !string.IsNullOrEmpty(Network.NetworkManager.Instance.privateKey);
            joinModal.SetActive(!hasPrivateKey);
            btnLogin.SetActive(hasPrivateKey);
        }

        public void JoinClick()
        {
        }

        public void LoginClick()
        {
            btnLogin.SetActive(false);
            GameObject stageObj = GameObject.FindGameObjectWithTag("Stage");
            Game.Stage stage = stageObj.GetComponent<Game.Stage>();

            // FIXME replace to input
            stage.User.CreateNovice(new Dictionary<string, string>
            {
                {"name", "test"}
            });
            stage.User.FirstClass(CharacterClass.Swordman.ToString().ToLower());
        }
    }
}
