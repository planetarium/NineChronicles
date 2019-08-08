using UnityEngine;

namespace Nekoyume.UI
{
    public class ExitPopup : SystemPopup
    {
        protected override void Awake()
        {
            base.Awake();

            CloseCallback = Application.Quit;
        }
    }
}
