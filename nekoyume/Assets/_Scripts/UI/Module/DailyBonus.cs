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
        private bool _updateEnable;
        private bool _isFull;
        private Animation _animation;
        private long _receivedIndex;

        #region Mono

        private void Awake()
        {
            slider.maxValue = DailyBlockState.UpdateInterval;
            text.text = $"0 / {DailyBlockState.UpdateInterval}";
            button.interactable = false;
            _animation = GetComponent<Animation>();
            _updateEnable = true;
        }

        private void OnEnable()
        {
            Game.Game.instance.agent.blockIndex.ObserveOnMainThread().Subscribe(SetIndex).AddTo(_disposables);
            ReactiveCurrentAvatarState.DailyRewardReceivedIndex.Subscribe(SetReceivedIndex).AddTo(_disposables);
            canvasGroup.alpha = 0;
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        #endregion

        private void SetIndex(long index)
        {
            var min = Math.Max(index - _receivedIndex, 0);
            var value = Math.Min(min, DailyBlockState.UpdateInterval);
            text.text = $"{value} / {DailyBlockState.UpdateInterval}";
            slider.value = value;

            _isFull = value >= DailyBlockState.UpdateInterval;
            button.interactable = _isFull;
            canvasGroup.interactable = _isFull;
            if (_isFull && _updateEnable)
            {
                _animation.Play();
            }
        }

        public void GetReward()
        {
            _updateEnable = false;
            ActionManager.instance.DailyReward().Subscribe(_ => { _updateEnable = true; });
            _animation.Stop();
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            button.interactable = false;
            _isFull = false;
        }

        private void SetReceivedIndex(long index)
        {
            if (index != _receivedIndex)
            {
                _receivedIndex = index;
                _updateEnable = true;
            }
        }
    }
}
