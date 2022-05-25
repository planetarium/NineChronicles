using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Arena
{
    using UniRx;

    public class ArenaJoinSeasonInfo : MonoBehaviour
    {
        [Flags, Serializable]
        public enum RewardType
        {
            None = 0,
            Medal = 1,
            NCG = 2,
            Food = 4,
        }

        [SerializeField]
        private TextMeshProUGUI _titleText;

        [SerializeField]
        private Slider _seasonProgressSlider;

        [SerializeField]
        private TextMeshProUGUI _seasonProgressSliderFillText;

        [SerializeField]
        private GameObject _conditionsContainer;

        [SerializeField]
        private Slider _conditionsSlider;

        [SerializeField]
        private TextMeshProUGUI _conditionsSliderFillText;

        [SerializeField]
        private string _conditionsSliderFillTextFormat;

        [SerializeField]
        private GameObject _medalReward;

        [SerializeField]
        private GameObject _ncgReward;

        [SerializeField]
        private GameObject _foodReward;
        
        [SerializeField]
        private List<Image> _medalImages;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private void OnEnable()
        {
            UpdateSliderAndText(RxProps.ArenaProgress.Value);
            RxProps.ArenaProgress
                .SubscribeOnMainThread()
                .Subscribe(UpdateSliderAndText)
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        /// <param name="title">Season Name</param>
        /// <param name="medalId">Season Medal Id on ItemSheet</param>
        /// <param name="conditions">Season Conditions</param>
        /// <param name="rewardType">
        ///   Reward types.
        ///   (e.g., RewardType.None or RewardType.Medal | RewardType.NCG)
        /// </param>
        public void SetData(
            string title,
            int medalId,
            (int max, int current)? conditions,
            RewardType rewardType)
        {
            _titleText.text = title;
            UpdateConditions(conditions);
            UpdateRewards(rewardType);
            UpdateMedalImages(SpriteHelper.GetItemIcon(medalId));
        }

        private void UpdateSliderAndText((long beginning, long end, long progress) tuple)
        {
            var (beginning, end, progress) = tuple;
            var range = end - beginning;
            var sliderNormalizedValue = (float)progress / range;
            _seasonProgressSlider.normalizedValue = sliderNormalizedValue;
            _seasonProgressSliderFillText.text = Util.GetBlockToTime(progress);
        }

        private void UpdateConditions((int max, int current)? conditions)
        {
            if (!conditions.HasValue)
            {
                _conditionsContainer.SetActive(false);
                return;
            }

            var (max, current) = conditions.Value;
            _conditionsSlider.normalizedValue = (float)current / max;
            _conditionsSliderFillText.text =
                string.Format(_conditionsSliderFillTextFormat, current, max);
            _conditionsContainer.SetActive(true);
        }

        private void UpdateRewards(RewardType rewardType)
        {
            _medalReward.SetActive(false);
            _ncgReward.SetActive(false);
            _foodReward.SetActive(false);

            if (rewardType == RewardType.None)
            {
                return;
            }

            if ((rewardType & RewardType.Medal) == RewardType.Medal)
            {
                _medalReward.SetActive(true);
            }
            
            if ((rewardType & RewardType.NCG) == RewardType.NCG)
            {
                _ncgReward.SetActive(true);
            }
            
            if ((rewardType & RewardType.Food) == RewardType.Food)
            {
                _foodReward.SetActive(true);
            }
        }

        private void UpdateMedalImages(Sprite medalSprite)
        {
            foreach (var medalImage in _medalImages)
            {
                medalImage.overrideSprite = medalSprite;
            }
        }
    }
}
