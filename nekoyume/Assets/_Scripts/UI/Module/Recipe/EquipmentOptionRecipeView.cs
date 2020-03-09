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
    public class EquipmentOptionRecipeView : MonoBehaviour
    {
        [Serializable]
        private struct OptionText
        {
            public TextMeshProUGUI percentageText;
            public TextMeshProUGUI descriptionText;
        }

        [SerializeField]
        private TextMeshProUGUI nameText = null;

        [SerializeField]
        private TextMeshProUGUI descriptionText = null;
        
        [SerializeField]
        private TextMeshProUGUI unlockConditionText = null;

        [SerializeField]
        private OptionText[] optionTexts = null;

        [SerializeField]
        private RequiredItemRecipeView requiredItemRecipeView = null;

        [SerializeField]
        private Button button = null;

        private void OnDisable()
        {
            button.onClick.RemoveAllListeners();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Show(
            string recipeName,
            EquipmentItemSubRecipeSheet.MaterialInfo baseMaterialInfo,
            int subRecipeId,
            UnityAction onClick)
        {
            if (Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheet
                .TryGetValue(subRecipeId, out var subRecipeRow))
            {
                requiredItemRecipeView.SetData(baseMaterialInfo, subRecipeRow.Materials);
                button.onClick.AddListener(onClick);
            }
            else
            {
                Debug.LogWarning($"SubRecipe ID not found : {subRecipeId}");
                Hide();
                return;
            }

            nameText.text = recipeName;

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

        private void SetOptionText(OptionText optionText, decimal percentage, string description)
        {
            optionText.percentageText.text = percentage.ToString("0%");
            optionText.descriptionText.text = description;
        }
    }
}
