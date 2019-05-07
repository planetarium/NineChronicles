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

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private T _data;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            this.ComponentFieldsNotNullTest();
            
            closeButton.OnClickAsObservable()
                .Subscribe(OnClickCloseButton)
                .AddTo(_disposables);

            editButton.OnClickAsObservable()
                .Subscribe(OnClickEditButton)
                .AddTo(_disposables);
        }

        protected override void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
            
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

            base.SetData(value);
            data.item.Subscribe(_ => UpdateView());
            data.count.Subscribe(SetCount);
            data.editButtonText.Subscribe(text => { editText.text = text; });
            
            UpdateView();
        }

        public override void Clear()
        {
            base.Clear();

            _data?.Dispose();
            _data = null;
            
            UpdateView();
        }

        private void UpdateView()
        {
            if (ReferenceEquals(_data, null))
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
            _data?.onClose.OnNext(_data);
            AudioController.PlayClick();
        }

        private void OnClickEditButton(Unit u)
        {
            _data?.onEdit.OnNext(_data);
            AudioController.PlayClick();
        }
    }
}
