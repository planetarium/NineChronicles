using Nekoyume.L10n;
using Nekoyume.Model.Stat;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class CoveredItemOptionView : ItemOptionWithCountView
    {
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

        public void UpdateAsStatWithCount(StatType type, long value, int count) =>
            UpdateView(
                $"{type} +{type.ValueToString(value)}",
                string.Empty,
                count,
                L10nManager.Localize("UI_ITEM_OPTION_COVER_TEXT_FORMAT", type.ToString()));

        public void UpdateAsSkill(string skillName, string powerString, int totalChance) =>
            UpdateView(
                $"{skillName} {powerString} / {totalChance}%",
                string.Empty,
                1,
                L10nManager.Localize("UI_ITEM_OPTION_COVER_TEXT_FORMAT", skillName));

        public override void UpdateToEmpty() =>
            UpdateView(string.Empty, string.Empty, 0, string.Empty);
    }
}
