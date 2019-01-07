using Nekoyume.Action;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Login : Widget
    {
        public GameObject btnLogin;
        public Text text;

        public void LoginClick()
        {
            ActionManager.Instance.StartSync();
            btnLogin.SetActive(false);
            text.text = "Connecting...";
        }
    }
}
