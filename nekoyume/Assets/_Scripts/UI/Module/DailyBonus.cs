using System;
using System.Collections;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using JetBrains.Annotations;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.State;
using Nekoyume.UI.Module.Common;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class DailyBonus : AlphaAnimateModule
    {
        [SerializeField]
        private TextMeshProUGUI text = null;

        [SerializeField]
        private Slider slider = null;

        [SerializeField]
        private Button button = null;

        [SerializeField]
        private CanvasGroup additiveCanvasGroup = null;

        [SerializeField]
        private RectTransform tooltipArea = null;

        [SerializeField]
        private Transform boxImageTransform = null;

        [SerializeField, CanBeNull]
        private ActionPoint actionPoint = null;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private bool _updateEnable;
        private bool _isFull;
        private Animator _animator;

        private long _currentBlockIndex;
        private long _rewardReceivedBlockIndex;
        private long _currentValue;

        private Coroutine _coUpdateSlider;

        private static readonly int IsFull = Animator.StringToHash("IsFull");
        private static readonly int Reward = Animator.StringToHash("GetReward");

        #region Mono

        private void Awake()
        {
            slider.maxValue = GameConfig.DailyRewardInterval;
            text.text = $"0 / {GameConfig.DailyRewardInterval}";
            slider.value = 0;
            button.interactable = false;
            _animator = GetComponent<Animator>();
            _updateEnable = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            additiveCanvasGroup.alpha = 0f;

            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(blockIndex => SetBlockIndex(blockIndex, true)).AddTo(_disposables);
            ReactiveAvatarState.DailyRewardReceivedIndex
                .Subscribe(blockIndex => SetRewardReceivedBlockIndex(blockIndex, true)).AddTo(_disposables);
        }

        protected override void OnDisable()
        {
            _disposables.DisposeAllAndClear();
            base.OnDisable();
        }

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
        }

        #endregion

        private void SetBlockIndex(long blockIndex, bool useLerp)
        {
            _currentBlockIndex = blockIndex;
            UpdateSlider(useLerp);
        }

        private void SetRewardReceivedBlockIndex(long rewardReceivedBlockIndex, bool useLerp)
        {
            _rewardReceivedBlockIndex = rewardReceivedBlockIndex;
            SetBlockIndex(_currentBlockIndex, useLerp);
        }

        private void UpdateSlider(bool useLerp)
        {
            var endValue = Math.Min(
                Math.Max(0, _currentBlockIndex - _rewardReceivedBlockIndex),
                GameConfig.DailyRewardInterval);
            
            if (!(_coUpdateSlider is null))
            {
                StopCoroutine(_coUpdateSlider);
                _coUpdateSlider = null;
            }

            if (useLerp)
            {
                _coUpdateSlider = StartCoroutine(CoUpdateSlider(endValue));
            }
            else
            {
                _currentValue = endValue;
                slider.value = _currentValue;
                text.text = $"{_currentValue} / {GameConfig.DailyRewardInterval}";

                _isFull = _currentValue >= GameConfig.DailyRewardInterval;
                additiveCanvasGroup.alpha = _isFull ? 1 : 0;
                additiveCanvasGroup.interactable = _isFull;
                button.interactable = _isFull;
                _animator.SetBool(IsFull, _isFull);
            }
        }

        private IEnumerator CoUpdateSlider(long endValue)
        {
            var distance = endValue - slider.value;
            while (distance > GameConfig.DailyRewardInterval * 0.01f)
            {
                distance -= distance * Time.deltaTime * 2f;
                _currentValue = (long) (endValue - distance);
                slider.value = endValue - distance;
                text.text = $"{_currentValue} / {GameConfig.DailyRewardInterval}";
                yield return null;
            }

            UpdateSlider(false);
        }

        public void GetReward()
        {
            Game.Game.instance.ActionManager.DailyReward().Subscribe(_ =>
            {
                _updateEnable = true;
                Notification.Push(Nekoyume.Model.Mail.MailType.System,
                    LocalizationManager.Localize("UI_RECEIVED_DAILY_REWARD"));
            });
            Notification.Push(Nekoyume.Model.Mail.MailType.System,
                LocalizationManager.Localize("UI_RECEIVING_DAILY_REWARD"));
            additiveCanvasGroup.alpha = 0;
            additiveCanvasGroup.interactable = false;
            button.interactable = false;
            _isFull = false;
            _animator.SetBool(IsFull, _isFull);
            _animator.StopPlayback();
            _animator.SetTrigger(Reward);
            VFXController.instance.Create<ItemMoveVFX>(boxImageTransform.position);

            if (!(actionPoint is null))
            {
                ItemMoveAnimation.Show(actionPoint.Image.sprite,
                    boxImageTransform.position,
                    actionPoint.Image.transform.position,
                    true,
                    1f,
                    0.8f);
            }

            SetBlockIndex(0L, true);
            _updateEnable = false;
        }

        public void ShowTooltip()
        {
            Widget.Find<VanilaTooltip>().Show("UI_PROSPERITY_DEGREE", "UI_PROSPERITY_DEGREE_DESCRIPTION",
                tooltipArea.position);
        }

        public void HideTooltip()
        {
            Widget.Find<VanilaTooltip>().Close();
        }
    }
}
