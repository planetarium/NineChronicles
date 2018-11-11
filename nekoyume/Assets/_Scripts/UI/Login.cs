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
            joinModal.SetActive(false);
            var nameField = joinModal.GetComponentInChildren<InputField>();
            new Network.Request.Join()
            {
                name = nameField.text,
                ResponseCallback = OnLogin
            }.Send();
        }

        public void LoginClick()
        {
            btnLogin.SetActive(false);
            GameObject stageObj = GameObject.FindGameObjectWithTag("Stage");
            Game.Stage stage = stageObj.GetComponent<Game.Stage>();
            stage.OnLogin();
        }

        public void OnLogin(Network.Response.Login response)
        {
            GameObject stageObj = GameObject.FindGameObjectWithTag("Stage");
            Game.Stage stage = stageObj.GetComponent<Game.Stage>();
            if (response.result == Network.ResultCode.OK)
            {
                stage.OnLogin(response);
                Close();
            }
            else
            {
                Debug.Log(response.message);
                joinModal.SetActive(true);
            }
        }
    }
}
