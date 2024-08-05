using Libplanet.Types.Assets;
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

        private FungibleAssetValue _cachedGrindingReward;

        public void SetCrystalReward(FungibleAssetValue reward)
        {
            iconImage.gameObject.SetActive(true);
            rewardTweener.gameObject.SetActive(true);
            moreInfoButton.gameObject.SetActive(false);

            iconImage.sprite = reward.GetIconSprite();

            var prevCrystalReward = _cachedGrindingReward.MajorUnit;
            _cachedGrindingReward = reward;
            rewardTweener.PlayWithNotation(
                (long)prevCrystalReward,
                (long)_cachedGrindingReward.MajorUnit);
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
