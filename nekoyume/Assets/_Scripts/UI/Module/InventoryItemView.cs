using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(Button))]
    public class InventoryItemView : CountableItemView<Model.InventoryItem>
    {
        public Image coverImage;
        public Image selectionImage;
        public Image glowImage;
        public TextMeshProUGUI equipmentText;
        
        private Button _button;

        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        #region Mono

        protected override void Awake()
        {
            base.Awake();
            
            this.ComponentFieldsNotNullTest();

            _button = GetComponent<Button>();
            _button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlaySelect();
                    data.onClick.OnNext(data);
                }).AddTo(_disposablesForAwake);
        }

        protected override void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
            Clear();
        }

        #endregion

        #region override

        public override void SetData(Model.InventoryItem value)
        {
            if (ReferenceEquals(value, null))
            {
                Clear();
                return;
            }
            
            base.SetData(value);
            _disposablesForSetData.DisposeAllAndClear();
            data.covered.Subscribe(SetCover).AddTo(_disposablesForSetData);
            data.dimmed.Subscribe(SetDim).AddTo(_disposablesForSetData);
            data.selected.Subscribe(SetSelect).AddTo(_disposablesForSetData);
            data.glowed.Subscribe(SetGlow).AddTo(_disposablesForSetData);
            data.count.Subscribe(SetCount).AddTo(_disposablesForSetData);
            
            UpdateView();
        }

        public override void Clear()
        {
            _disposablesForSetData.DisposeAllAndClear();
            base.Clear();
            
            UpdateView();
        }
        
        protected override void SetDim(bool isDim)
        {
            base.SetDim(isDim);
            
            selectionImage.color = isDim ? DimColor : DefaultColor;
        }

        #endregion

        private void UpdateView()
        {
            if (ReferenceEquals(data, null))
            {
                selectionImage.enabled = false;
                
                return;
            }
            
            coverImage.enabled = data.covered.Value;
            selectionImage.enabled = data.selected.Value;
            SetDim(data.dimmed.Value);
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
    }
}
