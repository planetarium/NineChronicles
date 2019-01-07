using System.Collections.Generic;
using Nekoyume.Action;
using Nekoyume.Model;
using Newtonsoft.Json.Linq;
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
            Action.ActionManager.Instance.StartSync();
//            MoveManager.Instance.StartSync();
            btnLogin.SetActive(false);
            text.text = "Connecting...";
        }
    }
}
