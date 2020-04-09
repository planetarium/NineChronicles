using Assets.SimpleLocalization;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Scroller;
using Nekoyume.UI.Tween;
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
        public Subject<long> RequiredBlockIndexSubject { get; } = new Subject<long>();

        public EquipmentRecipeCellView recipeCellView;
        public CombinationMaterialPanel materialPanel;

        public Button cancelButton;
        public TextMeshProUGUI cancelButtonText;
        public SubmitWithCostButton submitButton;

        [SerializeField]
        protected DOTweenRectTransformMoveTo cellViewTweener = null;

        [SerializeField]
        protected DOTweenGroupAlpha confirmAreaAlphaTweener = null;

        [SerializeField]
        protected AnchoredPositionYTweener confirmAreaYTweener = null;

        protected virtual void Awake()
        {
            cancelButton.OnClickAsObservable().Subscribe(_ => SubscribeOnClickCancel()).AddTo(gameObject);
            submitButton.OnSubmitClick.Subscribe(_ => SubscribeOnClickSubmit()).AddTo(gameObject);
            cancelButtonText.text = LocalizationManager.Localize("UI_CANCEL");
        }

        public void TweenCellView(EquipmentRecipeCellView view)
        {
            var rect = view.transform as RectTransform;

            cellViewTweener.SetBeginRect(rect);
            cellViewTweener.Play();
        }

        public void SetData(EquipmentItemRecipeSheet.Row recipeRow, int? subRecipeId = null)
        {
            recipeCellView.Set(recipeRow);
            materialPanel.SetData(recipeRow, subRecipeId);

            gameObject.SetActive(true);
            confirmAreaYTweener.StartTween();
            confirmAreaAlphaTweener.PlayDelayed(0.2f);

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
            RequiredBlockIndexSubject.OnNext(recipeRow.RequiredBlockIndex);
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
