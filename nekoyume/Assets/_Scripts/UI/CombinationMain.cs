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

            });

            upgradeButton.onClick.AddListener(() =>
            {
                Find<UpgradeEquipment>().Show();
            });

            closeButton.onClick.AddListener(() =>
            {
                Close(true);
            });

            CloseWidget = () => Close(true);
        }
    }
}
