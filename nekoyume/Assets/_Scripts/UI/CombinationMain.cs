using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class CombinationMain : Widget
    {
        [SerializeField] private Button combineButton;
        [SerializeField] private Button upgradeButton;

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
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
        }
    }
}
