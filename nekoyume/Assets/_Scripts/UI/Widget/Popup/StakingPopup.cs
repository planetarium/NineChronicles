using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class StakingPopup : PopupWidget
    {
        [Header("Top")]
        [SerializeField] private Image levelIconImage;
        [SerializeField] private Image[] levelImages;
        [SerializeField] private TextMeshProUGUI depositText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button ncgEditButton;
        [SerializeField] private TextMeshProUGUI editButtonText;

        [Header("Center")]
        [SerializeField] private StakingBuffBenefitsView[] buffBenefitsViews;
        [SerializeField] private StakingInterestBenefitsView[] interestBenefitsViews;
        [SerializeField] private TextMeshProUGUI remainingBlockText;
        [SerializeField] private ConditionalButton archiveButton;

        [Header("Bottom")]
        [SerializeField] private CategoryTabButton currentBenefitsTabButton;
        [SerializeField] private CategoryTabButton levelBenefitsTabButton;
        [SerializeField] private GameObject currentBenefitsTab;
        [SerializeField] private GameObject levelBenefitsTab;

        [SerializeField] private StakeIconDataScriptableObject stakeIconData;

        [SerializeField]
        private TMP_InputField stakingNcgInputField;

        private readonly ReactiveProperty<BigInteger> _deposit = new();
        private readonly Module.ToggleGroup _toggleGroup = new();

        private const string ActionPointBuffFormat = "{0} <color=#1FFF00>{1}% DC</color>";
        private const string BuffBenefitRateFormat = "{0} <color=#1FFF00>+{1}%</color>";
        private const string RemainingBlockFormat = "<Style=G5>{0}({1})";

        private bool HasStakeState => _deposit.Value > 0;

        private bool _benefitListViewsInitialized;

        protected override void Awake()
        {
            base.Awake();

            _toggleGroup.RegisterToggleable(currentBenefitsTabButton);
            _toggleGroup.RegisterToggleable(levelBenefitsTabButton);
            currentBenefitsTabButton.OnClick.Subscribe(_ =>
            {
                if (HasStakeState)
                {
                    currentBenefitsTab.SetActive(true);
                    levelBenefitsTab.SetActive(false);
                }
                else
                {
                    // TODO: change "asdgf" to localized system message.
                    OneLineSystem.Push(MailType.System, "asdgf", NotificationCell.NotificationType.UnlockCondition);
                }
            }).AddTo(gameObject);
            levelBenefitsTabButton.OnClick.Subscribe(_ =>
            {
                currentBenefitsTab.SetActive(false);
                levelBenefitsTab.SetActive(true);
            }).AddTo(gameObject);
            closeButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Close();
            });
            ncgEditButton.onClick.AddListener(OnClickEditButton);
            // TODO: 여기를 ClaimStakeReward action 보내는 버튼으로 바꿔야함
            archiveButton.OnSubmitSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                ActionManager.Instance
                    .ClaimStakeReward(States.Instance.CurrentAvatarState.address)
                    .Subscribe();
                archiveButton.Interactable = false;
                LoadingHelper.ClaimStakeReward.Value = true;
            }).AddTo(gameObject);

            stakingNcgInputField.onEndEdit.AddListener(value =>
            {
                ActionManager.Instance.Stake(BigInteger.Parse(value)).Subscribe();
            });
        }

        public override void Initialize()
        {
            SetBenefitsListViews();
            _deposit.Subscribe(OnDepositEdited).AddTo(gameObject);
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Where(_ => gameObject.activeSelf).Subscribe(OnBlockUpdated)
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            SetView();
            base.Show(ignoreStartAnimation);
        }

        public void SetView()
        {
            SetBenefitsListViews();
            var deposit = States.Instance.StakedBalanceState?.Gold.MajorUnit ?? 0;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;

            _deposit.Value = deposit;
            OnBlockUpdated(blockIndex);
            foreach (var toggleable in _toggleGroup.Toggleables)
            {
                toggleable.Toggleable = true;
            }

            _toggleGroup.SetToggledOffAll();
            var selectedTabButton = deposit > 0 ? currentBenefitsTabButton : levelBenefitsTabButton;
            selectedTabButton.OnClick.OnNext(selectedTabButton);
            selectedTabButton.SetToggledOn();
            stakingNcgInputField.interactable = false;
        }

        private void OnClickEditButton()
        {
            // it means States.Instance.StakeStateV2.HasValue is true.
            if (HasStakeState)
            {
                // ReSharper disable once PossibleInvalidOperationException
                var stakeState = States.Instance.StakeStateV2.Value;
                var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
                var cancellableBlockIndex =
                    stakeState.CancellableBlockIndex;
                if (cancellableBlockIndex > currentBlockIndex)
                {
                    // TODO: adjust l10n
                    OneLineSystem.Push(MailType.System,
                        $"can not modify until tip {cancellableBlockIndex}",
                        NotificationCell.NotificationType.UnlockCondition);
                    return;
                }

                if (stakeState.ClaimableBlockIndex <= currentBlockIndex)
                {
                    // TODO: adjust l10n
                    OneLineSystem.Push(MailType.System,
                        "claim reward first.",
                        NotificationCell.NotificationType.UnlockCondition);
                    return;
                }
            }
            else
            {
                if (States.Instance.GoldBalanceState.Gold.RawValue < 50)
                {
                    // TODO: adjust l10n
                    OneLineSystem.Push(MailType.System,
                        "You can only do Stake if you have more than 50 NCG.",
                        NotificationCell.NotificationType.UnlockCondition);
                    return;
                }
            }

            stakingNcgInputField.interactable = true;
        }

        private void OnDepositEdited(BigInteger deposit)
        {
            var states = States.Instance;
            var level = states.StakingLevel;

            levelIconImage.sprite = stakeIconData.GetIcon(level, IconType.Small);
            for (var i = 0; i < levelImages.Length; i++)
            {
                levelImages[i].enabled = i < level;
            }

            depositText.text = $"/ Hold NCG <Style=G0>{deposit.ToString("N0")}";
            // TODO: get data from external source or table sheet
            buffBenefitsViews[0].Set(
                string.Format(BuffBenefitRateFormat, L10nManager.Localize("ARENA_REWARD_BONUS"),
                    0));
            buffBenefitsViews[1].Set(
                string.Format(BuffBenefitRateFormat, L10nManager.Localize("GRINDING_CRYSTAL_BONUS"),
                    0));
            buffBenefitsViews[2].Set(
                string.Format(ActionPointBuffFormat, L10nManager.Localize("STAGE_AP_BONUS"),
                    0));
        }

        private void OnBlockUpdated(long blockIndex)
        {
            var states = States.Instance;
            var level = states.StakingLevel;
            var deposit = states.StakedBalanceState?.Gold.MajorUnit ?? 0;
            var regularSheet = states.StakeRegularRewardSheet;
            var regularFixedSheet = states.StakeRegularFixedRewardSheet;
            var stakeStateV2 = states.StakeStateV2;
            var rewardBlockInterval = stakeStateV2.HasValue
                ? (int) stakeStateV2.Value.Contract.RewardInterval
                : (int) StakeState.RewardInterval;

            TryGetWaitedBlockIndex(blockIndex, rewardBlockInterval, out var waitedBlockRange);
            var rewardCount = (int) waitedBlockRange / rewardBlockInterval;
            regularSheet.TryGetValue(level, out var regular);
            regularFixedSheet.TryGetValue(level, out var regularFixed);

            var materialSheet = TableSheets.Instance.MaterialItemSheet;
            for (var i = 0; i < interestBenefitsViews.Length; i++)
            {
                var result = GetReward(regular, regularFixed, (long) deposit, i);
                result *= Mathf.Max(rewardCount, 1);
                if (result <= 0)
                {
                    interestBenefitsViews[i].gameObject.SetActive(false);
                    continue;
                }

                interestBenefitsViews[i].gameObject.SetActive(true);
                if (regular != null)
                {
                    switch (regular.Rewards[i].Type)
                    {
                        case StakeRegularRewardSheet.StakeRewardType.Item:
                            interestBenefitsViews[i].Set(
                                ItemFactory.CreateMaterial(materialSheet,
                                    regular.Rewards[i].ItemId),
                                (int) result);
                            break;
                        case StakeRegularRewardSheet.StakeRewardType.Rune:
                            interestBenefitsViews[i].Set(
                                regular.Rewards[i].ItemId,
                                (int) result);
                            break;
                        case StakeRegularRewardSheet.StakeRewardType.Currency:
                            interestBenefitsViews[i].Set(
                                regular.Rewards[i].CurrencyTicker,
                                (int) result);
                            break;
                    }
                }
            }

            var remainingBlock = Math.Max(rewardBlockInterval - waitedBlockRange, 0);
            remainingBlockText.text = string.Format(
                RemainingBlockFormat,
                remainingBlock.ToString("N0"),
                remainingBlock.BlockRangeToTimeSpanString());
            archiveButton.Interactable = remainingBlock == 0 && !LoadingHelper.ClaimStakeReward.Value;

            // can not category UI toggle when user has not StakeState.
            currentBenefitsTabButton.Toggleable = HasStakeState;
            levelBenefitsTabButton.Toggleable = HasStakeState;
            // TODO: adjust l10n
            editButtonText.text = HasStakeState ? "Edit" : "Start";
        }

        private static bool TryGetWaitedBlockIndex(
            long blockIndex,
            int rewardBlockInterval,
            out long waitedBlockRange)
        {
            var stakeState = States.Instance.StakeStateV2;
            if (stakeState is null)
            {
                waitedBlockRange = 0;
                return false;
            }

            var started = stakeState.Value.StartedBlockIndex;
            var received = stakeState.Value.ReceivedBlockIndex;
            if (received > 0)
            {
                waitedBlockRange = blockIndex - received;
                waitedBlockRange += (received - started) % rewardBlockInterval;
            }
            else
            {
                waitedBlockRange = blockIndex - started;
            }

            return true;
        }

        private void SetBenefitsListViews()
        {
            if (_benefitListViewsInitialized)
            {
                return;
            }

            var regularSheet = States.Instance.StakeRegularRewardSheet;
            var regularFixedSheet = States.Instance.StakeRegularFixedRewardSheet;
            if (regularSheet is null || regularFixedSheet is null)
            {
                return;
            }

            _benefitListViewsInitialized = true;
        }

        private static long GetReward(
            StakeRegularRewardSheet.Row regular,
            StakeRegularFixedRewardSheet.Row regularFixed,
            long deposit, int index)
        {
            if (regular is null || regularFixed is null)
            {
                return 0;
            }

            if (regular.Rewards.Count <= index || index < 0)
            {
                return 0;
            }

            var result = (long)Math.Truncate(deposit / regular.Rewards[index].DecimalRate);
            var levelBonus = regularFixed.Rewards.FirstOrDefault(
                reward => reward.ItemId == regular.Rewards[index].ItemId)?.Count ?? 0;

            return result + levelBonus;
        }
    }
}
