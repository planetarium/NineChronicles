using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI.ItemView
{
    public class CountEditableItemView<T> : CountableItemView<Game.Item.Inventory.InventoryItem>
        where T : Model.Inventory.Item
    {
        public Button closeButton;
        public Image closeImage;
        public Button editButton;
        public Image editImage;
        public Text editText;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private Model.CountEditableItem<T> _data;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            if (ReferenceEquals(closeButton, null) ||
                ReferenceEquals(closeImage, null) ||
                ReferenceEquals(editButton, null) ||
                ReferenceEquals(editImage, null) ||
                ReferenceEquals(editText, null))
            {
                throw new SerializeFieldNullException();
            }
        }

        #endregion

        public void SetData(Model.CountEditableItem<T> data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }

            _disposables.DisposeAllAndClear();

            _data = data;
            _data.Item.Subscribe(SetItem).AddTo(_disposables);
            _data.Count.Subscribe(count => { Count = count; }).AddTo(_disposables);
            _data.EditButtonText.Subscribe(text => { editText.text = text; }).AddTo(_disposables);

            closeButton.OnClickAsObservable()
                .Subscribe(OnClickCloseButton)
                .AddTo(_disposables);

            editButton.OnClickAsObservable()
                .Subscribe(OnClickEditButton)
                .AddTo(_disposables);

            SetItem(data.Item.Value);
        }

        public override void Clear()
        {
            base.Clear();

            _disposables.DisposeAllAndClear();

            _data = null;

            closeImage.enabled = false;
            editImage.enabled = false;
            editText.enabled = false;
        }

        private void SetItem(T item)
        {
            base.SetData(item, _data.Count.Value);

            closeImage.enabled = true;
            editImage.enabled = true;
            editText.enabled = true;
        }

        private void OnClickCloseButton(Unit u)
        {
            _data?.OnClose.OnNext(_data);
            AudioController.PlayClick();
        }

        private void OnClickEditButton(Unit u)
        {
            _data?.OnEdit.OnNext(_data);
            AudioController.PlayClick();
        }
    }
}
