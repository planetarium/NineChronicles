using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class StakingPopupNew : PopupWidget
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
        [SerializeField] private Transform buffBenefitsCellContainer;
        [SerializeField] private StakingBuffBenefitsView buffBenefitsPrefab;
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

        private readonly ReactiveProperty<BigInteger> _deposit = new ReactiveProperty<BigInteger>();
        private readonly Module.ToggleGroup _toggleGroup = new Module.ToggleGroup();
        private Dictionary<int, (int ap, int arena)> _buffBenefits;

        private const string RemainingBlockFormat = "<Style=G5>{0}({1})";

        [Header("Test")]
        [SerializeField] private int testLevel;
        [SerializeField] private int testDeposit;
        [SerializeField] private long textBlockIndex;

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

            _deposit.Subscribe(OnDepositEdited).AddTo(gameObject);

            stakingButtonNone.OnClickSubject.Subscribe(_ =>
            {
                // Move to Staking
                AudioController.PlayClick();
            }).AddTo(gameObject);
            closeButtonNone.onClick.AddListener(() =>
            {
                Close();
                AudioController.PlayClick();
            });
            stakingButton.onClick.AddListener(() =>
            {
                // Move to Staking
                AudioController.PlayClick();
            });
            closeButton.onClick.AddListener(() =>
            {
                Close();
                AudioController.PlayClick();
            });
            archiveButton.OnClickSubject.Subscribe(_ =>
            {
                // Move to Archive
                AudioController.PlayClick();
            }).AddTo(gameObject);

            // temporary table - buff benefits (HAS ap, arena ranking)
            _buffBenefits = new Dictionary<int, (int ap, int arena)>
            {
                {1, (5, 0)}, {2, (4, 100)}, {3, (4, 200)}, {4, (3, 200)}, {5, (3, 200)},
            };

            var sheets = TableSheets.Instance;
            var regularSheet = sheets.StakeRegularRewardSheet;
            var regularFixedSheet = sheets.StakeRegularFixedRewardSheet;
            var stakingMultiplierSheet = sheets.CrystalMonsterCollectionMultiplierSheet;

            for (int level = 1; level <= 5; level++)
            {
                var model = new StakingBenefitsListView.Model();
                if (regularSheet.TryGetValue(level, out var regular)
                    && regularFixedSheet.TryGetValue(level, out var regularFixed))
                {
                    var hourGlass = regular.RequiredGold / regular.Rewards[0].Rate;
                    var levelBonus1 = regularFixed.Rewards.FirstOrDefault(
                        reward => reward.ItemId == regular.Rewards[0].ItemId)?.Count ?? 0;
                    hourGlass += levelBonus1;
                    var apPotion = regular.RequiredGold / regular.Rewards[1].Rate;
                    var levelBonus2 = regularFixed.Rewards.FirstOrDefault(
                        reward => reward.ItemId == regular.Rewards[1].ItemId)?.Count ?? 0;
                    apPotion += levelBonus2;
                    model.RequiredDeposit = regular.RequiredGold;
                    model.HourGlassInterest = hourGlass;
                    model.ApPotionInterest = apPotion;
                }
                model.RequiredDeposit = regular.RequiredGold;


                if (stakingMultiplierSheet.TryGetValue(level, out var row))
                {
                    model.CrystalBuff = row.Multiplier;
                }
                model.ActionPointBuff = _buffBenefits[level].ap;
                model.ArenaRewardBuff = _buffBenefits[level].arena;

                benefitsListViews[level].Set(level, model);
            }
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            var level = testLevel;
            var deposit = (BigInteger)testDeposit;

            if (level < 1)
            {
                none.SetActive(true);
                content.SetActive(false);
                base.Show(ignoreStartAnimation);
                return;
            }

            _deposit.Value = deposit;
            OnBlockUpdated(textBlockIndex);
            currentBenefitsTabButton.OnClick.OnNext(currentBenefitsTabButton);
            currentBenefitsTabButton.SetToggledOn();
            none.SetActive(false);
            content.SetActive(true);

            base.Show(ignoreStartAnimation);
        }

        private void OnDepositEdited(BigInteger deposit)
        {
            var level = testLevel;
            var sheets = TableSheets.Instance;
            var regularSheet = sheets.StakeRegularRewardSheet;

            levelIconImage.sprite = SpriteHelper.GetStakingIcon(level, IconType.Small);
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
                remainingDepositText.text = level >= 5 ? "" : (nextRequired - deposit).ToString("N2");
            }

            Instantiate(buffBenefitsPrefab, buffBenefitsCellContainer).Set("", 0, 100); // Todo : fill

            for (int i = 0; i <= 5; i++)
            {
                benefitsListViews[i].Set(i, level);
            }
        }

        private void OnBlockUpdated(long blockIndex)
        {
            var level = testLevel;
            var deposit = (BigInteger)testDeposit;

            var sheets = TableSheets.Instance;
            var regularSheet = sheets.StakeRegularRewardSheet;
            var regularFixedSheet = sheets.StakeRegularFixedRewardSheet;

            var stakedBlocks = lastEditedBlock - blockIndex;
            var rewardCount = (int)stakedBlocks / rewardBlockRange;

            if (regularSheet.TryGetValue(level, out var regular)
                && regularFixedSheet.TryGetValue(level, out var regularFixed))
            {
                var materialSheet = sheets.MaterialItemSheet;
                var rewardsCount = Mathf.Min(regular.Rewards.Count, interestBenefitsViews.Length);
                for (var i = 0; i < rewardsCount; i++)
                {
                    var item = ItemFactory.CreateMaterial(materialSheet, regular.Rewards[i].ItemId);
                    var result = (int)deposit / regular.Rewards[i].Rate;
                    var levelBonus = regularFixed.Rewards.FirstOrDefault(
                        reward => reward.ItemId == regular.Rewards[i].ItemId)?.Count ?? 0;
                    result += levelBonus;
                    result *= rewardCount;

                    interestBenefitsViews[i].Set(item, result, 100);
                }
            }

            if (rewardCount > 0)
            {
                archiveButtonArea.SetActive(false);
                remainingBlockArea.SetActive(true);
            }
            else
            {
                var remainingBlock = rewardBlockRange - stakedBlocks;
                remainingBlockText.text = string.Format(
                    RemainingBlockFormat,
                    remainingBlock.ToString("N0"),
                    remainingBlock.BlockRangeToTimeSpanString());

                archiveButtonArea.SetActive(false);
                remainingBlockArea.SetActive(true);
            }
        }
    }
}
