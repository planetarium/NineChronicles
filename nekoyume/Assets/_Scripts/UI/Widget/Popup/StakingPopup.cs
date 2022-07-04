using System.Linq;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class StakingPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private ConditionalButton stakingButton;

        [SerializeField]
        private Image stepImage;

        [SerializeField]
        private TextMeshProUGUI stepText;

        [SerializeField]
        private TextMeshProUGUI stepScoreText;

        [SerializeField]
        private StakingItemView[] regularItemViews;

        [SerializeField]
        private RectTransform tooltipRectTransform;

        [SerializeField]
        private StakingItemView systemRewardView;

        [SerializeField]
        private TextMeshProUGUI tooltipText;

        [SerializeField]
        private GameObject systemRewardArea;

        private const string StepScoreFormat = "{0}<size=18><color=#A36F56>/{1}</color></size>";

        protected override void Awake()
        {
            base.Awake();

            stakingButton.OnSubmitSubject.Subscribe(_ =>
            {
                // Do Something
                AudioController.PlayClick();
            }).AddTo(gameObject);

            closeButton.onClick.AddListener(() =>
            {
                Close();
                AudioController.PlayClick();
            });

            tooltipRectTransform.gameObject.SetActive(false);

            this.UpdateAsObservable()
                .Where(_ => tooltipRectTransform.gameObject.activeSelf
                            && Input.GetMouseButtonDown(0))
                .Subscribe(_ => tooltipRectTransform.gameObject.SetActive(false))
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            var level = States.Instance.StakingLevel;
            var deposit = States.Instance.StakedBalanceState?.Gold.MajorUnit ?? 0;
            var sheets = TableSheets.Instance;
            var regularSheet = sheets.StakeRegularRewardSheet;
            var regularFixedSheet = sheets.StakeRegularFixedRewardSheet;
            var stakingMultiplierSheet = sheets.CrystalMonsterCollectionMultiplierSheet;

            stepImage.sprite = SpriteHelper.GetStakingIcon(level);
            stepText.text = $"Step {level}";
            systemRewardArea.SetActive(false);

            if (regularSheet.TryGetValue(level, out var regular)
                && regularFixedSheet.TryGetValue(level, out var regularFixed))
            {
                var materialSheet = sheets.MaterialItemSheet;
                var rewardsCount = Mathf.Min(regular.Rewards.Count, regularItemViews.Length);
                for (var i = 0; i < rewardsCount; i++)
                {
                    var item = ItemFactory.CreateMaterial(materialSheet, regular.Rewards[i].ItemId);

                    var result = deposit / regular.Rewards[i].Rate;
                    var levelBonus = regularFixed.Rewards.FirstOrDefault(
                        reward => reward.ItemId == regular.Rewards[i].ItemId)?.Count ?? 0;
                    result += levelBonus;

                    var view = regularItemViews[i];
                    view.Set(item, result, itemBase =>
                    {
                        ShowToolTip(itemBase, view.transform);
                    });
                }

                var nextRequired = regularSheet.TryGetValue(level + 1, out var nextLevel)
                    ? nextLevel.RequiredGold
                    : regular.RequiredGold;
                stepScoreText.text = string.Format(StepScoreFormat, deposit, nextRequired);
            }

            if (stakingMultiplierSheet.TryGetValue(level, out var row))
            {
                if (row.Multiplier > 0)
                {
                    systemRewardArea.SetActive(true);
                    systemRewardView.Set(
                        $"{row.Multiplier}%",
                        () => ShowToolTip(L10nManager.Localize("UI_STAKING_BONUS_CRYSTAL"),
                            systemRewardView.transform));
                }
            }

            base.Show(ignoreStartAnimation);
        }

        private void ShowToolTip(ItemBase itemBase, Transform target)
        {
            ShowToolTip(itemBase.GetLocalizedDescription(), target);
        }

        private void ShowToolTip(string message, Transform target)
        {
            tooltipText.text = message;
            tooltipRectTransform.position = target.position;
            tooltipRectTransform.gameObject.SetActive(true);
        }
    }
}
