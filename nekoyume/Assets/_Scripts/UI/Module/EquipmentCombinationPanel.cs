using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Scroller;

namespace Nekoyume.UI.Module
{
    public class EquipmentCombinationPanel : CombinationPanel
    {
        public void SetData(EquipmentItemRecipeSheet.Row recipeRow, int? subRecipeId = null)
        {
            (recipeCellView as EquipmentRecipeCellView).Set(recipeRow);
            materialPanel.SetData(recipeRow, subRecipeId);

            gameObject.SetActive(true);
            confirmAreaYTweener.onComplete = OnTweenCompleted;
            confirmAreaYTweener.PlayTween();
            confirmAreaAlphaTweener.PlayDelayed(0.2f);

            CostNCG = (int) materialPanel.costNCG;
            CostAP = materialPanel.costAP;
            if (CostAP > 0)
            {
                submitButton.ShowAP(CostAP, States.Instance.CurrentAvatarState.actionPoint >= CostAP);
            }
            else
            {
                submitButton.HideAP();
            }

            if (CostNCG > 0)
            {
                submitButton.ShowNCG(CostNCG, States.Instance.GoldBalanceState.gold >= CostNCG);
            }
            else
            {
                submitButton.HideNCG();
            }
            submitButton.SetSubmittable(materialPanel.IsCraftable);
            var requiredBlockIndex = recipeRow.RequiredBlockIndex;
            if (subRecipeId.HasValue)
            {
                var subSheet = Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheet;
                if (subSheet.TryGetValue((int) subRecipeId, out var subRecipe))
                {
                    requiredBlockIndex += subRecipe.RequiredBlockIndex;

                }
            }
            RequiredBlockIndexSubject.OnNext(requiredBlockIndex);
        }
    }
}
