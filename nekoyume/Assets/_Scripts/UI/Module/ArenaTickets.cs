using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.Model.State;
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
            UpdateSliderAndFillText(RxProps.ArenaInfo.Value);
            RxProps.ArenaInfo
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

        private void UpdateSliderAndFillText(ArenaInfo info)
        {
            if (info is null)
            {
                return;
            }

            var progress = (float)info.DailyChallengeCount / GameConfig.ArenaChallengeCountMax;
            _slider.normalizedValue = progress;
            _fillText.text = $"{info.DailyChallengeCount}/{GameConfig.ArenaChallengeCountMax}";
        }

        private void UpdateTimespanText((long bedinning, long end, long progress) tuple)
        {
            var (_, _, progress) = tuple;
            _timespanText.text = Util.GetBlockToTime(progress);
        }
    }
}
