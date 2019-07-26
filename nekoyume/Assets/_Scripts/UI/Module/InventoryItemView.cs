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
        public GameObject equippedIcon;
        public Image coverImage;
        public Image selectionImage;
        public Image glowImage;
        public TextMeshProUGUI equipmentText;

        private Button _button;

        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        private readonly TimeSpan _timeSpan200Milliseconds = TimeSpan.FromMilliseconds(200);
        
        #region Mono

        protected override void Awake()
        {
            base.Awake();

            this.ComponentFieldsNotNullTest();

            _button = GetComponent<Button>();
            var buttonClickStream = _button.OnClickAsObservable();
            buttonClickStream
                .Subscribe(_ =>
                {
                    AudioController.PlaySelect();
                    Model?.onClick.OnNext(this);
                })
                .AddTo(_disposablesForAwake);
            buttonClickStream
                .Buffer(buttonClickStream.Throttle(_timeSpan200Milliseconds))
                .Where(_ => _.Count >= 2)
                .Subscribe(_ =>
                {
                    Model?.onDoubleClick.OnNext(this);
                }).AddTo(_disposablesForAwake);
        }

        protected override void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
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
            _disposablesForSetData.DisposeAllAndClear();
            Model.covered.Subscribe(SetCover).AddTo(_disposablesForSetData);
            Model.dimmed.Subscribe(SetDim).AddTo(_disposablesForSetData);
            Model.selected.Subscribe(SetSelect).AddTo(_disposablesForSetData);
            Model.glowed.Subscribe(SetGlow).AddTo(_disposablesForSetData);
            Model.count.Subscribe(SetCount).AddTo(_disposablesForSetData);
            Model.equipped.Subscribe(SetEquipped).AddTo(_disposablesForSetData);

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

        protected void SetEquipped(bool isEquipped)
        {
            equippedIcon.SetActive(isEquipped);
        }

        #endregion

        private void UpdateView()
        {
            if (ReferenceEquals(Model, null))
            {
                selectionImage.enabled = false;
                SetEquipped(false);

                return;
            }

            coverImage.enabled = Model.covered.Value;
            selectionImage.enabled = Model.selected.Value;
            SetDim(Model.dimmed.Value);
            SetEquipped(Model.equipped.Value);
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
