using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class StakingPopup : PopupWidget
    {
        [SerializeField] private GameObject none;
        [SerializeField] private GameObject content;

        [Header("None")]
        [SerializeField] private Button closeButtonNone;
        [SerializeField] private ConditionalButton stakingButtonNone;

        [Header("Top")]
        [SerializeField] private Image levelIconImage;
        [SerializeField] private Image[] levelImages;
        [SerializeField] private TextMeshProUGUI depositText;
        [SerializeField] private Image depositGaugeImage;
        [SerializeField] private TextMeshProUGUI remainingDepositText;
        [SerializeField] private Button stakingButton;
        [SerializeField] private Button closeButton;

        [Header("Center")]
        [SerializeField] private StakingBuffBenefitsView[] buffBenefitsViews;
        [SerializeField] private StakingInterestBenefitsView[] interestBenefitsViews;
        [SerializeField] private GameObject remainingBlockArea;
        [SerializeField] private TextMeshProUGUI remainingBlockText;
        [SerializeField] private GameObject archiveButtonArea;
        [SerializeField] private ConditionalButton archiveButton;

        [SerializeField] private StakingBenefitsListView[] benefitsListViews;

        [Header("Bottom")]
        [SerializeField] private CategoryTabButton currentBenefitsTabButton;
        [SerializeField] private CategoryTabButton levelBenefitsTabButton;
        [SerializeField] private GameObject currentBenefitsTab;
        [SerializeField] private GameObject levelBenefitsTab;

        [SerializeField] private StakeIconDataScriptableObject stakeIconData;

        private readonly ReactiveProperty<BigInteger> _deposit = new();
        private readonly Module.ToggleGroup _toggleGroup = new ();

        // temporary table - buff benefits (HAS ap, arena ranking)
        // TODO: THIS IS TEMPORARY TABLE. IT MUST CHANGE TO TABLESHEETS.
        private readonly Dictionary<int, (int ap, int arena)> _buffBenefits = new()
        {
            {1, (0, 0)}, {2, (0, 100)}, {3, (20, 200)}, {4, (20, 200)}, {5, (40, 200)}, {6, (40, 200)}, {7, (40, 200)}
        };
        // temporary table - benefits rate
        private readonly Dictionary<int, int> _benefitRates = new()
        {
            {1, 20}, {2, 50}, {3, 100}, {4, 500}, {5, 1000}, {6, 1000}, {7, 1000}
        };

        private readonly StakingBenefitsListView.Model[] _cachedModel =
            new StakingBenefitsListView.Model[8];

        public const string StakingUrl = "ninechronicles-launcher://open/monster-collection";
        private const string ActionPointBuffFormat = "{0} <color=#1FFF00>{1}% DC</color>";
        private const string BuffBenefitRateFormat = "{0} <color=#1FFF00>+{1}%</color>";
        private const string RemainingBlockFormat = "<Style=G5>{0}({1})";

        protected override void Awake()
        {
            base.Awake();

            _toggleGroup.RegisterToggleable(currentBenefitsTabButton);
            _toggleGroup.RegisterToggleable(levelBenefitsTabButton);
            currentBenefitsTabButton.OnClick.Subscribe(_ =>
            {
                currentBenefitsTab.SetActive(true);
                levelBenefitsTab.SetActive(false);
            }).AddTo(gameObject);
            levelBenefitsTabButton.OnClick.Subscribe(_ =>
            {
                currentBenefitsTab.SetActive(false);
                levelBenefitsTab.SetActive(true);
            }).AddTo(gameObject);

            stakingButtonNone.OnClickSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                Application.OpenURL(StakingUrl);
            }).AddTo(gameObject);
            closeButtonNone.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Close();
            });

            stakingButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Application.OpenURL(StakingUrl);
            });
            closeButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Close();
            });

            archiveButton.OnClickSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                Application.OpenURL(StakingUrl);
            }).AddTo(gameObject);

            SetBenefitsListViews();
            _deposit.Subscribe(OnDepositEdited).AddTo(gameObject);
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Where(_ => gameObject.activeSelf).Subscribe(OnBlockUpdated)
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            var level = States.Instance.StakingLevel;
            var deposit = States.Instance.StakedBalanceState?.Gold.MajorUnit ?? 0;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;

            if (level < 1)
            {
                none.SetActive(true);
                content.SetActive(false);
                base.Show(ignoreStartAnimation);
                return;
            }

            _deposit.Value = deposit;
            OnBlockUpdated(blockIndex);
            _toggleGroup.SetToggledOffAll();
            currentBenefitsTabButton.OnClick.OnNext(currentBenefitsTabButton);
            currentBenefitsTabButton.SetToggledOn();
            none.SetActive(false);
            content.SetActive(true);

            base.Show(ignoreStartAnimation);
        }

        private void OnDepositEdited(BigInteger deposit)
        {
            var level = States.Instance.StakingLevel;
            if (level < 1) return;

            var sheets = TableSheets.Instance;
            var regularSheet = sheets.StakeRegularRewardSheet;

            levelIconImage.sprite = stakeIconData.GetIcon(level, IconType.Small);
            for (var i = 0; i < levelImages.Length; i++)
            {
                levelImages[i].enabled = i < level;
            }

            if (regularSheet.TryGetValue(level, out var regular))
            {
                var nextRequired = regularSheet.TryGetValue(level + 1, out var nextLevel)
                    ? nextLevel.RequiredGold
                    : regular.RequiredGold;
                depositText.text = deposit.ToString("N0");
                depositGaugeImage.fillAmount = (float)deposit / nextRequired;
                remainingDepositText.text = level >= 7
                    ? ""
                    : L10nManager.Localize("UI_REMAINING_NCG_TO_NEXT_LEVEL", nextRequired - deposit);
            }

            buffBenefitsViews[0].Set(
                string.Format(BuffBenefitRateFormat, L10nManager.Localize("ARENA_REWARD_BONUS"),
                    _cachedModel[level].ArenaRewardBuff), _benefitRates[level]);
            buffBenefitsViews[1].Set(
                string.Format(BuffBenefitRateFormat, L10nManager.Localize("GRINDING_CRYSTAL_BONUS"),
                    _cachedModel[level].CrystalBuff), _benefitRates[level]);
            buffBenefitsViews[2].Set(
                string.Format(ActionPointBuffFormat, L10nManager.Localize("STAGE_AP_BONUS"),
                    _cachedModel[level].ActionPointBuff), _benefitRates[level]);

            for (int i = 0; i <= 7; i++)
            {
                benefitsListViews[i].Set(i, level);
            }
        }

        private void OnBlockUpdated(long blockIndex)
        {
            var level = States.Instance.StakingLevel;
            var deposit = States.Instance.StakedBalanceState?.Gold.MajorUnit ?? 0;

            var sheets = TableSheets.Instance;
            var regularSheet = sheets.StakeRegularRewardSheet;
            var regularFixedSheet = sheets.StakeRegularFixedRewardSheet;
            var stakeStateV2 = States.Instance.StakeStateV2;
            var rewardBlockInterval = stakeStateV2.HasValue
                ? (int)stakeStateV2.Value.Contract.RewardInterval
                : (int)StakeState.RewardInterval;

            if (!TryGetWaitedBlockIndex(blockIndex, rewardBlockInterval, out var waitedBlockRange))
            {
                return;
            }

            var rewardCount = (int)waitedBlockRange / rewardBlockInterval;

            if (regularSheet.TryGetValue(level, out var regular)
                && regularFixedSheet.TryGetValue(level, out var regularFixed))
            {
                var materialSheet = sheets.MaterialItemSheet;
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
                    switch (regular.Rewards[i].Type)
                    {
                        case StakeRegularRewardSheet.StakeRewardType.Item:
                            interestBenefitsViews[i].Set(
                                ItemFactory.CreateMaterial(materialSheet, regular.Rewards[i].ItemId),
                                (int)result,
                                _benefitRates[level]);
                            break;
                        case StakeRegularRewardSheet.StakeRewardType.Rune:
                            interestBenefitsViews[i].Set(
                                regular.Rewards[i].ItemId,
                                (int)result,
                                _benefitRates[level]);
                            break;
                        case StakeRegularRewardSheet.StakeRewardType.Currency:
                            interestBenefitsViews[i].Set(
                                regular.Rewards[i].CurrencyTicker,
                                (int)result,
                                _benefitRates[level]);
                            break;
                    }
                }
            }

            if (rewardCount > 0)
            {
                archiveButtonArea.SetActive(true);
                remainingBlockArea.SetActive(false);
            }
            else
            {
                var remainingBlock = rewardBlockInterval - waitedBlockRange;
                remainingBlockText.text = string.Format(
                    RemainingBlockFormat,
                    remainingBlock.ToString("N0"),
                    remainingBlock.BlockRangeToTimeSpanString());

                archiveButtonArea.SetActive(false);
                remainingBlockArea.SetActive(true);
            }
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
            var sheets = TableSheets.Instance;
            var regularSheet = sheets.StakeRegularRewardSheet;
            var regularFixedSheet = sheets.StakeRegularFixedRewardSheet;
            var stakingMultiplierSheet = sheets.CrystalMonsterCollectionMultiplierSheet;

            for (int level = 1; level <= 7; level++)
            {
                var model = new StakingBenefitsListView.Model();

                if (regularSheet.TryGetValue(level, out var regular)
                    && regularFixedSheet.TryGetValue(level, out var regularFixed))
                {
                    model.RequiredDeposit = regular.RequiredGold;
                    model.HourGlassInterest = GetReward(regular, regularFixed,regular.RequiredGold, 0);
                    model.ApPotionInterest = GetReward(regular, regularFixed, regular.RequiredGold, 1);
                    model.RuneInterest = GetReward(regular, regularFixed, regular.RequiredGold, 2);

                    model.CrystalInterest = GetReward(regular, regularFixed, regular.RequiredGold, GetCrystalRewardIndex(regular));
                    model.GoldenPowderInterest = GetReward(regular, regularFixed, regular.RequiredGold, GetGoldenPowderRewardIndex(regular));
                }

                model.BenefitRate = _benefitRates[level];
                if (stakingMultiplierSheet.TryGetValue(level, out var row))
                {
                    model.CrystalBuff = row.Multiplier;
                }
                model.ActionPointBuff = _buffBenefits[level].ap;
                model.ArenaRewardBuff = _buffBenefits[level].arena;

                benefitsListViews[level].Set(level, model);
                _cachedModel[level] = model;
            }
        }

        private static long GetReward(
            StakeRegularRewardSheet.Row regular,
            StakeRegularFixedRewardSheet.Row regularFixed,
            long deposit, int index)
        {
            if (regular.Rewards.Count <= index || index < 0)
            {
                return 0;
            }

            var result = (long)Math.Truncate(deposit / regular.Rewards[index].DecimalRate);
            var levelBonus = regularFixed.Rewards.FirstOrDefault(
                reward => reward.ItemId == regular.Rewards[index].ItemId)?.Count ?? 0;

            return result + levelBonus;
        }

        private static int GetCrystalRewardIndex(StakeRegularRewardSheet.Row regular)
        {
            for (int i = 0; i < regular.Rewards.Count; i++)
            {
                if (regular.Rewards[i].CurrencyTicker == "CRYSTAL")
                    return i;
            }
            return -1;
        }

        private static int GetGoldenPowderRewardIndex(StakeRegularRewardSheet.Row regular)
        {
            for (int i = 0; i < regular.Rewards.Count; i++)
            {
                if (regular.Rewards[i].ItemId == 600201)
                    return i;
            }
            return -1;
        }
    }
}
