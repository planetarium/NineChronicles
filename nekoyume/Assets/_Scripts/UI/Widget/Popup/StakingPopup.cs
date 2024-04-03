using System;
using System.Linq;
using System.Numerics;
using Lib9c;
using Nekoyume.Blockchain;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.LiveAsset;
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
        [SerializeField] private TextMeshProUGUI depositText; // it shows having NCG, not staked.
        [SerializeField] private TextMeshProUGUI stakedNcgValueText; // it shows staked NCG, not having.
        [SerializeField] private Button closeButton;
        [SerializeField] private Button ncgEditButton;
        [SerializeField] private Button migrateButton;
        [SerializeField] private Button stakingStartButton;
        [SerializeField] private Button editCancelButton;
        [SerializeField] private Button editSaveButton;
        [SerializeField] private GameObject editingUIParent;
        [SerializeField] private GameObject defaultUIParent;
        [SerializeField] private TMP_InputField stakingNcgInputField;

        [Header("Center")]
        [SerializeField] private StakingBuffBenefitsView[] buffBenefitsViews;
        [SerializeField] private StakingInterestBenefitsView[] interestBenefitsViews;
        [SerializeField] private TextMeshProUGUI remainingBlockText;
        [SerializeField] private ConditionalButton archiveButton;
        [SerializeField] private RectTransform scrollableRewardsRectTransform;
        [SerializeField] private Image stakingLevelImage;
        [SerializeField] private Image stakingRewardImage;
        [SerializeField] private GameObject[] myLevelHighlightObjects;

        [Header("Bottom")]
        [SerializeField] private CategoryTabButton currentBenefitsTabButton;
        [SerializeField] private CategoryTabButton levelBenefitsTabButton;
        [SerializeField] private GameObject currentBenefitsTab;
        [SerializeField] private GameObject levelBenefitsTab;

        [SerializeField] private StakeIconDataScriptableObject stakeIconData;
        [SerializeField] private GameObject stakingInformationObject;
        [SerializeField] private UIBackground informationBg;

        private readonly Module.ToggleGroup _toggleGroup = new();

        private readonly Color _normalInputFieldColor = new(0xEB, 0xCE, 0xB1, 0xFF);

        private const string ActionPointBuffFormat = "{0} <color=#1FFF00>{1}% DC</color>";
        private const string BuffBenefitRateFormat = "{0} <color=#1FFF00>+{1}%</color>";
        private const string RemainingBlockFormat = "<Style=G5>{0}({1})";
        private const string TableSheetViewUrlPrefix = "https://9c-board.netlify.app/odin/tablesheet/";

        private bool _benefitListViewsInitialized;

        private int[] _tempArenaBonusValues = { 0, 0, 100, 200, 200, 200, 200, 200, 200 };

        protected override void Awake()
        {
            base.Awake();

            _toggleGroup.RegisterToggleable(currentBenefitsTabButton);
            _toggleGroup.RegisterToggleable(levelBenefitsTabButton);
            currentBenefitsTabButton.OnClick.Subscribe(_ =>
            {
                if (States.Instance.StakeStateV2.HasValue)
                {
                    currentBenefitsTab.SetActive(true);
                    levelBenefitsTab.SetActive(false);
                }
                else
                {
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_REQUIRE_STAKE"),
                        NotificationCell.NotificationType.UnlockCondition);
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
            migrateButton.onClick.AddListener(OnClickMigrateButton);
            stakingStartButton.onClick.AddListener(OnClickEditButton);
            archiveButton.SetCondition(() => !LoadingHelper.ClaimStakeReward.Value);
            archiveButton.OnSubmitSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                ActionManager.Instance
                    .ClaimStakeReward(States.Instance.CurrentAvatarState.address)
                    .Subscribe();
                LoadingHelper.ClaimStakeReward.Value = true;
                archiveButton.UpdateObjects();
            }).AddTo(gameObject);
            editSaveButton.onClick.AddListener(OnClickSaveButton);
            editCancelButton.onClick.AddListener(() =>
            {
                OnChangeEditingState(false);
            });
            informationBg.OnClick = () =>
            {
                stakingInformationObject.SetActive(false);
            };
            stakingNcgInputField.onEndEdit.AddListener(value =>
            {
                var totalDeposit = (States.Instance.GoldBalanceState.Gold +
                                    States.Instance.StakedBalanceState.Gold)
                    .MajorUnit;
                stakingNcgInputField.textComponent.color =
                    BigInteger.TryParse(value, out var inputBigInt) && totalDeposit < inputBigInt
                        ? Palette.GetColor(ColorType.TextDenial)
                        : _normalInputFieldColor;
            });
        }

        public override void Initialize()
        {
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Where(_ => gameObject.activeSelf).Subscribe(OnBlockUpdated)
                .AddTo(gameObject);
            var arenaBonusValues = LiveAssetManager.instance.StakingArenaBonusValues;
            if (arenaBonusValues.Length > 0)
            {
                _tempArenaBonusValues = arenaBonusValues;
            }
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            base.Show(ignoreStartAnimation);
            OnChangeEditingState(false);
            SetView();

            if (!States.Instance.StakeStateV2.HasValue)
            {
                stakingInformationObject.SetActive(true);
            }
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            var anchoredPosition = scrollableRewardsRectTransform.anchoredPosition;
            anchoredPosition.x = 0;
            scrollableRewardsRectTransform.anchoredPosition = anchoredPosition;
        }

        public void SetView()
        {
            var deposit = States.Instance.StakedBalanceState?.Gold.MajorUnit ?? 0;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;

            OnDepositSet(deposit);
            OnBlockUpdated(blockIndex);

            // can not category UI toggle when user has not StakeState.
            var hasStakeState = States.Instance.StakeStateV2.HasValue;

            // if StakePolicySheet has diff with my StakeStateV2.Contract, require migration
            var requiredMigrate = hasStakeState &&
                                  States.Instance.StakeStateV2.Value.Contract
                                      .StakeRegularRewardSheetTableName !=
                                  TableSheets.Instance.StakePolicySheet
                                      .StakeRegularRewardSheetValue;
            stakingStartButton.gameObject.SetActive(!hasStakeState);
            migrateButton.gameObject.SetActive(requiredMigrate);
            ncgEditButton.gameObject.SetActive(hasStakeState && !requiredMigrate);

            foreach (var toggleable in _toggleGroup.Toggleables)
            {
                toggleable.Toggleable = true;
            }

            _toggleGroup.SetToggledOffAll();
            var selectedTabButton = hasStakeState ? currentBenefitsTabButton : levelBenefitsTabButton;
            selectedTabButton.OnClick.OnNext(selectedTabButton);
            selectedTabButton.SetToggledOn();

            if (!hasStakeState)
            {
                foreach (var toggleable in _toggleGroup.Toggleables)
                {
                    toggleable.Toggleable = false;
                }
            }

            var liveAssetManager = LiveAssetManager.instance;
            if (liveAssetManager.StakingLevelSprite != null)
            {
                stakingLevelImage.overrideSprite = liveAssetManager.StakingLevelSprite;
                stakingLevelImage.SetNativeSize();
            }

            if (liveAssetManager.StakingRewardSprite != null)
            {
                stakingRewardImage.overrideSprite = liveAssetManager.StakingRewardSprite;
                stakingLevelImage.SetNativeSize();
            }
        }

        private void OnClickEditButton()
        {
            if (States.Instance.StakeStateV2.HasValue)
            {
                var stakeState = States.Instance.StakeStateV2.Value;
                var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;

                if (stakeState.ClaimableBlockIndex <= currentBlockIndex)
                {
                    OneLineSystem.Push(MailType.System,
                        L10nManager.Localize("UI_REQUIRE_CLAIM_STAKE_REWARD"),
                        NotificationCell.NotificationType.UnlockCondition);
                    return;
                }
            }
            else
            {
                if (States.Instance.GoldBalanceState.Gold.MajorUnit < 50)
                {
                    OneLineSystem.Push(MailType.System,
                        L10nManager.Localize("UI_REQUIRE_STAKE_MINIMUM_NCG_FORMAT", 50),
                        NotificationCell.NotificationType.UnlockCondition);
                    return;
                }
            }

            OnChangeEditingState(true);
            currentBenefitsTabButton.SetToggledOff();
            levelBenefitsTabButton.OnClick.OnNext(levelBenefitsTabButton);
            levelBenefitsTabButton.SetToggledOn();
        }

        private void OnClickMigrateButton()
        {
            if (!States.Instance.StakeStateV2.HasValue)
            {
                NcDebug.LogWarning("[StakingPopup] invoked OnClickMigrateButton(), but not has StakeState.");
                return;
            }

            var stakeState = States.Instance.StakeStateV2.Value;
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            if (stakeState.ClaimableBlockIndex <= currentBlockIndex)
            {
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize("UI_REQUIRE_CLAIM_STAKE_REWARD"),
                    NotificationCell.NotificationType.UnlockCondition);
                return;
            }

            var confirmUI = Find<IconAndButtonSystem>();
            var confirmTitle = L10nManager.Localize("UI_ITEM_INFORMATION");
            var confirmContent = L10nManager.Localize("UI_INTRODUCE_MIGRATION",
                $"{TableSheetViewUrlPrefix}{stakeState.Contract.StakeRegularRewardSheetTableName}",
                $"{TableSheetViewUrlPrefix}{TableSheets.Instance.StakePolicySheet.StakeRegularRewardSheetValue}");

            confirmUI.ShowWithTwoButton(
                confirmTitle,
                confirmContent,
                labelYes: L10nManager.Localize("UI_OK"),
                labelNo: L10nManager.Localize("UI_CANCEL"),
                localize:false,
                type: IconAndButtonSystem.SystemType.Information);
            var disposable = confirmUI.ContentText.SubscribeForClickLink(linkInfo =>
            {
                Application.OpenURL(linkInfo.GetLinkID());
            });
            confirmUI.ConfirmCallback = () =>
            {
                ActionManager.Instance.Stake(States.Instance.StakedBalanceState.Gold.MajorUnit)
                    .Subscribe();
                disposable.Dispose();
                OnChangeEditingState(false);
            };
            confirmUI.CancelCallback = () =>
            {
                disposable.Dispose();
            };
        }

        private void OnClickSaveButton()
        {
            if (!BigInteger.TryParse(stakingNcgInputField.text, out var inputBigInt))
            {
                // TODO: adjust l10n
                OneLineSystem.Push(MailType.System,
                    $"wrong value: {stakingNcgInputField.text}",
                    NotificationCell.NotificationType.UnlockCondition);
                return;
            }

            if (inputBigInt == States.Instance.StakedBalanceState.Gold.MajorUnit)
            {
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize("UI_REQUIRE_DIFF_VALUE_WITH_EXIST"),
                    NotificationCell.NotificationType.UnlockCondition);
                return;
            }

            if (inputBigInt != 0 && inputBigInt < 50)
            {
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize("UI_REQUIRE_STAKE_MINIMUM_NCG_FORMAT", 50),
                    NotificationCell.NotificationType.UnlockCondition);
                return;
            }

            var totalDepositNcg = States.Instance.GoldBalanceState.Gold +
                                  States.Instance.StakedBalanceState.Gold;
            if (inputBigInt > totalDepositNcg.MajorUnit)
            {
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize("UI_STAKING_AMOUNT_LIMIT"),
                    NotificationCell.NotificationType.Alert);
                return;
            }

            var confirmUI = Find<IconAndButtonSystem>();
            var confirmTitle = "UI_ITEM_INFORMATION";
            var confirmContent = "UI_INTRODUCE_STAKING";
            var confirmIcon = IconAndButtonSystem.SystemType.Information;
            var nullableStakeState = States.Instance.StakeStateV2;
            if (nullableStakeState.HasValue)
            {
                var stakeState = nullableStakeState.Value;
                var cancellableBlockIndex =
                    stakeState.CancellableBlockIndex;
                if (inputBigInt < States.Instance.StakedBalanceState.Gold.MajorUnit)
                {
                    if (cancellableBlockIndex > Game.Game.instance.Agent.BlockIndex)
                    {
                        OneLineSystem.Push(MailType.System,
                            L10nManager.Localize("UI_STAKING_LOCK_BLOCK_TIP_FORMAT",
                                cancellableBlockIndex),
                            NotificationCell.NotificationType.UnlockCondition);
                        return;
                    }

                    confirmTitle = "UI_CAUTION";
                    confirmContent = "UI_WARNING_STAKING_REDUCE";
                    confirmIcon = IconAndButtonSystem.SystemType.Error;
                }
            }

            confirmUI.ShowWithTwoButton(confirmTitle, confirmContent, localize:true, type: confirmIcon);
            confirmUI.ConfirmCallback = () =>
            {
                ActionManager.Instance.Stake(BigInteger.Parse(stakingNcgInputField.text))
                    .Subscribe();
                OnChangeEditingState(false);
            };
        }

        private void OnDepositSet(BigInteger stakedDeposit)
        {
            var states = States.Instance;
            var level = states.StakingLevel;
            levelIconImage.sprite = stakeIconData.GetIcon(level, IconType.Small);
            for (var i = 0; i < levelImages.Length; i++)
            {
                levelImages[i].enabled = i < level;
            }

            for (int i = 0; i < myLevelHighlightObjects.Length; i++)
            {
                myLevelHighlightObjects[i].SetActive(i + 1 == level);
            }

            var grindingBonus = 0;
            if (TableSheets.Instance.CrystalMonsterCollectionMultiplierSheet.TryGetValue(level, out var crystalRow))
            {
                grindingBonus = crystalRow.Multiplier;
            }

            var apUsingPercent = 100;
            if (TableSheets.Instance.StakeActionPointCoefficientSheet.TryGetValue(level, out var apRow))
            {
                apUsingPercent = apRow.Coefficient;
            }

            var totalDepositNcg = states.StakedBalanceState.Gold + states.GoldBalanceState.Gold;
            depositText.text = $"<Style=G0>{totalDepositNcg.GetQuantityString()}";
            stakedNcgValueText.text = stakedDeposit.ToString();

            buffBenefitsViews[0].Set(
                string.Format(BuffBenefitRateFormat, L10nManager.Localize("ARENA_REWARD_BONUS"),
                    _tempArenaBonusValues[level]));
            buffBenefitsViews[1].Set(
                string.Format(BuffBenefitRateFormat, L10nManager.Localize("GRINDING_CRYSTAL_BONUS"),
                    grindingBonus));
            buffBenefitsViews[2].Set(
                string.Format(ActionPointBuffFormat, L10nManager.Localize("STAGE_AP_BONUS"),
                    100 - apUsingPercent));
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
                            var ticker = regular.Rewards[i].CurrencyTicker;
                            interestBenefitsViews[i].Set(
                                ticker,
                                (int) result,
                                ticker.Equals(Currencies.Crystal.Ticker));
                            break;
                    }
                }
            }

            var remainingBlock = Math.Max(rewardBlockInterval - waitedBlockRange, 0);
            remainingBlockText.text = string.Format(
                RemainingBlockFormat,
                remainingBlock.BlockRangeToTimeSpanString(),
                remainingBlock.ToString("N0"));

            if (!LoadingHelper.ClaimStakeReward.Value)
            {
                archiveButton.Interactable = remainingBlock == 0;
            }
        }

        private static bool TryGetWaitedBlockIndex(
            long blockIndex,
            int rewardBlockInterval,
            out long waitedBlockRange)
        {
            var stakeState = States.Instance.StakeStateV2;
            if (!stakeState.HasValue)
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

        private void OnChangeEditingState(bool isEdit)
        {
            editingUIParent.SetActive(isEdit);
            defaultUIParent.SetActive(!isEdit);
            stakingNcgInputField.interactable = isEdit;
        }
    }
}
