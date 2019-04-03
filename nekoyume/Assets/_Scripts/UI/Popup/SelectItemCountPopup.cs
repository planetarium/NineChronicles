using System;
using System.Collections.Generic;
using Nekoyume.UI.ItemView;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class SelectItemCountPopup : Widget
    {
        private const string CountStringFormat = "총 {0}개";

        public Text titleText;
        public Text countText;
        public Button minusButton;
        public Button plusButton;
        public Button cancelButton;
        public Button okButton;
        public SimpleCountableItemView itemView;
        
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private Model.SelectItemCountPopup<Model.Inventory.Item> _data;

        #region Mono

        private void Awake()
        {
            if (ReferenceEquals(titleText, null) ||
                ReferenceEquals(countText, null) ||
                ReferenceEquals(itemView, null) ||
                ReferenceEquals(minusButton, null) ||
                ReferenceEquals(plusButton, null) ||
                ReferenceEquals(cancelButton, null) ||
                ReferenceEquals(okButton, null))
            {
                throw new SerializeFieldNullException();
            }
        }

        private void OnDestroy()
        {
            _disposables.ForEach(d => d.Dispose());
        }

        #endregion

        public void Pop(Model.SelectItemCountPopup<Model.Inventory.Item> data)
        {
            if (ReferenceEquals(data, null))
            {
                return;
            }

            SetData(data);
            base.Show();
        }

        public void SetData(Model.SelectItemCountPopup<Model.Inventory.Item> data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }
            
            _disposables.ForEach(d => d.Dispose());

            _data = data;
            _data.Count.Subscribe(SetCount)
                .AddTo(_disposables);

            minusButton.OnClickAsObservable()
                .Subscribe(_ => { _data.OnClickMinus.OnNext(_data); })
                .AddTo(_disposables);

            plusButton.OnClickAsObservable()
                .Subscribe(_ => { _data.OnClickPlus.OnNext(_data); })
                .AddTo(_disposables);

            cancelButton.OnClickAsObservable()
                .Subscribe(_ => { _data.OnClickClose.OnNext(_data); })
                .AddTo(_disposables);

            okButton.OnClickAsObservable()
                .Subscribe(_ => { _data.OnClickSubmit.OnNext(_data); })
                .AddTo(_disposables);
            
            SetCount(_data.Count.Value);
            itemView.SetData(data.Item.Value);
        }

        public void SetCount(int count)
        {
            countText.text = string.Format(CountStringFormat, count);
        }

        public void Clear()
        {
            _disposables.ForEach(d => d.Dispose());
            
            _data = null;
            
            SetCount(0);
            itemView.Clear();
        }
    }
}
