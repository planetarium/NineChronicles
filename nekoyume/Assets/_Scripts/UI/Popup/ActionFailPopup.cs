using UnityEngine;

namespace Nekoyume.UI
{
    public class ActionFailPopup : SystemPopup
    {
        public override void Show()
        {
            CloseCallback = Application.Quit;
            base.Show();
        }
    }
}
