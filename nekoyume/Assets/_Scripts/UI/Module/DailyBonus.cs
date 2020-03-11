using System;
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
        private SliderAnimator sliderAnimator = null;

        [SerializeField]
        private TextMeshProUGUI text = null;

        [SerializeField]
        private Button button = null;

        [SerializeField]
        private CanvasGroup additiveCanvasGroup = null;

        [SerializeField]
        private RectTransform tooltipArea = null;

        [SerializeField]
        private Transform boxImageTransform = null;

        [SerializeField]
        private Animator animator;

        [SerializeField, CanBeNull]
        private ActionPoint actionPoint = null;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private bool _isFull;

        private long _currentBlockIndex;
        private long _rewardReceivedBlockIndex;

        private static readonly int IsFull = Animator.StringToHash("IsFull");
        private static readonly int Reward = Animator.StringToHash("GetReward");

        #region Mono

        private void Awake()
        {
            sliderAnimator.OnSliderChange.Subscribe(_ => OnSliderChange()).AddTo(gameObject);
            sliderAnimator.SetMaxValue(GameConfig.DailyRewardInterval);
            sliderAnimator.SetValue(0f, false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            additiveCanvasGroup.alpha = 0f;

            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SetBlockIndex).AddTo(_disposables);
            ReactiveAvatarState.DailyRewardReceivedIndex
                .Subscribe(SetRewardReceivedBlockIndex).AddTo(_disposables);
        }

        protected override void OnDisable()
        {
            sliderAnimator.Stop();
            _disposables.DisposeAllAndClear();
            base.OnDisable();
        }

        #endregion

        private void SetBlockIndex(long blockIndex)
        {
            if (_currentBlockIndex == blockIndex)
                return;

            _currentBlockIndex = blockIndex;
            UpdateSlider();
        }

        private void SetRewardReceivedBlockIndex(long rewardReceivedBlockIndex)
        {
            if (_rewardReceivedBlockIndex == rewardReceivedBlockIndex)
                return;

            _rewardReceivedBlockIndex = rewardReceivedBlockIndex;
            UpdateSlider();
        }

        private void UpdateSlider()
        {
            var endValue = Math.Min(
                Math.Max(0, _currentBlockIndex - _rewardReceivedBlockIndex),
                GameConfig.DailyRewardInterval);

            sliderAnimator.SetValue(endValue);
        }

        private void OnSliderChange()
        {
            text.text = $"{(int) sliderAnimator.Value} / {sliderAnimator.MaxValue}";

            if (_isFull == sliderAnimator.IsFull)
                return;

            _isFull = sliderAnimator.IsFull;
            additiveCanvasGroup.alpha = _isFull ? 1f : 0f;
            additiveCanvasGroup.interactable = _isFull;
            button.interactable = _isFull;
            animator.SetBool(IsFull, _isFull);
        }

        public void GetReward()
        {
            Notification.Push(Nekoyume.Model.Mail.MailType.System,
                LocalizationManager.Localize("UI_RECEIVING_DAILY_REWARD"));

            Game.Game.instance.ActionManager.DailyReward().Subscribe(_ =>
            {
                Notification.Push(Nekoyume.Model.Mail.MailType.System,
                    LocalizationManager.Localize("UI_RECEIVED_DAILY_REWARD"));
            });

            _isFull = false;
            additiveCanvasGroup.alpha = 0;
            additiveCanvasGroup.interactable = _isFull;
            button.interactable = _isFull;
            animator.SetBool(IsFull, _isFull);
            animator.StopPlayback();
            animator.SetTrigger(Reward);
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
