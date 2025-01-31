﻿using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.TableData;
using Nekoyume.ValueControlComponents.Shader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GeneratedApiNamespace.ArenaServiceClient;

namespace Nekoyume.UI.Module.Arena.Join
{
    using UniRx;

    public class ArenaJoinSeasonInfo : MonoBehaviour
    {
        [Flags][Serializable]
        public enum RewardType
        {
            None = 0,
            Medal = 1,
            NCG = 2,
            Food = 4,
            Costume = 8,
            Courage = 16,
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
        private GameObject _medalReward;

        [SerializeField]
        private GameObject _ncgReward;

        [SerializeField]
        private GameObject _foodReward;

        [SerializeField]
        private GameObject _costumeReward;

        [SerializeField]
        private GameObject _courageReward;
        [SerializeField]
        private List<Image> _currentRoundMedalImages;

        private readonly List<IDisposable> _disposablesFromOnEnable = new();

        private SeasonResponse _seasonData;

        private readonly Subject<Unit> _onSeasonBeginning = new();
        public IObservable<Unit> OnSeasonBeginning => _onSeasonBeginning;

        private readonly Subject<Unit> _onSeasonEnded = new();
        public IObservable<Unit> OnSeasonEnded => _onSeasonEnded;

        private void OnEnable()
        {
            Game.Game.instance.Agent.BlockIndexSubject
                .Subscribe(blockIndex =>
                    SetSliderAndText(_seasonData.GetSeasonProgress(blockIndex)))
                .AddTo(_disposablesFromOnEnable);
        }

        private void OnDisable()
        {
            _disposablesFromOnEnable.DisposeAllAndClear();
        }

        /// <param name="title">Season Name</param>
        /// <param name="seasonData"></param>
        /// <param name="rewardType">
        ///   Reward types.
        ///   (e.g., RewardType.None or RewardType.Medal | RewardType.NCG)
        /// </param>
        /// <param name="medalItemId">Season Medal ItemId on ItemSheet</param>
        public void SetData(
            string title,
            SeasonResponse seasonData,
            RewardType rewardType,
            int? medalItemId)
        {
            _titleText.text = title;
            _seasonData = seasonData;

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            SetSliderAndText(_seasonData.GetSeasonProgress(blockIndex));
            SetRewards(rewardType);
            SetMedalImages(medalItemId);
        }

        private void SetSliderAndText((long beginning, long end, long current) tuple)
        {
            var (beginning, end, current) = tuple;
            if (current > end)
            {
                _seasonProgressFillImage.enabled = false;
                _seasonProgressSliderFillText.enabled = false;

                if (current == end + 1)
                {
                    _onSeasonEnded.OnNext(Unit.Default);
                }

                return;
            }

            long remainBlock;
            if (current < beginning)
            {
                remainBlock = beginning - current;
                _seasonProgressFillImage.enabled = false;
            }
            else
            {
                var range = end - beginning;
                var progress = current - beginning;
                var sliderNormalizedValue = (float)progress / range;
                remainBlock = range - progress;
                _seasonProgressSlider.NormalizedValue = sliderNormalizedValue;
                _seasonProgressFillImage.enabled = true;

                if (current == beginning)
                {
                    _onSeasonBeginning.OnNext(Unit.Default);
                }
            }

            var remainTimeText = $"{remainBlock:#,0}({remainBlock.BlockRangeToTimeSpanString()})";
            _seasonProgressSliderFillText.text = remainTimeText;
            _seasonProgressSliderFillText.enabled = true;
        }

        private void SetRewards(RewardType rewardType)
        {
            _costumeReward.SetActive(false);
            _medalReward.SetActive(false);
            _ncgReward.SetActive(false);
            _foodReward.SetActive(false);
            _courageReward.SetActive(false);

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

            if ((rewardType & RewardType.Courage) == RewardType.Courage)
            {
                _courageReward.SetActive(true);
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
