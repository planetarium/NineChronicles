using System.Linq;
using Libplanet.Types.Assets;
using Nekoyume.Model.Item;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class GrindRewardPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private GrindRewardScroll scroll;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() => Close());
        }

        public void Show(
            FungibleAssetValue[] favRewards,
            (ItemBase itemBase, int count)[] itemRewards,
            bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            var models = favRewards.Select(favReward => new GrindRewardCell.Model(favReward)).ToList();
            models.AddRange(itemRewards.Select(itemReward => new GrindRewardCell.Model(itemReward)));

            scroll.UpdateData(models);
        }
    }
}
