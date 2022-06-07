using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using JetBrains.Annotations;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using Nekoyume.UI.Scroller;
    using UniRx;

    public class DailyBonus : AlphaAnimateModule
    {
        [SerializeField]
        private SliderAnimator sliderAnimator = null;

        [SerializeField]
        private TextMeshProUGUI text = null;

        [SerializeField]
        private RectTransform tooltipArea = null;

        [SerializeField]
        private Transform boxImageTransform = null;

        [SerializeField]
        private Image[] additiveImages = null;

        [SerializeField]
        private Image hasNotificationImage = null;

        [SerializeField]
        private Animator animator = null;

        [SerializeField, CanBeNull]
        private ActionPoint actionPoint = null;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private long _currentBlockIndex;
        private long _rewardReceivedBlockIndex;
        private bool _isFull;

        private static readonly int IsFull = Animator.StringToHash("IsFull");
        private static readonly int GetReward = Animator.StringToHash("GetReward");

        #region Mono

        private void Awake()
        {
            sliderAnimator.OnSliderChange
                .Subscribe(_ => OnSliderChange())
                .AddTo(gameObject);
            sliderAnimator.SetMaxValue(States.Instance.GameConfigState.DailyRewardInterval);
            sliderAnimator.SetValue(0f, false);

            GameConfigStateSubject.GameConfigState
                .Subscribe(state => sliderAnimator.SetMaxValue(state.DailyRewardInterval))
                .AddTo(gameObject);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (!(States.Instance.CurrentAvatarState is null))
            {
                SetBlockIndex(Game.Game.instance.Agent.BlockIndex, false);
                SetRewardReceivedBlockIndex(States.Instance.CurrentAvatarState.dailyRewardReceivedIndex, false);
            }

            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(x => SetBlockIndex(x, true))
                .AddTo(_disposables);
            ReactiveAvatarState.DailyRewardReceivedIndex
                .Subscribe(x => SetRewardReceivedBlockIndex(x, true))
                .AddTo(_disposables);

            OnSliderChange();
        }

        protected override void OnDisable()
        {
            sliderAnimator.Stop();
            _disposables.DisposeAllAndClear();
            base.OnDisable();
        }

        #endregion

        private void SetBlockIndex(long blockIndex, bool useAnimation)
        {
            if (_currentBlockIndex == blockIndex)
            {
                return;
            }

            _currentBlockIndex = blockIndex;
            UpdateSlider(useAnimation);
        }

        private void SetRewardReceivedBlockIndex(long rewardReceivedBlockIndex, bool useAnimation)
        {
            if (_rewardReceivedBlockIndex == rewardReceivedBlockIndex)
            {
                return;
            }

            _rewardReceivedBlockIndex = rewardReceivedBlockIndex;
            UpdateSlider(useAnimation);
        }

        private void UpdateSlider(bool useAnimation)
        {
            var gameConfigState = States.Instance.GameConfigState;
            var endValue = Math.Min(
                gameConfigState.DailyRewardInterval,
                Math.Max(0, _currentBlockIndex - _rewardReceivedBlockIndex));

            sliderAnimator.SetValue(endValue, useAnimation);
        }

        private void OnSliderChange()
        {
            var current = ((int)sliderAnimator.Value).ToString("N0", CultureInfo.CurrentCulture);
            var max = ((int)sliderAnimator.MaxValue).ToString("N0", CultureInfo.CurrentCulture);
            text.text = $"{current}/{max}";

            _isFull = sliderAnimator.IsFull;
            foreach (var additiveImage in additiveImages)
            {
                additiveImage.enabled = _isFull;
            }

            hasNotificationImage.enabled = _isFull && States.Instance.CurrentAvatarState?.actionPoint == 0;

            animator.SetBool(IsFull, _isFull);
        }

        public void ShowTooltip()
        {
            Widget.Find<VanilaTooltip>()
                .Show("UI_PROSPERITY_DEGREE", "UI_PROSPERITY_DEGREE_DESCRIPTION", tooltipArea.position);
        }

        public void HideTooltip()
        {
            Widget.Find<VanilaTooltip>().Close();
        }

        public void RequestDailyReward()
        {
            if (!_isFull)
            {
                return;
            }

            if (actionPoint != null && actionPoint.NowCharging)
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_CHARGING_AP"),
                    NotificationCell.NotificationType.Information);
            }
            else if (States.Instance.CurrentAvatarState.actionPoint > 0)
            {
                var confirm = Widget.Find<ConfirmPopup>();
                confirm.Show("UI_CONFIRM", "UI_AP_REFILL_CONFIRM_CONTENT");
                confirm.CloseCallback = result =>
                {
                    if (result == ConfirmResult.No)
                    {
                        return;
                    }

                    GetDailyReward();
                };
            }
            else
            {
                GetDailyReward();
            }
        }

        private void GetDailyReward()
        {
            NotificationSystem.Push(
                MailType.System,
                L10nManager.Localize("UI_RECEIVING_DAILY_REWARD"),
                NotificationCell.NotificationType.Information);
            
            Game.Game.instance.ActionManager.DailyReward().Subscribe();

            var address = States.Instance.CurrentAvatarState.address;
            if (GameConfigStateSubject.ActionPointState.ContainsKey(address))
            {
                GameConfigStateSubject.ActionPointState.Remove(address);
            }
            GameConfigStateSubject.ActionPointState.Add(address, true);

            StartCoroutine(CoGetDailyRewardAnimation());
        }

        private IEnumerator CoGetDailyRewardAnimation()
        {
            animator.SetTrigger(GetReward);
            VFXController.instance.Create<ItemMoveVFX>(boxImageTransform.position);

            if (actionPoint != null)
            {
                ItemMoveAnimation.Show(actionPoint.Image.sprite,
                    boxImageTransform.position,
                    actionPoint.Image.transform.position,
                    Vector2.one,
                    true,
                    true,
                    1f,
                    0.8f);

                yield return new WaitForSeconds(1.5f);
            }
        }
    }
}
