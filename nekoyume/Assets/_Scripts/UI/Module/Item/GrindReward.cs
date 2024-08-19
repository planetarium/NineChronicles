using Libplanet.Types.Assets;
using Nekoyume.Model.Item;
using Nekoyume.UI.Tween;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class GrindReward : MonoBehaviour
    {
        private class RewardType
        {
            public string FavTicker;
            public int ItemId;
            public bool IsButton;

            public RewardType()
            {
                Reset();
            }

            public void Reset()
            {
                FavTicker = null;
                ItemId = 0;
                IsButton = false;
            }
        }

        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private DigitTextTweener rewardTweener;

        [SerializeField]
        private Button moreInfoButton;

        [SerializeField]
        private Animator animator;

        private static readonly int Show = Animator.StringToHash("Show");
        private readonly RewardType _cachedRewardType = new RewardType();
        private long _cachedGrindingReward;

        public void ShowWithFavReward(FungibleAssetValue reward)
        {
            gameObject.SetActive(true);
            if (reward.Currency.Ticker != _cachedRewardType.FavTicker)
            {
                _cachedRewardType.FavTicker = reward.Currency.Ticker;
                _cachedGrindingReward = 0;
                // To avoid GrindModule animation conflict
                Observable.NextFrame().Subscribe(_ => animator.SetTrigger(Show));
            }

            iconImage.gameObject.SetActive(true);
            rewardTweener.gameObject.SetActive(true);
            moreInfoButton.gameObject.SetActive(false);

            iconImage.sprite = reward.GetIconSprite();
            var prevReward = _cachedGrindingReward;
            _cachedGrindingReward = (long)reward.MajorUnit;
            rewardTweener.PlayWithNotation(prevReward, _cachedGrindingReward);
        }

        public void ShowWithItemReward((ItemBase itemBase, int count) reward)
        {
            gameObject.SetActive(true);
            if (reward.itemBase.Id != _cachedRewardType.ItemId)
            {
                _cachedRewardType.ItemId = reward.itemBase.Id;
                _cachedGrindingReward = 0;
                Observable.NextFrame().Subscribe(_ => animator.SetTrigger(Show));
            }

            iconImage.gameObject.SetActive(true);
            rewardTweener.gameObject.SetActive(true);
            moreInfoButton.gameObject.SetActive(false);

            iconImage.sprite = reward.itemBase.GetIconSprite();
            var prevReward = _cachedGrindingReward;
            _cachedGrindingReward = reward.count;
            rewardTweener.PlayWithNotation(prevReward, _cachedGrindingReward);
        }

        public void ShowWithButton(UnityAction onClick)
        {
            gameObject.SetActive(true);
            if (!_cachedRewardType.IsButton)
            {
                _cachedRewardType.IsButton = true;
                Observable.NextFrame().Subscribe(_ => animator.SetTrigger(Show));
            }

            iconImage.gameObject.SetActive(false);
            rewardTweener.gameObject.SetActive(false);
            moreInfoButton.gameObject.SetActive(true);

            moreInfoButton.onClick.AddListener(onClick);
        }

        public void HideAndReset()
        {
            gameObject.SetActive(false);
            _cachedRewardType.Reset();
        }
    }
}
