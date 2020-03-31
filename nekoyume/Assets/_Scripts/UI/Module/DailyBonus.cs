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
        private Animator animator = null;

        [SerializeField, CanBeNull]
        private ActionPoint actionPoint = null;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private long _currentBlockIndex;
        private long _rewardReceivedBlockIndex;
        private bool _isFull;

        // NOTE: CoGetDailyRewardAnimation() 연출 로직이 모두 흐르지 않았을 경우를 대비해서 연출 단계를 저장하는 필드.
        private int _coGetDailyRewardAnimationStep = 0;

        private static readonly int IsFull = Animator.StringToHash("IsFull");
        private static readonly int GetReward = Animator.StringToHash("GetReward");

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

            OnSliderChange();

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
        }

        protected override void OnDisable()
        {
            if (_coGetDailyRewardAnimationStep > 0)
            {
                var avatarAddress = States.Instance.CurrentAvatarState.address;
                switch (_coGetDailyRewardAnimationStep)
                {
                    case 1:

                        LocalStateModifier.ModifyAvatarDailyRewardReceivedIndex(avatarAddress, true);
                        break;
                    case 2:
                        LocalStateModifier.ModifyAvatarActionPoint(avatarAddress, GameConfig.ActionPointMax);
                        break;
                }

                _coGetDailyRewardAnimationStep = 0;
            }

            sliderAnimator.Stop();
            _disposables.DisposeAllAndClear();
            base.OnDisable();
        }

        #endregion

        private void SetBlockIndex(long blockIndex, bool useAnimation)
        {
            if (_currentBlockIndex == blockIndex)
                return;

            _currentBlockIndex = blockIndex;
            UpdateSlider(useAnimation);
        }

        private void SetRewardReceivedBlockIndex(long rewardReceivedBlockIndex, bool useAnimation)
        {
            if (_rewardReceivedBlockIndex == rewardReceivedBlockIndex)
                return;

            _rewardReceivedBlockIndex = rewardReceivedBlockIndex;
            UpdateSlider(useAnimation);
        }

        private void UpdateSlider(bool useAnimation)
        {
            var endValue = Math.Min(
                Math.Max(0, _currentBlockIndex - _rewardReceivedBlockIndex),
                GameConfig.DailyRewardInterval);

            sliderAnimator.SetValue(endValue, useAnimation);
        }

        private void OnSliderChange()
        {
            text.text = $"{(int) sliderAnimator.Value} / {sliderAnimator.MaxValue}";

            _isFull = sliderAnimator.IsFull;
            foreach (var additiveImage in additiveImages)
            {
                additiveImage.enabled = _isFull;
            }

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

        public void GetDailyReward()
        {
            if (!_isFull)
            {
                return;
            }

            Notification.Push(Nekoyume.Model.Mail.MailType.System,
                LocalizationManager.Localize("UI_RECEIVING_DAILY_REWARD"));

            Game.Game.instance.ActionManager.DailyReward().Subscribe(_ =>
            {
                Notification.Push(Nekoyume.Model.Mail.MailType.System,
                    LocalizationManager.Localize("UI_RECEIVED_DAILY_REWARD"));
            });

            StartCoroutine(CoGetDailyRewardAnimation());
        }

        private IEnumerator CoGetDailyRewardAnimation()
        {
            _coGetDailyRewardAnimationStep = 1;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            LocalStateModifier.ModifyAvatarDailyRewardReceivedIndex(avatarAddress, true);
            _coGetDailyRewardAnimationStep = 2;
            animator.SetTrigger(GetReward);
            VFXController.instance.Create<ItemMoveVFX>(boxImageTransform.position);

            if (actionPoint is null)
                yield break;

            ItemMoveAnimation.Show(actionPoint.Image.sprite,
                boxImageTransform.position,
                actionPoint.Image.transform.position,
                true,
                1f,
                0.8f);

            yield return new WaitForSeconds(1.5f);
            LocalStateModifier.ModifyAvatarActionPoint(avatarAddress, GameConfig.ActionPointMax);
            _coGetDailyRewardAnimationStep = 0;
        }
    }
}
