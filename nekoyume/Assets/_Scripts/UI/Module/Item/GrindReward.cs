using Libplanet.Types.Assets;
using Nekoyume.Model.Item;
using Nekoyume.UI.Tween;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class GrindReward : MonoBehaviour
    {
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private DigitTextTweener rewardTweener;

        [SerializeField]
        private Button moreInfoButton;

        private long _cachedGrindingReward;

        public void SetCrystalReward(FungibleAssetValue reward)
        {
            iconImage.gameObject.SetActive(true);
            rewardTweener.gameObject.SetActive(true);
            moreInfoButton.gameObject.SetActive(false);

            iconImage.sprite = reward.GetIconSprite();

            var prevReward = _cachedGrindingReward;
            _cachedGrindingReward = (long)reward.MajorUnit;
            rewardTweener.PlayWithNotation(prevReward, _cachedGrindingReward);
        }

        public void SetItemReward((ItemBase itemBase, int count) reward)
        {
            iconImage.gameObject.SetActive(true);
            rewardTweener.gameObject.SetActive(true);
            moreInfoButton.gameObject.SetActive(false);

            iconImage.sprite = reward.itemBase.GetIconSprite();

            var prevReward = _cachedGrindingReward;
            _cachedGrindingReward = reward.count;
            rewardTweener.PlayWithNotation(prevReward, _cachedGrindingReward);
        }

        public void SetButton(UnityAction onClick)
        {
            iconImage.gameObject.SetActive(false);
            rewardTweener.gameObject.SetActive(false);
            moreInfoButton.gameObject.SetActive(true);

            moreInfoButton.onClick.AddListener(onClick);
        }
    }
}
