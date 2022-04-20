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

        private int _currentRecipeKey;

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
                        ReactiveAvatarState.CrystalBalance,
                        500,
                        usageMessage,
                        UnlockRecipeAction);
                })
                .AddTo(gameObject);
        }

        public void Set(EquipmentItemRecipeSheet.Row row)
        {
            _currentRecipeKey = row.Key;
            recipeCell.Show(row, false);

            var resultItem = row.GetResultEquipmentItemRow();
            for (int i = 0; i < gradeImages.Length; ++i)
            {
                gradeImages[i].SetActive(i < resultItem.Grade);
            }

            recipeNameText.text = resultItem.GetLocalizedName(false);

            var unlockedRecipeIds = Craft.SharedModel.UnlockedRecipes.Value;
            var previousRecipeIds =
                Game.Game.instance.TableSheets.EquipmentItemRecipeSheet.Values
                .Where(x => x.Id < row.Id)
                .Select(x => x.Id);
            var unlockable = previousRecipeIds.All(unlockedRecipeIds.Contains);
            costButton.gameObject.SetActive(unlockable);
            openConditionText.enabled = !unlockable;

            if (unlockable)
            {
                costButton.SetCost(ConditionalCostButton.CostType.Crystal, 500);
            }
        }

        public void UnlockRecipeAction()
        {
            Craft.SharedModel.SelectedRecipeCell.Unlock();
            Game.Game.instance.ActionManager.UnlockEquipmentRecipe(new List<int>() { _currentRecipeKey });
        }
    }
}
