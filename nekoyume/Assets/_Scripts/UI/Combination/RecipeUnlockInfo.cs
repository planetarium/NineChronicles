using Nekoyume.Helper;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using System.Linq;
using Nekoyume.L10n;
using Nekoyume.State;
using System.Collections.Generic;

namespace Nekoyume.UI
{
    using UniRx;

    public class RecipeUnlockInfo : MonoBehaviour
    {
        [SerializeField]
        private RecipeCell recipeCell = null;

        [SerializeField]
        private GameObject[] gradeImages = null;

        [SerializeField]
        private TextMeshProUGUI recipeNameText = null;

        [SerializeField]
        private ConditionalCostButton costButton = null;

        [SerializeField]
        private TextMeshProUGUI openConditionText = null;

        private List<int> _unlockList = null;

        private EquipmentItemRecipeSheet.Row _row = null;

        private void Awake()
        {
            costButton.OnClickSubject
                .Subscribe(state =>
                {
                    if (state == ConditionalButton.State.Disabled)
                    {
                        return;
                    }

                    var usageMessage = L10nManager.Localize("UI_UNLOCK_RECIPE");
                    Widget.Find<PaymentPopup>().Show(
                        ReactiveCrystalState.CrystalBalance,
                        costButton.Cost,
                        usageMessage,
                        UnlockRecipeAction);
                })
                .AddTo(gameObject);
        }

        public void Set(EquipmentItemRecipeSheet.Row row)
        {
            _row = row;
            _unlockList = new List<int> { row.Key };
            recipeCell.Show(row, false);

            var resultItem = row.GetResultEquipmentItemRow();
            for (int i = 0; i < gradeImages.Length; ++i)
            {
                gradeImages[i].SetActive(i < resultItem.Grade);
            }

            recipeNameText.text = resultItem.GetLocalizedName(false);

            if (Craft.SharedModel.UnlockingRecipes.Contains(row.Id))
            {
                openConditionText.text = L10nManager.Localize("UI_LOADING_STATES");
                openConditionText.enabled = true;
                costButton.gameObject.SetActive(false);
                return;
            }

            UpdateButton(Craft.SharedModel.UnlockedRecipes.Value);
        }

        private void UpdateButton(List<int> unlockedRecipes)
        {
            var sheet = Game.Game.instance.TableSheets.EquipmentItemRecipeSheet;
            var previousRecipeIds = sheet.Values
                .Where(x =>
                x.GetResultEquipmentItemRow().ItemSubType == _row.GetResultEquipmentItemRow().ItemSubType &&
                x.Id < _row.Id)
                .Select(x => x.Id);
            var unlockable = previousRecipeIds.All(unlockedRecipes.Contains);
            costButton.gameObject.SetActive(unlockable);
            openConditionText.text = L10nManager.Localize("UI_INFORM_OPEN_PREVIOUS_RECIPE");
            openConditionText.enabled = !unlockable;

            if (unlockable)
            {
                var cost = CrystalCalculator.CalculateRecipeUnlockCost(_unlockList, sheet);
                costButton.SetCost(ConditionalCostButton.CostType.Crystal, (int)cost.MajorUnit);
            }
        }
        
        public void UnlockRecipeAction()
        {
            var sharedModel = Craft.SharedModel;

            sharedModel.SelectedRecipeCell.Unlock();
            sharedModel.UnlockingRecipes.AddRange(_unlockList);
            LocalLayerModifier.ModifyAgentCrystal(
                States.Instance.AgentState.address, -costButton.Cost);
            Game.Game.instance.ActionManager
                .UnlockEquipmentRecipe(_unlockList)
                .Subscribe();

            openConditionText.text = L10nManager.Localize("UI_LOADING_STATES");
            openConditionText.enabled = true;
            costButton.gameObject.SetActive(false);
        }
    }
}
