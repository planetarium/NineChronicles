using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CountEditableItemView<T> : CountableItemView<T> where T : Model.CountEditableItem
    {
        public Button closeButton;
        public Image closeImage;
        public Button editButton;
        public Image editImage;
        public Text editText;

        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            this.ComponentFieldsNotNullTest();
            
            closeButton.OnClickAsObservable()
                .Subscribe(OnClickCloseButton)
                .AddTo(_disposablesForAwake);

            editButton.OnClickAsObservable()
                .Subscribe(OnClickEditButton)
                .AddTo(_disposablesForAwake);
        }

        protected override void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
            
            base.OnDestroy();
        }

        #endregion

        public override void SetData(T value)
        {
            if (ReferenceEquals(value, null))
            {
                Clear();
                return;
            }

            _disposablesForSetData.DisposeAllAndClear();
            base.SetData(value);
            data.item.Subscribe(_ => UpdateView()).AddTo(_disposablesForSetData);
            data.count.Subscribe(SetCount).AddTo(_disposablesForSetData);
            data.editButtonText.Subscribe(text => { editText.text = text; }).AddTo(_disposablesForSetData);
            
            UpdateView();
        }

        public override void Clear()
        {
            _disposablesForSetData.DisposeAllAndClear();
            base.Clear();
            
            UpdateView();
        }

        private void UpdateView()
        {
            if (ReferenceEquals(data, null))
            {
                closeImage.enabled = false;
                editImage.enabled = false;
                editText.enabled = false;
                
                return;
            }
            
            closeImage.enabled = true;
            editImage.enabled = true;
            editText.enabled = true;
        }

        private void OnClickCloseButton(Unit u)
        {
            data?.onClose.OnNext(data);
            AudioController.PlayClick();
        }

        private void OnClickEditButton(Unit u)
        {
            data?.onEdit.OnNext(data);
            AudioController.PlayClick();
        }
    }
}
