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
    public class EquipmentOptionRecipeView : EquipmentOptionView
    {
        [SerializeField]
        private TextMeshProUGUI unlockConditionText = null;

        [SerializeField]
        private RequiredItemRecipeView requiredItemRecipeView = null;

        [SerializeField]
        private Button button = null;

        [SerializeField]
        private GameObject lockParent = null;

        [SerializeField]
        private GameObject header = null;

        [SerializeField]
        private GameObject options = null;

        private void OnDisable()
        {
            button.onClick.RemoveAllListeners();
        }

        public void Show(
            string recipeName,
            EquipmentItemSubRecipeSheet.MaterialInfo baseMaterialInfo,
            int subRecipeId,
            bool isAvailable,
            UnityAction onClick)
        {
            if (Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheet
                .TryGetValue(subRecipeId, out var subRecipeRow))
            {
                requiredItemRecipeView.SetData(baseMaterialInfo, subRecipeRow.Materials);

                if (isAvailable && !(onClick is null))
                    button.onClick.AddListener(onClick);
            }
            else
            {
                Debug.LogWarning($"SubRecipe ID not found : {subRecipeId}");
                Hide();
                return;
            }

            SetLocked(false);
            Show(recipeName, subRecipeId, isAvailable);
        }

        public void ShowLocked()
        {
            SetLocked(true);
            Show();
        }

        private void SetLocked(bool value)
        {
            lockParent.SetActive(value);
            header.SetActive(!value);
            options.SetActive(!value);
            requiredItemRecipeView.gameObject.SetActive(!value);
            SetPanelDimmed(value);
        }
    }
}
