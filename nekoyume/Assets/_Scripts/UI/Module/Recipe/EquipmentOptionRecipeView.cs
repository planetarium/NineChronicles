using System;
using TMPro;
using UnityEngine;

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

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Show(int recipe)
        {
            
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
