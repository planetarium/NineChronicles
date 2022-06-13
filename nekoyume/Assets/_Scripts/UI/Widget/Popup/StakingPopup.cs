using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
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
        [SerializeField] private Button closeButton;

        [SerializeField] private ConditionalButton stakingButton;

        [SerializeField] private Image stepImage;

        [SerializeField] private TextMeshProUGUI stepText;

        [SerializeField] private TextMeshProUGUI stepScoreText;

        [SerializeField] private StakingItemView[] regularItemViews;

        [SerializeField] private GameObject tooltip;

        [SerializeField] private RectTransform tooltipRectTransform;

        [SerializeField] private TextMeshProUGUI tooltipText;

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

            tooltip.SetActive(false);

            this.UpdateAsObservable()
                .Where(_ => tooltip.activeSelf && Input.GetMouseButtonDown(0))
                .Subscribe(_ => tooltip.SetActive(false));
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            var level = States.Instance.StakingLevel;
            var deposit = States.Instance.StakedBalanceState?.Gold.MajorUnit ?? 0;
            var regularSheet = Game.Game.instance.TableSheets.StakeRegularRewardSheet;
            var regularFixedSheet = Game.Game.instance.TableSheets.StakeRegularFixedRewardSheet;

            stepImage.sprite = SpriteHelper.GetStakingIcon(level, true);
            stepText.text = $"Step {level}";

            if (regularSheet.TryGetValue(level, out var regular)
                && regularFixedSheet.TryGetValue(level, out var regularFixed))
            {
                var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
                var rewardsCount = Mathf.Min(regular.Rewards.Count, regularItemViews.Length);
                for (int i = 0; i < rewardsCount; i++)
                {
                    var item = ItemFactory.CreateMaterial(materialSheet, regular.Rewards[i].ItemId);

                    var result = deposit / regular.Rewards[i].Rate;
                    var levelBonus = regularFixed.Rewards.FirstOrDefault(
                        reward => reward.ItemId == regular.Rewards[i].ItemId)?.Count ?? 0;
                    result += levelBonus;

                    var view = regularItemViews[i];
                    view.Set(item, result, itemBase =>
                    {
                        ShowToolTip(itemBase, view.gameObject.transform); // 틀림
                    });
                }

                var nextRequired = regularSheet.TryGetValue(level + 1, out var nextLevel)
                    ? nextLevel.RequiredGold
                    : regular.RequiredGold;
                stepScoreText.text = string.Format(StepScoreFormat, deposit, nextRequired);
            }

            base.Show(ignoreStartAnimation);
        }

        private void ShowToolTip(ItemBase itemBase, Transform target)
        {
            tooltip.SetActive(true);
            tooltipText.text = itemBase.GetLocalizedDescription();
            tooltipRectTransform.position = target.position;
        }
    }
}
