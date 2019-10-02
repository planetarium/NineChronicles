using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Scroller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(Button))]
    public class InventoryItemView : CountableItemView<Model.InventoryItem>, IPointerClickHandler
    {
        public Image equippedIcon;
        public Image coverImage;
        public Image selectionImage;
        public Image glowImage;
        public TextMeshProUGUI equipmentText;

        private Button _button;

        private readonly List<IDisposable> _disposablesForClear = new List<IDisposable>();

        public InventoryCellView inventoryCellView { get; private set; }

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            this.ComponentFieldsNotNullTest();

            _button = GetComponent<Button>();
            inventoryCellView = transform.parent.GetComponent<InventoryCellView>();
            if(ReferenceEquals(inventoryCellView, null))
            {
                Debug.LogError("InventoryCellView not attached to the parent GameObject!");
            }

            var buttonClickStream = _button.OnClickAsObservable();
            buttonClickStream
                .Subscribe(_=>
                {
                    AudioController.PlaySelect();
                    Model?.OnClick.OnNext(this);
                })
                .AddTo(gameObject);
        }

        protected override void OnDestroy()
        {
            Clear();
        }

        #endregion

        #region override

        public override void SetData(Model.InventoryItem model)
        {
            if (ReferenceEquals(model, null))
            {
                Clear();
                return;
            }
            
            base.SetData(model);
            _disposablesForClear.DisposeAllAndClear();
            Model.Covered.Subscribe(SetCover).AddTo(_disposablesForClear);
            Model.Dimmed.Subscribe(SetDim).AddTo(_disposablesForClear);
            Model.Selected.Subscribe(SetSelect).AddTo(_disposablesForClear);
            Model.Glowed.Subscribe(SetGlow).AddTo(_disposablesForClear);
            Model.Count.Subscribe(SetCount).AddTo(_disposablesForClear);
            Model.Equipped.Subscribe(SetEquipped).AddTo(_disposablesForClear);

            UpdateView();
        }

        public override void Clear()
        {
            _disposablesForClear.DisposeAllAndClear();
            base.Clear();

            UpdateView();
        }

        protected override void SetDim(bool isDim)
        {
            base.SetDim(isDim);

            selectionImage.color = isDim ? DimColor : DefaultColor;
        }

        #endregion

        public void ClearHighlights()
        {
            coverImage.enabled = false;
            selectionImage.enabled = false;
            SetDim(false);
            SetEquipped(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                Model?.OnRightClick.OnNext(this);
            }
        }

        private void UpdateView()
        {
            if (ReferenceEquals(Model, null))
            {
                ClearHighlights();
                return;
            }

            coverImage.enabled = Model.Covered.Value;
            selectionImage.enabled = Model.Selected.Value;
            SetDim(Model.Dimmed.Value);
            SetEquipped(Model.Equipped.Value);
        }

        private void SetCover(bool isCover)
        {
            coverImage.enabled = isCover;
        }

        private void SetSelect(bool isSelect)
        {
            selectionImage.enabled = isSelect;
        }

        private void SetGlow(bool isGlow)
        {
            glowImage.enabled = isGlow;
        }

        protected void SetEquipped(bool isEquipped)
        {
            equippedIcon.enabled = isEquipped;
        }
    }
}
