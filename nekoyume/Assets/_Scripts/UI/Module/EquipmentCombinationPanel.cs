using Assets.SimpleLocalization;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Scroller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EquipmentCombinationPanel : MonoBehaviour, ICombinationPanel
    {
        public int CostNCG { get; private set; }
        public int CostAP { get; private set; }

        public EquipmentRecipeCellView recipeCellView;
        public CombinationMaterialPanel materialPanel;

        public Button cancelButton;
        public TextMeshProUGUI cancelButtonText;
        public SubmitWithCostButton submitButton;

        protected virtual void Awake()
        {
            cancelButton.OnClickAsObservable().Subscribe(_ => SubscribeOnClickCancel()).AddTo(gameObject);
            submitButton.OnSubmitClick.Subscribe(_ => SubscribeOnClickSubmit()).AddTo(gameObject);
            cancelButtonText.text = LocalizationManager.Localize("UI_CANCEL");
        }

        public void SetData(EquipmentItemRecipeSheet.Row recipeRow, int? subRecipeId = null)
        {
            recipeCellView.Set(recipeRow);
            materialPanel.SetData(recipeRow, subRecipeId);

            gameObject.SetActive(true);
            CostNCG = (int) materialPanel.costNcg;
            CostAP = materialPanel.costAp;
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
                submitButton.ShowNCG(CostNCG, States.Instance.AgentState.gold >= CostNCG);
            }
            else
            {
                submitButton.HideNCG();
            }
            submitButton.SetSubmittable(materialPanel.IsCraftable);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private static void SubscribeOnClickCancel()
        {
            Widget.Find<Combination>().State.SetValueAndForceNotify(Combination.StateType.CombineEquipment);
        }

        private static void SubscribeOnClickSubmit()
        {
            Widget.Find<Combination>().State.SetValueAndForceNotify(Combination.StateType.CombineEquipment);
        }
    }
}
