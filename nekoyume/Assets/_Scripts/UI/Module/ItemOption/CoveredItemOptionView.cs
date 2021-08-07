using Nekoyume.L10n;
using Nekoyume.Model.Stat;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class CoveredItemOptionView : ItemOptionWithCountView
    {
        [SerializeField]
        private GameObject _coverObject;

        [SerializeField]
        private TextMeshProUGUI _coverText;

        private static readonly int AnimatorHashDiscover = Animator.StringToHash("Discover");

        public void Show(
            string leftText,
            string rightText,
            int optionCount,
            string coverText,
            bool ignoreAnimation = false)
        {
            UpdateView(leftText, rightText, optionCount, coverText);
            Show(ignoreAnimation);
        }

        public void Discover(bool ignoreAnimation = false)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (ignoreAnimation)
            {
                animator.Play(AnimatorHashDiscover, 0, 1f);
            }
            else
            {
                animator.SetTrigger(AnimatorHashDiscover);
            }
        }

        public void UpdateView(string leftText, string rightText, int optionCount, string coverText)
        {
            UpdateView(leftText, rightText, optionCount);
            UpdateText(_coverText, coverText);

            IsEmpty = IsEmpty && string.IsNullOrEmpty(coverText);
        }

        public override void UpdateAsStatWithCount(StatType type, int value, int count) =>
            UpdateView(
                $"{type.ToString()} {value}",
                string.Empty,
                count,
                L10nManager.Localize("UI_ITEM_OPTION_COVER_TEXT_FORMAT", type.ToString()));

        public override void UpdateAsTotalAndPlusStatWithCount(StatType type, int totalValue, int count, int plusValue) =>
            UpdateView(
                $"{type.ToString()} {totalValue}",
                $"+{plusValue}",
                count,
                L10nManager.Localize("UI_ITEM_OPTION_COVER_TEXT_FORMAT", type.ToString()));

        public override void UpdateAsSkill(string skillName, int totalPower, int totalChance) =>
            UpdateView(
                $"{skillName} {totalPower} / {totalChance}%",
                string.Empty,
                1,
                L10nManager.Localize("UI_ITEM_OPTION_COVER_TEXT_FORMAT", skillName));

        public override void UpdateAsTotalAndPlusSkill(
            string skillName,
            int totalPower,
            int totalChance,
            int plusPower,
            int plusChance) =>
            UpdateView(
                $"{skillName} {totalPower} / {totalChance}%",
                $"+{plusPower} / +{plusChance}%",
                1,
                L10nManager.Localize("UI_ITEM_OPTION_COVER_TEXT_FORMAT", skillName));

        public override void UpdateToEmpty() =>
            UpdateView(string.Empty, string.Empty, 0, string.Empty);
    }
}
