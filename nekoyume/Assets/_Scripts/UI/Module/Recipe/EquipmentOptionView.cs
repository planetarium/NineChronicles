using Assets.SimpleLocalization;
using Nekoyume.Helper;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EquipmentOptionView : MonoBehaviour
    {
        [Serializable]
        protected struct OptionText
        {
            public TextMeshProUGUI percentageText;
            public TextMeshProUGUI descriptionText;
        }

        [SerializeField]
        protected TextMeshProUGUI nameText = null;

        [SerializeField]
        protected TextMeshProUGUI descriptionText = null;

        [SerializeField]
        protected OptionText[] optionTexts = null;

        [SerializeField]
        protected Image decoration;

        [SerializeField]
        protected Image panel;

        [SerializeField]
        protected Image innerPanel;

        protected readonly Color disabledColor = new Color(0.5f, 0.5f, 0.5f);
        protected readonly Color disabledYellow = Color.yellow * 0.5f;

        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        public void Show(
            string recipeName,
            int subRecipeId,
            bool isAvailable)
        {
            if (!Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheet
                .TryGetValue(subRecipeId, out var subRecipeRow))
            {
                Debug.LogWarning($"SubRecipe ID not found : {subRecipeId}");
                return;
            }

            SetEnabled(true);

            nameText.text = recipeName;

            var colorTag = isAvailable ?
                "yellow"
                : $"#{ColorHelper.ColorToHexRGBA(disabledYellow)}";

            var format = LocalizationManager.Localize(
               subRecipeRow.MaxOptionLimit == 1 ?
               "UI_RANDOM_OPTION_COUNT_FORMAT_SINGULAR"
               : "UI_RANDOM_OPTION_COUNT_FORMAT_PLURAL");

            descriptionText.text = string.Format(format, colorTag,
                subRecipeRow.MaxOptionLimit == 1 ? 1 : subRecipeRow.MaxOptionLimit);

            var optionSheet = Game.Game.instance.TableSheets.EquipmentItemOptionSheet;
            var skillSheet = Game.Game.instance.TableSheets.SkillSheet;

            for (int i = 0; i < optionTexts.Length; ++i)
            {
                if(i >= subRecipeRow.Options.Count)
                {
                    optionTexts[i].percentageText.enabled = false;
                    optionTexts[i].descriptionText.enabled = false;
                    continue;
                }

                optionTexts[i].percentageText.enabled = true;
                optionTexts[i].descriptionText.enabled = true;

                var optionInfo = subRecipeRow.Options[i];
                optionSheet.TryGetValue(optionInfo.Id, out var optionRow);

                if (optionRow.StatType != StatType.NONE)
                {
                    var statMin = optionRow.StatType == StatType.SPD
                    ? (optionRow.StatMin / 100f).ToString(CultureInfo.InvariantCulture)
                    : optionRow.StatMin.ToString();

                    var statMax = optionRow.StatType == StatType.SPD
                    ? (optionRow.StatMax / 100f).ToString(CultureInfo.InvariantCulture)
                    : optionRow.StatMax.ToString();

                    var description = $"{optionRow.StatType} +({statMin}~{statMax})";
                    SetOptionText(optionTexts[i], optionInfo.Ratio, description);
                }
                else
                {
                    skillSheet.TryGetValue(optionRow.SkillId, out var skillRow);
                    SetOptionText(optionTexts[i], optionInfo.Ratio, skillRow.GetLocalizedName());
                }
            }

            Show();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        protected void SetEnabled(bool value)
        {
            nameText.color = value ? Color.white : disabledColor;
            descriptionText.color = value ? Color.white : disabledColor;

            foreach (var option in optionTexts)
            {
                option.percentageText.color = value ? Color.white : disabledColor;
                option.descriptionText.color = value ? Color.white : disabledColor;
            }

            SetPanelDimmed(!value);
        }

        protected void SetPanelDimmed(bool isDimmed)
        {
            decoration.color = isDimmed ? disabledColor : Color.white;
            panel.color = isDimmed ? disabledColor : Color.white;
            innerPanel.color = isDimmed ? disabledColor : Color.white;
        }

        protected void SetOptionText(OptionText optionText, decimal percentage, string description)
        {
            optionText.percentageText.text = percentage.ToString("0%");
            optionText.descriptionText.text = description;
        }
    }
}
