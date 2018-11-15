using System.Collections.Generic;
using Nekoyume.Model;
using Nekoyume.Move;
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
            joinModal.SetActive(false);
        }

        public void JoinClick()
        {
        }

        public void LoginClick()
        {
            MoveManager.Instance.StartSync();
            Close();
        }
    }
}
