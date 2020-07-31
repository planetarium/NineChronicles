using System.Numerics;
using Nekoyume.L10n;
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
    public interface ICombinationPanel
    {
        BigInteger CostNCG { get; }
        int CostAP { get; }
    }

    public class CombinationPanel : MonoBehaviour, ICombinationPanel
    {
        public BigInteger CostNCG { get; protected set; }
        public int CostAP { get; protected set; }
        public Subject<long> RequiredBlockIndexSubject { get; } = new Subject<long>();

        public RecipeCellView recipeCellView;
        public CombinationMaterialPanel materialPanel;

        public Button cancelButton;
        public TextMeshProUGUI cancelButtonText;
        public SubmitWithCostButton submitButton;

        [SerializeField]
        protected GameObject cellViewFrontVfx = null;

        [SerializeField]
        protected GameObject cellViewBackVfx = null;

        [SerializeField]
        protected DOTweenRectTransformMoveTo cellViewTweener = null;

        [SerializeField]
        protected DOTweenGroupAlpha confirmAreaAlphaTweener = null;

        [SerializeField]
        protected AnchoredPositionYTweener confirmAreaYTweener = null;

        protected System.Action onTweenCompleted = null;

        public Combination.StateType stateType = Combination.StateType.CombineEquipment;

        protected virtual void Awake()
        {
            cancelButton.OnClickAsObservable().Subscribe(_ => SubscribeOnClickCancel()).AddTo(gameObject);
            cancelButtonText.text = L10nManager.Localize("UI_CANCEL");
            submitButton.HideHourglass();
            var submitText = L10nManager.Localize("UI_COMBINATION_ITEM");
            submitButton.SetSubmitText(submitText, submitText);
        }

        protected virtual void OnDisable()
        {
            cellViewFrontVfx.SetActive(false);
            cellViewBackVfx.SetActive(false);
            confirmAreaYTweener.onComplete = null;
        }

        public void TweenCellView(RecipeCellView view, System.Action onCompleted)
        {
            var rect = view.transform as RectTransform;

            cellViewTweener.SetBeginRect(rect);
            onTweenCompleted = onCompleted;
            cellViewTweener.Play();
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        protected virtual void SubscribeOnClickCancel()
        {
            var combination = Widget.Find<Combination>();
            if (!combination.CanHandleInputEvent)
                return;

            combination.State.SetValueAndForceNotify(stateType);
        }

        public virtual void SubscribeOnClickSubmit()
        {
            var combination = Widget.Find<Combination>();
            if (!combination.CanHandleInputEvent)
                return;

            combination.State.SetValueAndForceNotify(stateType);
        }

        protected void OnTweenCompleted()
        {
            cellViewFrontVfx.SetActive(true);
            cellViewBackVfx.SetActive(true);
            onTweenCompleted?.Invoke();
        }

        protected void Enable()
        {
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
        }

        public void SetData(EquipmentItemRecipeSheet.Row recipeRow, int? subRecipeId = null)
        {
            recipeCellView.Set(recipeRow);
            materialPanel.SetData(recipeRow, subRecipeId);
            Enable();
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
            stateType = Combination.StateType.CombineEquipment;
        }
        public void SetData(ConsumableItemRecipeSheet.Row recipeRow)
        {
            recipeCellView.Set(recipeRow);
            materialPanel.SetData(recipeRow, true, Widget.Find<Combination>().selectedIndex >= 0);
            Enable();
            stateType = Combination.StateType.CombineConsumable;
        }
    }
}
