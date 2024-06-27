using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nekoyume.Game.Battle;
using Nekoyume.Action;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module.Common;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class ActionPoint : AlphaAnimateModule
    {
        [Serializable]
        private struct DailyBonus
        {
            public GameObject container;
            public SliderAnimator sliderAnimator;
            public TextMeshProUGUI blockText;
        }

        [SerializeField]
        private SliderAnimator sliderAnimator = null;

        [SerializeField]
        private TextMeshProUGUI text = null;

        [SerializeField]
        private Image image = null;

        [SerializeField]
        private DailyBonus dailyBonus;

        [SerializeField]
        private bool syncWithAvatarState = true;

        [SerializeField]
        private GameObject loading;

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private Button button;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private long _currentActionPoint;
        private long _currentBlockIndex;
        private long _rewardReceivedBlockIndex;

        private static readonly int IsFull = Animator.StringToHash("IsFull");
        private static readonly int GetReward = Animator.StringToHash("GetReward");

        public bool IsRemained => _currentActionPoint > 0;

        public Image IconImage => image;

        public bool NowCharging => loading.activeSelf;

        #region Mono

        private void Awake()
        {
            sliderAnimator.OnSliderChange
                .Subscribe(_ => OnSliderChange())
                .AddTo(gameObject);
            dailyBonus.sliderAnimator.OnSliderChange
                .Subscribe(_ => OnDailyBonusSliderChange())
                .AddTo(gameObject);

            sliderAnimator.SetValue(0f, false);
            sliderAnimator.SetMaxValue(DailyReward.ActionPointMax);
            dailyBonus.sliderAnimator.SetValue(0f, false);
            dailyBonus.sliderAnimator.SetMaxValue(DailyReward.DailyRewardInterval);

            GameConfigStateSubject.ActionPointState
                .ObserveAdd()
                .ObserveOnMainThread()
                .Where(x => x.Key == States.Instance.CurrentAvatarState.address)
                .Subscribe(x => Charger(true))
                .AddTo(gameObject);

            GameConfigStateSubject.ActionPointState
                .ObserveRemove()
                .ObserveOnMainThread()
                .Where(x => x.Key == States.Instance.CurrentAvatarState.address)
                .Subscribe(x => Charger(false)).AddTo(gameObject);

            button.onClick.AddListener(ShowMaterialNavigationPopup);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            sliderAnimator.SetMaxValue(DailyReward.ActionPointMax);
            dailyBonus.sliderAnimator.SetMaxValue(DailyReward.DailyRewardInterval);

            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState is not null)
            {
                SetActionPoint(ReactiveAvatarState.ActionPoint, false);
                SetBlockIndex(Game.Game.instance.Agent.BlockIndex, false);
                SetRewardReceivedBlockIndex(ReactiveAvatarState.DailyRewardReceivedIndex, false);
            }

            ReactiveAvatarState.ObservableActionPoint
                .Subscribe(x => SetActionPoint(x, true))
                .AddTo(_disposables);
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(x => SetBlockIndex(x, true))
                .AddTo(_disposables);
            ReactiveAvatarState.ObservableDailyRewardReceivedIndex
                .Subscribe(x => SetRewardReceivedBlockIndex(x, true))
                .AddTo(_disposables);

            OnSliderChange();
            OnDailyBonusSliderChange();

            if (States.Instance.CurrentAvatarState is null)
            {
                Charger(false);
            }
            else
            {
                var address = States.Instance.CurrentAvatarState.address;
                Charger(
                    GameConfigStateSubject.ActionPointState.TryGetValue(address, out var value) &&
                    value);
            }
        }

        protected override void OnDisable()
        {
            sliderAnimator.Stop();
            dailyBonus.sliderAnimator.Stop();
            _disposables.DisposeAllAndClear();
            base.OnDisable();
        }

        #endregion

        private void SetActionPoint(long actionPoint, bool useAnimation)
        {
            if (_currentActionPoint == actionPoint)
            {
                return;
            }

            _currentActionPoint = actionPoint;
            sliderAnimator.SetValue(_currentActionPoint, useAnimation);
        }

        private void SetBlockIndex(long blockIndex, bool useAnimation)
        {
            if (_currentBlockIndex == blockIndex)
            {
                return;
            }

            _currentBlockIndex = blockIndex;
            UpdateDailyBonusSlider(useAnimation);
        }

        private void SetRewardReceivedBlockIndex(long rewardReceivedBlockIndex, bool useAnimation)
        {
            if (_rewardReceivedBlockIndex == rewardReceivedBlockIndex)
            {
                return;
            }

            _rewardReceivedBlockIndex = rewardReceivedBlockIndex;
            UpdateDailyBonusSlider(useAnimation);
        }

        private void UpdateDailyBonusSlider(bool useAnimation)
        {
            var endValue = Math.Max(0, _currentBlockIndex - _rewardReceivedBlockIndex);
            var value = Math.Min(DailyReward.DailyRewardInterval, endValue);
            var remainBlock = DailyReward.DailyRewardInterval - value;

            dailyBonus.sliderAnimator.SetValue(value, useAnimation);
            var timeSpanString =
                remainBlock > 0 ? $"({remainBlock.BlockRangeToTimeSpanString()})" : string.Empty;
            dailyBonus.blockText.text =  $"{remainBlock:#,0}{timeSpanString}";
        }

        private void OnSliderChange()
        {
            var current = ((int)sliderAnimator.Value).ToString("N0", CultureInfo.CurrentCulture);
            var max = ((int)sliderAnimator.MaxValue).ToString("N0", CultureInfo.CurrentCulture);
            text.text = $"{current}/{max}";
        }

        private void OnDailyBonusSliderChange()
        {
            animator.SetBool(IsFull, dailyBonus.sliderAnimator.IsFull);
        }

        public void SetEventTriggerEnabled(bool value)
        {
            button.interactable = value;
        }

        private void Charger(bool isCharging)
        {
            loading.SetActive(isCharging);
            text.enabled = !isCharging;
        }

        // Call at Event Trigger Component
        public void ShowMaterialNavigationPopup()
        {
            const int requiredStage = Game.LiveAsset.GameConfig.RequiredStage.ChargeAP;
            if (!States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(requiredStage) &&
                IsRemained)
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_REQUIRE_CLEAR_STAGE", requiredStage),
                    NotificationCell.NotificationType.UnlockCondition);
                return;
            }

            var popup = Widget.Find<MaterialNavigationPopup>();

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var apStoneCount = Game.Game.instance.States.CurrentAvatarState.inventory.Items
                .Where(x =>
                    x.item.ItemSubType == ItemSubType.ApStone &&
                    !x.Locked &&
                    !(x.item is ITradableItem tradableItem &&
                      tradableItem.RequiredBlockIndex > blockIndex))
                .Sum(item => item.count);

            var itemCountText = $"{sliderAnimator.Value}/{sliderAnimator.MaxValue}";
            var blockRange = (long)dailyBonus.sliderAnimator.Value;
            var maxBlockRange = (long)dailyBonus.sliderAnimator.MaxValue;
            var isInteractable = IsInteractableMaterial(); // 이 경우 버튼 자체를 비활성화

            popup.ShowAP(
                itemCountText,
                apStoneCount,
                blockRange,
                maxBlockRange,
                isInteractable,
                ChargeAP,
                GetDailyReward,
                true);
        }

        public static void ChargeAP()
        {
            Game.Game.instance.ActionManager.ChargeActionPoint().Subscribe();
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

            animator.SetTrigger(GetReward);
        }

        public static bool IsInteractableMaterial()
        {
            if (Widget.Find<HeaderMenuStatic>().ActionPoint.NowCharging) // is charging?
            {
                return false;
            }

            // if full?
            if (ReactiveAvatarState.ActionPoint == DailyReward.ActionPointMax)
            {
                return false;
            }

            return !BattleRenderer.Instance.IsOnBattle;
        }

        public static void ShowRefillConfirmPopup(System.Action confirmCallback)
        {
            var confirm = Widget.Find<IconAndButtonSystem>();
            confirm.ShowWithTwoButton("UI_CONFIRM", "UI_AP_REFILL_CONFIRM_CONTENT",
                "UI_OK", "UI_CANCEL",
                true, IconAndButtonSystem.SystemType.Information);
            confirm.ConfirmCallback = confirmCallback;
            confirm.CancelCallback = () => confirm.Close();
        }
    }
}
