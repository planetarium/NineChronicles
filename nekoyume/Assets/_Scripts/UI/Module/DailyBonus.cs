using System;
using System.Collections.Generic;
using Nekoyume.BlockChain;
using Nekoyume.Model;
using Nekoyume.State;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class DailyBonus : MonoBehaviour
    {
        public TextMeshProUGUI text;
        public Slider slider;
        public Button button;
        public CanvasGroup canvasGroup;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private long _nextBlockIndex;
        private bool _updateEnable;
        private Animation _animation;

        #region Mono

        private void Awake()
        {
            slider.maxValue = DailyBlockState.UpdateInterval;
            text.text = $"0 / {DailyBlockState.UpdateInterval}";
            button.interactable = false;
            _animation = GetComponent<Animation>();
        }

        private void OnEnable()
        {
            Game.Game.instance.agent.blockIndex.ObserveOnMainThread().Subscribe(SetIndex).AddTo(_disposables);
            ReactiveCurrentAvatarState.NextDailyRewardIndex.Subscribe(SetNextBlockIndex).AddTo(_disposables);
            canvasGroup.alpha = 0;
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        #endregion

        private void SetIndex(long index)
        {
            if (_updateEnable)
            {
                var a = _nextBlockIndex - DailyBlockState.UpdateInterval;
                var value = Math.Min(index - a, DailyBlockState.UpdateInterval);
                text.text = $"{value} / {DailyBlockState.UpdateInterval}";
                slider.value = value;

                var isFull = value == DailyBlockState.UpdateInterval;
                button.interactable = isFull;
                canvasGroup.interactable = isFull;
                if (isFull)
                {
                    _animation.Play();
                }
            }
        }

        public void GetReward()
        {
            ActionManager.instance.DailyReward();
            _nextBlockIndex += DailyBlockState.UpdateInterval;
            slider.value = 0;
            text.text = $"0 / {DailyBlockState.UpdateInterval}";
            button.interactable = false;
            _updateEnable = false;
            _animation.Stop();
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
        }

        private void SetNextBlockIndex(long index)
        {
            _nextBlockIndex = index;
            slider.value = 0;
            text.text = $"0 / {DailyBlockState.UpdateInterval}";
            _updateEnable = true;
        }
    }
}
