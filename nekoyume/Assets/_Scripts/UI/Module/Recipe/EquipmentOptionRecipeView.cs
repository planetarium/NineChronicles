using Nekoyume.TableData;
using System;
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

            Show();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void SetOptionText(OptionText optionText, int percentage, string description)
        {
            optionText.percentageText.text = percentage.ToString();
            optionText.descriptionText.text = description;
        }
    }
}
