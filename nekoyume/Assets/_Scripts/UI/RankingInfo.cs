using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class RankingInfo : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI winCount = null;

        [SerializeField]
        private TextMeshProUGUI loseCount = null;

        [SerializeField]
        private TextMeshProUGUI remainTime = null;

        [SerializeField]
        private TextMeshProUGUI remainTitle = null;

        [SerializeField]
        private Slider remainTimeSlider = null;

        [SerializeField]
        private GameObject rewardObject;

        [SerializeField]
        private TextMeshProUGUI rewardText;

        [SerializeField]
        private TextMeshProUGUI blockCountText;

        [SerializeField]
        private GameObject seasonEnable;

        [SerializeField]
        private GameObject seasonDisable;

        private long _resetIndex;

        private readonly List<IDisposable> _disposablesFromOnEnable = new List<IDisposable>();

        private void Awake()
        {
            remainTimeSlider.OnValueChangedAsObservable()
                .Subscribe(OnSliderChange)
                .AddTo(gameObject);
            remainTimeSlider.maxValue = States.Instance.GameConfigState.DailyArenaInterval;
            remainTimeSlider.value = 0;
        }

        private void OnEnable()
        {
            Game.Game.instance.Agent.BlockIndexSubject
                .Subscribe(SetBlockIndex)
                .AddTo(_disposablesFromOnEnable);
            WeeklyArenaStateSubject.WeeklyArenaState
                .Subscribe(SetWeeklyArenaState)
                .AddTo(_disposablesFromOnEnable);

            var weeklyArenaState = States.Instance.WeeklyArenaState;
            SetWeeklyArenaState(weeklyArenaState);
            SetBlockIndex(Game.Game.instance.Agent.BlockIndex);
        }

        private void OnDisable()
        {
            _disposablesFromOnEnable.DisposeAllAndClear();
        }

        private void SetBlockIndex(long blockIndex)
        {
            remainTimeSlider.value = blockIndex - _resetIndex;

            var isActive = EventManager.TryGetArenaSeasonInfo(blockIndex, out var info);
            seasonEnable.SetActive(isActive);
            seasonDisable.SetActive(!isActive);
            rewardObject.SetActive(isActive);
            if (isActive)
            {
                rewardText.text = info.RewradNcg % 1000 == 0
                    ? $"{info.RewradNcg / 1000}k"
                    : $"{info.RewradNcg}";
                var start = info.StartBlockIndex % 1000 == 0
                    ? $"{info.StartBlockIndex / 1000}k"
                    : $"{info.StartBlockIndex}";
                var end = info.EndBlockIndex % 1000 == 0
                    ? $"{info.EndBlockIndex / 1000}k"
                    : $"{info.EndBlockIndex}";
                blockCountText.text = $"{start}-{end}";
            }
        }

        private void SetWeeklyArenaState(WeeklyArenaState weeklyArenaState)
        {
            _resetIndex = weeklyArenaState.ResetIndex;
            remainTimeSlider.value = Game.Game.instance.Agent.BlockIndex - _resetIndex;
            UpdateArenaInfo(weeklyArenaState);
        }

        private void UpdateArenaInfo(WeeklyArenaState weeklyArenaState)
        {
            var avatarAddress = States.Instance.CurrentAvatarState?.address;
            if (avatarAddress == null)
            {
                winCount.text = "-";
                loseCount.text = "-";
                return;
            }

            var arenaInfos = weeklyArenaState
                .GetArenaInfos(avatarAddress.Value, 0, 0);

            if (arenaInfos.Count == 0)
            {
                winCount.text = "-";
                loseCount.text = "-";
                return;
            }

            var record = arenaInfos[0].arenaInfo.ArenaRecord;
            winCount.text = record.Win.ToString();
            loseCount.text = record.Lose.ToString();
        }

        private void OnSliderChange(float value)
        {
            var gameConfigState = States.Instance.GameConfigState;
            var remainBlock = gameConfigState.DailyArenaInterval - value;
            var time = Util.GetBlockToTime((int)remainBlock);
            remainTitle.text = L10nManager.Localize("UI_REMAINING_TIME_ONLY");
            remainTime.text = string.Format(
                L10nManager.Localize("UI_ABOUT"),
                time,
                (int)value, gameConfigState.DailyArenaInterval);
        }
    }
}
