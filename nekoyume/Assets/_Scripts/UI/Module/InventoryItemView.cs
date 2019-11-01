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
    public class InventoryItemView : CountableItemView<Model.InventoryItem>
    {
        public Image coverImage;
        public Image glowImage;
        public Image equippedIcon;

        private readonly List<IDisposable> _disposablesAtSetData = new List<IDisposable>();

        public InventoryCellView InventoryCellView { get; private set; }

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            InventoryCellView = transform.parent.GetComponent<InventoryCellView>();
            if(InventoryCellView is null)
                Debug.LogError("InventoryCellView not attached to the parent GameObject!");
        }

        protected override void OnDestroy()
        {
            Clear();
        }

        #endregion

        #region override

        public override void SetData(Model.InventoryItem model)
        {
            if (model is null)
            {
                Clear();
                return;
            }
            
            base.SetData(model);
            _disposablesAtSetData.DisposeAllAndClear();
            Model.Covered.SubscribeTo(coverImage).AddTo(_disposablesAtSetData);
            Model.Glowed.SubscribeTo(glowImage).AddTo(_disposablesAtSetData);
            Model.Equipped.SubscribeTo(equippedIcon).AddTo(_disposablesAtSetData);
            Model.View = this;
            UpdateView();
        }

        public override void Clear()
        {
            _disposablesAtSetData.DisposeAllAndClear();
            base.Clear();

            UpdateView();
        }

        protected override void SetDim(bool isDim)
        {
            base.SetDim(isDim);
            
            var alpha = isDim ? .3f : 1f;
            coverImage.color = GetColor(coverImage.color, alpha);
            glowImage.color = GetColor(glowImage.color, alpha);
            equippedIcon.color = GetColor(equippedIcon.color, alpha);
        }

        #endregion

        private void UpdateView()
        {
            if (Model is null)
            {
                selectionImage.enabled = false;
                coverImage.enabled = false;
                glowImage.enabled = false;
                equippedIcon.enabled = false;
                
                return;
            }

            selectionImage.enabled = Model.Selected.Value;
            coverImage.enabled = Model.Covered.Value;
            glowImage.enabled = Model.Glowed.Value;
            equippedIcon.enabled = Model.Equipped.Value;
        }
    }
}
