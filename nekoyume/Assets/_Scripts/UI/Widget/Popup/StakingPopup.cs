using System;
using System.Linq;
using System.Numerics;
using Cysharp.Threading.Tasks;
using Lib9c;
using Libplanet.Types.Assets;
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
        [Serializable]
        private struct CurrencyView
        {
            public GameObject view;
            public TMP_Text amountText;
        }

        [Header("Top")]
        [SerializeField] private Image levelIconImage;

        [SerializeField] private Image[] levelImages;
        [SerializeField] private TextMeshProUGUI depositText; // it shows having NCG, not staked.
        [SerializeField] private TextMeshProUGUI stakedNcgValueText; // it shows staked NCG, not having.
        [SerializeField] private Button closeButton;
        [SerializeField] private Button migrateButton;
        [SerializeField] private Button stakingStartButton;
        [SerializeField] private GameObject editingUIParent;
        [SerializeField] private GameObject defaultUIParent;
        [SerializeField] private TMP_InputField stakingNcgInputField;

        [Header("Editing")]
        [SerializeField] private Button ncgEditButton;
        [SerializeField] private Button editCancelButton;
        [SerializeField] private ConditionalButton editSaveButton;
        [SerializeField] private ConditionalButton unbondButton;

        [Header("Center")]
        [SerializeField] private StakingBuffBenefitsView[] buffBenefitsViews;
        [SerializeField] private GameObject currentBenefitsTab;
        [SerializeField] private GameObject levelBenefitsTab;

        [Space]
        [SerializeField] private StakingInterestBenefitsView[] interestBenefitsViews;
        [SerializeField] private TextMeshProUGUI remainingBlockText;
        [SerializeField] private ConditionalButton archiveButton;

        [Space]
        [SerializeField] private TMP_Text rewardNcgText;
        [SerializeField] private ConditionalButton ncgArchiveButton;
        [SerializeField] private GameObject ncgLoadingIndicator;

        [Space]
        [SerializeField] private RectTransform scrollableRewardsRectTransform;
        [SerializeField] private Image stakingLevelImage;
        [SerializeField] private Image stakingRewardImage;
        [SerializeField] private GameObject[] myLevelHighlightObjects;

        [Header("Bottom")]
        [SerializeField] private CategoryTabButton currentBenefitsTabButton;
        [SerializeField] private CategoryTabButton levelBenefitsTabButton;

        [Header("etc")]
        [SerializeField] private StakeIconDataScriptableObject stakeIconData;
        [SerializeField] private GameObject stakingInformationObject;
        [SerializeField] private UIBackground informationBg;

        private readonly Module.ToggleGroup _toggleGroup = new();

        private readonly Color _normalInputFieldColor = new(0xEB, 0xCE, 0xB1, 0xFF);

        private const string ActionPointBuffFormat = "{0} <color=#1FFF00>{1}% DC</color>";
        private const string BuffBenefitRateFormat = "{0} <color=#1FFF00>+{1}%</color>";
        private const string RemainingBlockFormat = "<Style=G5>{0}({1})";
        private const string TableSheetViewUrlPrefix = "https://9c-board.nine-chronicles.dev/odin/tablesheet/";

        private bool _benefitListViewsInitialized;

        private int[] _tempArenaBonusValues = { 0, 0, 100, 200, 200, 200, 200, 200, 200 };

        private long _getUnbondClaimableHeight = -1;

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
            editSaveButton.OnSubmitSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                OnClickSaveButton();
            });
            unbondButton.OnSubmitSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                ActionManager.Instance.ClaimUnbonded();
                unbondButton.SetCondition(() => false);
                unbondButton.UpdateObjects();
            });
            editCancelButton.onClick.AddListener(() => { OnChangeEditingState(false); });
            informationBg.OnClick = () => { stakingInformationObject.SetActive(false); };
            stakingNcgInputField.onEndEdit.AddListener(value =>
            {
                var totalDeposit = (States.Instance.GoldBalanceState.Gold +
                        States.Instance.StakedBalance)
                    .MajorUnit;
                stakingNcgInputField.textComponent.color =
                    BigInteger.TryParse(value, out var inputBigInt) && totalDeposit < inputBigInt
                        ? Palette.GetColor(ColorType.TextDenial)
                        : _normalInputFieldColor;
            });
            ncgArchiveButton.OnSubmitSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                ActionManager.Instance
                    .ClaimReward()
                    .Subscribe();
                SetNcgArchiveButtonLoading(true);
            }).AddTo(gameObject);
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

            CheckClaimNcgReward().Forget();
        }

        private async UniTask CheckUnbondBlock()
        {
            var agent = Game.Game.instance.Agent;
            editSaveButton.SetCondition(() => false);
            editSaveButton.UpdateObjects();
            unbondButton.SetCondition(() => false);
            unbondButton.UpdateObjects();

            var value = await agent.GetUnbondClaimableHeightByBlockHashAsync(States.Instance.AgentState.address);
            editSaveButton.SetCondition(() => true);
            editSaveButton.UpdateObjects();

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            unbondButton.SetCondition(() => true);
            unbondButton.Interactable = value != -1 && value <= blockIndex;
            unbondButton.UpdateObjects();

            _getUnbondClaimableHeight = value;
        }

        public void OnRenderClaimUnbonded()
        {
            unbondButton.SetCondition(() => false);
            unbondButton.Interactable = false;
            unbondButton.UpdateObjects();
            _getUnbondClaimableHeight = -1;
        }

        public async UniTask CheckClaimNcgReward(bool callByActionRender = false)
        {
            var agent = Game.Game.instance.Agent;
            rewardNcgText.gameObject.SetActive(false);
            ncgLoadingIndicator.SetActive(true);

            if (ncgArchiveButton.CurrentState.Value ==
                ConditionalButton.State.Conditional)
            {
                return;
            }

            ncgArchiveButton.Interactable = false;
            ncgArchiveButton.UpdateObjects();

            var claimableRewards = await agent.GetClaimableRewardsByBlockHashAsync(States.Instance.AgentState.address);
            var fungibleAssetValue = new FungibleAssetValue(claimableRewards[0]);
            var claimableQuantity = fungibleAssetValue.RawValue;

            // action render시점에서 claimableQuantity가 이전 값을 갖고 있어 강제로 0으로 설정
            if (callByActionRender)
            {
                claimableQuantity = 0;
            }

            ncgArchiveButton.Interactable = claimableQuantity >= 1;
            ncgArchiveButton.UpdateObjects();

            rewardNcgText.text = $"+{fungibleAssetValue.GetQuantityString()}";
            rewardNcgText.gameObject.SetActive(true);
            ncgLoadingIndicator.gameObject.SetActive(false);
        }

        public void SetNcgArchiveButtonLoading(bool loading)
        {
            ncgArchiveButton.SetCondition(() => !loading);
            ncgArchiveButton.UpdateObjects();
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            var anchoredPosition = scrollableRewardsRectTransform.anchoredPosition;
            anchoredPosition.x = 0;
            scrollableRewardsRectTransform.anchoredPosition = anchoredPosition;
        }

        public void SetView()
        {
            var deposit = States.Instance.StakedBalance.MajorUnit;
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
            ncgEditButton.gameObject.SetActive(hasStakeState);

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

            if (requiredMigrate)
            {
                var confirmSystem = Find<IconAndButtonSystem>();
                confirmSystem.ShowWithTwoButton("UI_ITEM_INFORMATION", "MIGRATE_NOTICE", type: IconAndButtonSystem.SystemType.Information);
                confirmSystem.ConfirmCallback = OnClickMigrateButton;
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

            CheckUnbondBlock().Forget();
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
                L10nManager.Localize("UI_OK"),
                L10nManager.Localize("UI_CANCEL"),
                false,
                IconAndButtonSystem.SystemType.Information);
            var disposable = confirmUI.ContentText.SubscribeForClickLink(linkInfo => { Application.OpenURL(linkInfo.GetLinkID()); });
            confirmUI.ConfirmCallback = () =>
            {
                var majorUnit = States.Instance.StakedBalance.MajorUnit;
                var avatarAddress = States.Instance.CurrentAvatarState.address;
                ActionManager.Instance.Stake(majorUnit, avatarAddress).Subscribe();
                disposable.Dispose();
                OnChangeEditingState(false);
            };
            confirmUI.CancelCallback = () => { disposable.Dispose(); };
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

            if (inputBigInt == States.Instance.StakedBalance.MajorUnit)
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
                States.Instance.StakedBalance;
            if (inputBigInt > totalDepositNcg.MajorUnit)
            {
                Find<PaymentPopup>().ShowLackPaymentNCG(inputBigInt.ToString(), true);
                Close(true);
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
                var cancellableBlockIndex = stakeState.ReceivedBlockIndex;
                if (inputBigInt < States.Instance.StakedBalance.MajorUnit)
                {
                    if (cancellableBlockIndex > Game.Game.instance.Agent.BlockIndex)
                    {
                        OneLineSystem.Push(MailType.System,
                            L10nManager.Localize("UI_STAKING_LOCK_BLOCK_TIP_FORMAT",
                                cancellableBlockIndex),
                            NotificationCell.NotificationType.UnlockCondition);
                        return;
                    }

                    if (_getUnbondClaimableHeight != -1)
                    {
                        // TODO: unbond 기달리라고 말하기
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

            confirmUI.ShowWithTwoButton(confirmTitle, confirmContent, localize: true, type: confirmIcon);
            confirmUI.ConfirmCallback = () =>
            {
                var majorUnit = BigInteger.Parse(stakingNcgInputField.text);
                var avatarAddress = States.Instance.CurrentAvatarState.address;
                ActionManager.Instance.Stake(majorUnit, avatarAddress).Subscribe();
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

            for (var i = 0; i < myLevelHighlightObjects.Length; i++)
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

            var totalDepositNcg = states.StakedBalance + states.GoldBalanceState.Gold;
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
            var deposit = states.StakedBalance.MajorUnit;
            var regularSheet = states.StakeRegularRewardSheet;
            var regularFixedSheet = states.StakeRegularFixedRewardSheet;
            var stakeStateV2 = states.StakeStateV2;
            var rewardBlockInterval = stakeStateV2.HasValue
                ? (int)stakeStateV2.Value.Contract.RewardInterval
                : (int)LegacyStakeState.RewardInterval;

            TryGetWaitedBlockIndex(blockIndex, rewardBlockInterval, out var waitedBlockRange);
            var rewardCount = (int)waitedBlockRange / rewardBlockInterval;
            regularSheet.TryGetValue(level, out var regular);
            regularFixedSheet.TryGetValue(level, out var regularFixed);

            var materialSheet = TableSheets.Instance.MaterialItemSheet;
            for (var i = 0; i < interestBenefitsViews.Length; i++)
            {
                var result = GetReward(regular, regularFixed, (long)deposit, i);
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
                                (int)result);
                            break;
                        case StakeRegularRewardSheet.StakeRewardType.Rune:
                            interestBenefitsViews[i].Set(
                                regular.Rewards[i].ItemId,
                                (int)result);
                            break;
                        case StakeRegularRewardSheet.StakeRewardType.Currency:
                            var ticker = regular.Rewards[i].CurrencyTicker;
                            interestBenefitsViews[i].Set(
                                ticker,
                                (int)result,
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
