using UnityEngine;

namespace Nekoyume.UI.Module.Lobby
{
    public class DccMenu : MainMenu
    {
        [SerializeField]
        private GameObject notification;

        private void Start()
        {
            // TODO: subscribe cost condition about pet enhancement
            notification.SetActive(true);
        }
    }
}
