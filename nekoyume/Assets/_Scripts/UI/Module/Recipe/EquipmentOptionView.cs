using Assets.SimpleLocalization;
using Nekoyume.Helper;
using Nekoyume.Model.Stat;
using System;
using System.Globalization;
using TMPro;
using UnityEngine;
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

        private static readonly Color DimmedColor = new Color(0.5f, 0.5f, 0.5f);
        private static readonly Color DisabledYellow = Color.yellow * 0.5f;
        private const string ColorTagForDescriptionText1 = "yellow";
        private static readonly string ColorTagForDescriptionText2 = $"#{ColorHelper.ColorToHexRGBA(DisabledYellow)}";

        [SerializeField]
        protected TextMeshProUGUI nameText = null;

        [SerializeField]
        protected TextMeshProUGUI descriptionText = null;

        [SerializeField]
        protected OptionText[] optionTexts = null;

        [SerializeField]
        protected Image decoration = null;

        [SerializeField]
        protected Image panel = null;

        [SerializeField]
        protected Image innerPanel;

        [SerializeField]
        protected readonly Color[] optionColors =
        {
            Color.white,
            Color.white,
            Color.green,
            //purple
            ColorHelper.HexToColorRGB("ba00ff")
        };

        public int SubRecipeId { get; private set; }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Show(string recipeName, int subRecipeId)
        {
            if (!Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheet
                .TryGetValue(subRecipeId, out var subRecipeRow))
            {
                Debug.LogWarning($"SubRecipe ID not found : {subRecipeId}");
                return;
            }

            SubRecipeId = subRecipeId;
            nameText.text = recipeName;

            var format = LocalizationManager.Localize(
                subRecipeRow.MaxOptionLimit == 1
                    ? "UI_RANDOM_OPTION_COUNT_FORMAT_SINGULAR"
                    : "UI_RANDOM_OPTION_COUNT_FORMAT_PLURAL");

            descriptionText.text = string.Format(format, ColorTagForDescriptionText1,
                subRecipeRow.MaxOptionLimit == 1 ? 1 : subRecipeRow.MaxOptionLimit);

            var optionSheet = Game.Game.instance.TableSheets.EquipmentItemOptionSheet;
            var skillSheet = Game.Game.instance.TableSheets.SkillSheet;
            var hasThreeOptions = subRecipeRow.Options.Count == 3;

            for (var i = 0; i < optionTexts.Length; ++i)
            {
                if (i >= subRecipeRow.Options.Count)
                {
                    optionTexts[i].percentageText.enabled = false;
                    optionTexts[i].descriptionText.enabled = false;
                    continue;
                }

                optionTexts[i].percentageText.enabled = true;
                optionTexts[i].descriptionText.enabled = true;

                var optionInfo = subRecipeRow.Options[i];
                if (!optionSheet.TryGetValue(optionInfo.Id, out var optionRow))
                {
                    continue;
                }

                var optionColor = optionColors[hasThreeOptions ? i + 1 : i];

                if (optionRow.StatType != StatType.NONE)
                {
                    var statMin = optionRow.StatType == StatType.SPD
                        ? (optionRow.StatMin / 100f).ToString(CultureInfo.InvariantCulture)
                        : optionRow.StatMin.ToString();

                    var statMax = optionRow.StatType == StatType.SPD
                        ? (optionRow.StatMax / 100f).ToString(CultureInfo.InvariantCulture)
                        : optionRow.StatMax.ToString();

                    var description = $"{optionRow.StatType} +({statMin}~{statMax})";
                    SetOptionText(optionTexts[i], optionInfo.Ratio, description, optionColor);
                }
                else
                {
                    skillSheet.TryGetValue(optionRow.SkillId, out var skillRow);
                    SetOptionText(optionTexts[i], optionInfo.Ratio, skillRow.GetLocalizedName(), optionColor);
                }
            }

            SetDimmed(false);
            Show();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public virtual void SetDimmed(bool value)
        {
            nameText.color = value ? DimmedColor : Color.white;
            descriptionText.color = value ? DimmedColor : Color.white;

            string fromColorTag = value ? ColorTagForDescriptionText1 : ColorTagForDescriptionText2;
            string toColorTag =  value ? ColorTagForDescriptionText2 : ColorTagForDescriptionText1;
            
            if (descriptionText.text.Contains($"<color={fromColorTag}>"))
            {
                descriptionText.text = descriptionText.text
                    .Replace($"<color={fromColorTag}>", $"<color={toColorTag}>");
            }

            foreach (var option in optionTexts)
            {
                var descriptionColor = value ?
                    option.descriptionText.color * DimmedColor :
                    option.descriptionText.color;

                option.percentageText.color = value ? DimmedColor : Color.white;
                option.descriptionText.color = descriptionColor;
            }

            SetPanelDimmed(value);
        }

        protected void SetPanelDimmed(bool isDimmed)
        {
            decoration.color = isDimmed ? DimmedColor : Color.white;
            panel.color = isDimmed ? DimmedColor : Color.white;
            innerPanel.color = isDimmed ? DimmedColor : Color.white;
        }

        protected static void SetOptionText(OptionText optionText, decimal percentage, string description, Color color)
        {
            optionText.percentageText.text = percentage.ToString("0%");
            optionText.descriptionText.text = description;
            optionText.descriptionText.color = color;
        }
    }
}
