using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.UI.ItemView;
using UniRx;
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

        protected override void Awake()
        {
            base.Awake();

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
            _disposables.DisposeAllAndClear();
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
            
            AudioController.PlayPopup();
        }

        public void SetData(Model.SelectItemCountPopup<Model.Inventory.Item> data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }
            
            _disposables.DisposeAllAndClear();

            _data = data;
            _data.Count.Subscribe(SetCount)
                .AddTo(_disposables);

            minusButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data.OnClickMinus.OnNext(_data);
                    AudioController.PlayClick();
                })
                .AddTo(_disposables);

            plusButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data.OnClickPlus.OnNext(_data);
                    AudioController.PlayClick();
                })
                .AddTo(_disposables);

            cancelButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data.OnClickClose.OnNext(_data);
                    AudioController.PlayCancel();
                })
                .AddTo(_disposables);

            okButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data.OnClickSubmit.OnNext(_data);
                    AudioController.PlayClick();
                })
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
            _disposables.DisposeAllAndClear();
            
            _data = null;
            
            SetCount(0);
            itemView.Clear();
        }
    }
}
