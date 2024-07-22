using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class RecipeOptionView : MonoBehaviour
    {
        [Serializable]
        private struct OptionView
        {
            public GameObject parentObject;
            public TextMeshProUGUI optionText;
            public Slider percentageSlider;
            public Image sliderFillImage;
        }

        [Serializable]
        private struct SkillView
        {
            public GameObject parentObject;
            public TextMeshProUGUI optionText;
            public Slider percentageSlider;
            public Image sliderFillImage;
            public Button tooltipButton;
        }

        [SerializeField] private List<OptionView> optionViews;
        [SerializeField] private List<SkillView> skillViews;
        [SerializeField] private List<GameObject> optionIcons;
        [SerializeField] private TextMeshProUGUI greatSuccessRateText;
        [SerializeField] private SkillPositionTooltip skillTooltip;

        private static readonly Color BaseColor = ColorHelper.HexToColorRGB("3E2524");
        private static readonly Color PremiumColor = ColorHelper.HexToColorRGB("602F44");

        public void SetOptions(List<EquipmentItemSubRecipeSheetV2.OptionInfo> optionInfos, bool isPremium)
        {
            foreach (var optionView in optionViews)
            {
                optionView.parentObject.SetActive(false);
            }

            foreach (var skillView in skillViews)
            {
                skillView.parentObject.SetActive(false);
            }

            optionIcons?.ForEach(obj => obj.SetActive(false));

            var tableSheets = TableSheets.Instance;
            var optionSheet = tableSheets.EquipmentItemOptionSheet;
            var skillSheet = tableSheets.SkillSheet;
            var options = optionInfos
                .Select(x => (ratio: x.Ratio, option: optionSheet[x.Id]))
                .ToList();

            var siblingIndex = 1; // 0 is for the main option
            foreach (var (ratio, option) in options)
            {
                if (option.StatType != StatType.NONE)
                {
                    var optionView = optionViews.First(x => !x.parentObject.activeSelf);
                    var normalizedRatio = ratio.NormalizeFromTenThousandths();
                    optionView.optionText.text = option.OptionRowToString(normalizedRatio, siblingIndex != 1);
                    optionView.percentageSlider.value = (float)normalizedRatio;
                    optionView.sliderFillImage.color = isPremium ? PremiumColor : BaseColor;
                    optionView.parentObject.transform.SetSiblingIndex(siblingIndex);
                    optionView.parentObject.SetActive(true);

                    if (optionIcons != null && optionIcons.Count > siblingIndex - 1)
                    {
                        optionIcons[siblingIndex - 1].SetActive(true);
                    }
                }
                else
                {
                    var skillView = skillViews.First(x => !x.parentObject.activeSelf);
                    var skillName = skillSheet.TryGetValue(option.SkillId, out var skillRow)
                        ? skillRow.GetLocalizedName()
                        : string.Empty;
                    var normalizedRatio = ratio.NormalizeFromTenThousandths();
                    skillName += $" ({normalizedRatio:0%})";
                    skillView.optionText.text = skillName;
                    skillView.percentageSlider.value = (float)normalizedRatio;
                    skillView.sliderFillImage.color = isPremium ? PremiumColor : BaseColor;
                    skillView.parentObject.transform.SetSiblingIndex(siblingIndex);
                    skillView.parentObject.SetActive(true);
                    skillView.tooltipButton.onClick.RemoveAllListeners();
                    skillView.tooltipButton.onClick.AddListener(() =>
                    {
                        var rect = skillView.tooltipButton.GetComponent<RectTransform>();
                        skillTooltip.transform.position = rect.GetWorldPositionOfPivot(PivotPresetType.MiddleLeft);
                        skillTooltip.Show(skillRow, option);
                    });
                    if (optionIcons != null && optionIcons.Count > 0)
                    {
                        optionIcons.Last().SetActive(true);
                    }
                }

                ++siblingIndex;
            }

            if (greatSuccessRateText != null)
            {
                var greatSuccessRate = optionInfos
                    .Select(x => x.Ratio.NormalizeFromTenThousandths())
                    .Aggregate((a, b) => a * b);

                greatSuccessRateText.text = greatSuccessRate == 0m
                    ? "-"
                    : L10nManager.Localize("UI_COMBINATION_GREAT_SUCCESS_RATE_FORMAT",
                        greatSuccessRate.ToString("0.0%"));
            }
        }

        public void SetOptions(RuneOptionSheet.Row.RuneOptionInfo option)
        {
            foreach (var optionView in optionViews)
            {
                optionView.parentObject.SetActive(false);
            }

            foreach (var view in skillViews)
            {
                view.parentObject.SetActive(false);
            }

            optionIcons?.ForEach(obj => obj.SetActive(false));

            var skillSheet = TableSheets.Instance.SkillSheet;

            var siblingIndex = 1; // 0 is for the main option

            var skillView = skillViews.First(x => !x.parentObject.activeSelf);
            var skillName = skillSheet.TryGetValue(option.SkillId, out var skillRow)
                ? skillRow.GetLocalizedName()
                : string.Empty;
            skillView.optionText.text = skillName;
            skillView.percentageSlider.value = skillView.percentageSlider.maxValue;
            skillView.sliderFillImage.color = BaseColor;
            skillView.parentObject.transform.SetSiblingIndex(siblingIndex);
            skillView.parentObject.SetActive(true);
            skillView.tooltipButton.onClick.RemoveAllListeners();
            skillView.tooltipButton.onClick.AddListener(() =>
            {
                var rect = skillView.tooltipButton.GetComponent<RectTransform>();
                skillTooltip.transform.position = rect.GetWorldPositionOfPivot(PivotPresetType.MiddleLeft);
                skillTooltip.Show(skillRow, option);
            });
            if (optionIcons != null && optionIcons.Count > 0)
            {
                optionIcons.Last().SetActive(true);
            }
            ++siblingIndex;

            foreach (var (stat, _) in option.Stats)
            {
                var optionView = optionViews.First(x => !x.parentObject.activeSelf);
                optionView.optionText.text = $"{stat.StatType} {stat.StatType.ValueToString(stat.TotalValue)}";
                optionView.percentageSlider.value = optionView.percentageSlider.maxValue;
                optionView.sliderFillImage.color = BaseColor;
                optionView.parentObject.transform.SetSiblingIndex(siblingIndex);
                optionView.parentObject.SetActive(true);

                if (optionIcons != null && optionIcons.Count > siblingIndex - 1)
                {
                    optionIcons[siblingIndex - 1].SetActive(true);
                }

                ++siblingIndex;
            }
        }
    }
}
