using System;
using System.Collections.Generic;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.ValueControlComponents.Shader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Arena.Join
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
            Costume = 8,
        }

        [SerializeField]
        private TextMeshProUGUI _titleText;

        [SerializeField]
        private ShaderPropertySlider _seasonProgressSlider;

        [SerializeField]
        private Image _seasonProgressFillImage;

        [SerializeField]
        private TextMeshProUGUI _seasonProgressSliderFillText;

        [SerializeField]
        private GameObject _conditionsContainer;

        [SerializeField]
        private Image _conditionsSliderFillArea;

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
        private GameObject _costumeReward;

        [SerializeField]
        private List<Image> _currentRoundMedalImages;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private ArenaSheet.RoundData _roundData;
        
        private void OnEnable()
        {
            Game.Game.instance.Agent.BlockIndexSubject
                .Subscribe(blockIndex =>
                    SetSliderAndText(_roundData.GetSeasonProgress(blockIndex)))
                .AddTo(gameObject);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        /// <param name="title">Season Name</param>
        /// <param name="roundData"></param>
        /// <param name="conditions">Season Conditions</param>
        /// <param name="rewardType">
        ///   Reward types.
        ///   (e.g., RewardType.None or RewardType.Medal | RewardType.NCG)
        /// </param>
        /// <param name="medalItemId">Season Medal ItemId on ItemSheet</param>
        public void SetData(
            string title,
            ArenaSheet.RoundData roundData,
            (int max, int current)? conditions,
            RewardType rewardType,
            int? medalItemId)
        {
            _disposables.DisposeAllAndClear();
            _titleText.text = title;
            _roundData = roundData;

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            SetSliderAndText(_roundData.GetSeasonProgress(blockIndex));
            SetConditions(conditions);
            SetRewards(rewardType);
            SetMedalImages(medalItemId);
        }

        private void SetSliderAndText((long beginning, long end, long current) tuple)
        {
            var (beginning, end, current) = tuple;
            if (current < beginning)
            {
                _seasonProgressFillImage.enabled = false;
                _seasonProgressSliderFillText.enabled = false;
                return;
            }

            if (current > end)
            {
                _seasonProgressFillImage.enabled = false;
                _seasonProgressSliderFillText.enabled = false;
                return;
            }

            var range = end - beginning;
            var progress = current - beginning;
            var sliderNormalizedValue = (float)progress / range;
            _seasonProgressSlider.NormalizedValue = sliderNormalizedValue;
            _seasonProgressFillImage.enabled = true;
            _seasonProgressSliderFillText.text = Util.GetBlockToTime(range - progress);
            _seasonProgressSliderFillText.enabled = true;
        }

        private void SetConditions((int max, int current)? conditions)
        {
            if (!conditions.HasValue)
            {
                _conditionsContainer.SetActive(false);
                return;
            }

            var (max, current) = conditions.Value;
            _conditionsSliderFillArea.fillAmount = (float)current / max;
            _conditionsSliderFillText.text =
                string.Format(_conditionsSliderFillTextFormat, current, max);
            _conditionsContainer.SetActive(true);
        }

        private void SetRewards(RewardType rewardType)
        {
            _costumeReward.SetActive(false);
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

            if ((rewardType & RewardType.Costume) == RewardType.Costume)
            {
                _costumeReward.SetActive(true);
            }
        }

        private void SetMedalImages(int? medalItemId)
        {
            if (medalItemId.HasValue)
            {
                var medalSprite = SpriteHelper.GetItemIcon(medalItemId.Value);
                foreach (var medalImage in _currentRoundMedalImages)
                {
                    medalImage.overrideSprite = medalSprite;
                    medalImage.enabled = true;
                }

                return;
            }

            foreach (var medalImage in _currentRoundMedalImages)
            {
                medalImage.enabled = false;
            }
        }
    }
}
