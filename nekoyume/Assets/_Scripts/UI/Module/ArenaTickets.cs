using System;
using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class ArenaTickets : MonoBehaviour
    {
        [SerializeField]
        private Slider _slider;

        [SerializeField]
        private TextMeshProUGUI _fillText;

        [SerializeField]
        private TextMeshProUGUI _timespanText;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private void Awake()
        {
            _slider.normalizedValue = 0f;
            _fillText.text = string.Empty;
        }

        private void OnEnable()
        {
            UpdateSliderAndFillText(RxProps.ArenaInfoTuple.Value);
            RxProps.ArenaInfoTuple
                .SubscribeOnMainThreadWithUpdateOnce(UpdateSliderAndFillText)
                .AddTo(_disposables);

            UpdateTimespanText(RxProps.ArenaTicketProgress.Value);
            RxProps.ArenaTicketProgress
                .SubscribeOnMainThread()
                .Subscribe(UpdateTimespanText)
                .AddTo(_disposables);
        }

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
        }

        private void UpdateSliderAndFillText((ArenaInformation current, ArenaInformation next) tuple)
        {
            const int max = ArenaInformation.MaxTicketCount;
            var (current, _) = tuple;
            if (current is null)
            {
                _slider.normalizedValue = 1f;
                _fillText.text = $"{max}/{max}";
                return;
            }

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var currentRoundData = TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(blockIndex);
            var ticket = current.GetTicketCount(
                blockIndex,
                currentRoundData.StartBlockIndex,
                States.Instance.GameConfigState.DailyArenaInterval);
            var progress = (float)ticket / max;
            _slider.normalizedValue = progress;
            _fillText.text = $"{ticket}/{max}";
        }

        private void UpdateTimespanText((long beginning, long end, long progress) tuple)
        {
            var (bedinning, end, progress) = tuple;
            _timespanText.text = Util.GetBlockToTime(end - bedinning - progress);
        }
    }
}
