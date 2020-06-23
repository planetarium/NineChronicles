using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Scroller;

namespace Nekoyume.UI.Module
{
    public class ConsumableCombinationPanel : CombinationPanel
    {
        public void SetData(ConsumableItemRecipeSheet.Row recipeRow)
        {
            (recipeCellView as ConsumableRecipeCellView).Set(recipeRow);
            materialPanel.SetData(recipeRow);

            gameObject.SetActive(true);
            confirmAreaYTweener.OnComplete = OnTweenCompleted;
            confirmAreaYTweener.StartTween();
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
        }

        protected override void SubscribeOnClickCancel()
        {
            Widget.Find<Combination>().State.SetValueAndForceNotify(Combination.StateType.CombineConsumable);
        }

        protected override void SubscribeOnClickSubmit()
        {
            Widget.Find<Combination>().State.SetValueAndForceNotify(Combination.StateType.CombineConsumable);
        }
    }
}
