using UnityEngine;

namespace Nekoyume.UI.Module.Lobby
{
    public class EventRewardMenu : MainMenu
    {
        [SerializeField]
        private GameObject notification;

        private void OnEnable()
        {
            SetNotification();

            void SetNotification()
            {
                var hasNotification = false;
                // Todo: Fill this

                notification.SetActive(hasNotification);
            }
        }
    }
}
