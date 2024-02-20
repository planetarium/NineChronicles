using UnityEngine;

namespace Nekoyume.UI.Module.Lobby
{
    public class CollectionMenu : MainMenu
    {
        [SerializeField] private GameObject notification;

        private void OnEnable()
        {
            var hasNotification = false;
            if (Game.Game.instance.IsInitialized &&
                Game.Game.instance.States.CurrentAvatarState != null)
            {
                // Check initialized Collection and init collection models
                var collection = Widget.Find<Collection>();
                collection.TryInitialize();

                hasNotification = collection.HasNotification;
            }

            notification.SetActive(hasNotification);
        }
    }
}
