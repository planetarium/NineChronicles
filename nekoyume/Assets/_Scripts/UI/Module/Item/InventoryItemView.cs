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
        public Image effectImage;
        public Image glowImage;
        public Image equippedIcon;
        
        protected override ImageSizeType imageSizeType => ImageSizeType.Middle;

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
            Model.EffectEnabled.SubscribeTo(effectImage).AddTo(_disposablesAtSetData);
            Model.GlowEnabled.SubscribeTo(glowImage).AddTo(_disposablesAtSetData);
            Model.EquippedEnabled.SubscribeTo(equippedIcon).AddTo(_disposablesAtSetData);
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
            
            effectImage.color = isDim ? DimmedColor : OriginColor;
            glowImage.color = isDim ? DimmedColor : OriginColor;
            equippedIcon.color = isDim ? DimmedColor : OriginColor;
        }

        #endregion

        private void UpdateView()
        {
            if (Model is null)
            {
                selectionImage.enabled = false;
                effectImage.enabled = false;
                glowImage.enabled = false;
                equippedIcon.enabled = false;
                
                return;
            }

            selectionImage.enabled = Model.Selected.Value;
            effectImage.enabled = Model.EffectEnabled.Value;
            glowImage.enabled = Model.GlowEnabled.Value;
            equippedIcon.enabled = Model.EquippedEnabled.Value;
        }
    }
}
