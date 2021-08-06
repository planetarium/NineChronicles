using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CombinationMain : Widget
    {
        [SerializeField] private Button combineButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Image craftNotificationImage;

        protected override void Awake()
        {
            base.Awake();

            combineButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<Craft>().Show();
            });

            upgradeButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<UpgradeEquipment>().Show();
            });

            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            });

            CloseWidget = () =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            craftNotificationImage.enabled = Craft.SharedModel.HasNotification;
            base.Show(ignoreShowAnimation);
        }
    }
}
