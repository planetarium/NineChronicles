using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class CombinationMain : Widget
    {
        [SerializeField] private Button combineButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button closeButton;

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
    }
}
