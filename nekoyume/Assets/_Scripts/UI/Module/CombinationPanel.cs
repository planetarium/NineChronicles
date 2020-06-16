using Assets.SimpleLocalization;
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
        decimal CostNCG { get; }
        int CostAP { get; }
    }

    public class CombinationPanel : MonoBehaviour, ICombinationPanel
    {
        public decimal CostNCG { get; protected set; }
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

        protected virtual void Awake()
        {
            cancelButton.OnClickAsObservable().Subscribe(_ => SubscribeOnClickCancel()).AddTo(gameObject);
            submitButton.OnSubmitClick.Subscribe(_ => SubscribeOnClickSubmit()).AddTo(gameObject);
            cancelButtonText.text = LocalizationManager.Localize("UI_CANCEL");
            submitButton.HideHourglass();
        }

        protected virtual void OnDisable()
        {
            cellViewFrontVfx.SetActive(false);
            cellViewBackVfx.SetActive(false);
            confirmAreaYTweener.OnComplete = null;
        }

        public void TweenCellView(RecipeCellView view, System.Action onCompleted)
        {
            var rect = view.transform as RectTransform;

            cellViewTweener.SetBeginRect(rect);
            onTweenCompleted = onCompleted;
            cellViewTweener.Play();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        protected virtual void SubscribeOnClickCancel()
        {
            Widget.Find<Combination>().State.SetValueAndForceNotify(Combination.StateType.CombineEquipment);
        }

        protected virtual void SubscribeOnClickSubmit()
        {
            Widget.Find<Combination>().State.SetValueAndForceNotify(Combination.StateType.CombineEquipment);
        }

        protected void OnTweenCompleted()
        {
            cellViewFrontVfx.SetActive(true);
            cellViewBackVfx.SetActive(true);
            onTweenCompleted?.Invoke();
        }
    }
}
