using System;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class RankingInfo : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI winCount;

        [SerializeField]
        private TextMeshProUGUI loseCount;

        [SerializeField]
        private TextMeshProUGUI remainTime;

        [SerializeField]
        private Slider remainTimeSlider;

        private long resetIndex;

        private void Awake()
        {
            remainTimeSlider.OnValueChangedAsObservable().Subscribe(OnSliderChange).AddTo(gameObject);
            remainTimeSlider.maxValue = GameConfig.DailyArenaInterval;
            remainTimeSlider.value = 0;

            WeeklyArenaStateSubject.WeeklyArenaState.ObserveOnMainThread().Subscribe(SetData).AddTo(gameObject);
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread().Subscribe(SetData).AddTo(gameObject);
        }

        private void OnEnable()
        {
            var weeklyArenaState = States.Instance.WeeklyArenaState;

            SetData(weeklyArenaState);
            var avatarAddress = States.Instance.CurrentAvatarState?.address;
            if (avatarAddress == null) return;

            var arenaInfos = weeklyArenaState
                .GetArenaInfos(avatarAddress.Value, 0, 0);

            if (arenaInfos.Count == 0)
            {
                winCount.text = "0";
                loseCount.text = "0";

                return;
            }

            var (rank, arenaInfo) = arenaInfos[0];

            var record = arenaInfo.ArenaRecord;

            winCount.text = record.Win.ToString();
            loseCount.text = record.Lose.ToString();
        }

        private void SetData(WeeklyArenaState value)
        {
            resetIndex = value.ResetIndex;

            remainTimeSlider.value = Game.Game.instance.Agent.BlockIndex - resetIndex;
        }

        private void SetData(long blockIndex)
        {
            remainTimeSlider.value = blockIndex - resetIndex;
        }

        private void OnSliderChange(float value)
        {
            var remainSecond = (GameConfig.DailyArenaInterval - value) * 15;
            var timeSpan = TimeSpan.FromSeconds(remainSecond);

            var remainString = $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            remainString = remainString.Replace("0h ", "");
            remainString = remainString.Replace("h 0m", "");

            remainTime.text = string.Format(L10nManager.Localize("UI_REMAININGTIME"), remainString,
                (int) value, GameConfig.DailyArenaInterval);
        }
    }
}
