using System.Numerics;
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

        protected virtual void Awake()
        {
            cancelButton.OnClickAsObservable().Subscribe(_ => SubscribeOnClickCancel()).AddTo(gameObject);
            cancelButtonText.text = LocalizationManager.Localize("UI_CANCEL");
            submitButton.HideHourglass();
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

            combination.State.SetValueAndForceNotify(Combination.StateType.CombineEquipment);
        }

        public virtual void SubscribeOnClickSubmit()
        {
            var combination = Widget.Find<Combination>();
            if (!combination.CanHandleInputEvent)
                return;

            combination.State.SetValueAndForceNotify(Combination.StateType.CombineEquipment);
        }

        protected void OnTweenCompleted()
        {
            cellViewFrontVfx.SetActive(true);
            cellViewBackVfx.SetActive(true);
            onTweenCompleted?.Invoke();
        }
    }
}
