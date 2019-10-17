using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ActionFailPopup : SystemPopup
    {
        public Text contentTextField;
        public void Show(string msg)
        {
            contentTextField.text = msg;
            SubmitCallback = Application.Quit;
            base.Show();
        }
    }
}
