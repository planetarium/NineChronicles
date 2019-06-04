using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ItemCountPopup<T> : Widget where T : Model.ItemCountPopup<T>
    {
        private const string CountStringFormat = "총 {0}개";

        public Text titleText;
        public Text countText;
        public Button minusButton;
        public Button plusButton;
        public Button cancelButton;
        public Button submitButton;
        public Text submitButtonText;
        public SimpleCountableItemView itemView;
        
        private T _data;
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            this.ComponentFieldsNotNullTest();
            
            minusButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data.onClickMinus.OnNext(_data);
                    AudioController.PlayClick();
                })
                .AddTo(_disposablesForAwake);

            plusButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data.onClickPlus.OnNext(_data);
                    AudioController.PlayClick();
                })
                .AddTo(_disposablesForAwake);

            cancelButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data.onClickClose.OnNext(_data);
                    AudioController.PlayCancel();
                })
                .AddTo(_disposablesForAwake);

            submitButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data.onClickSubmit.OnNext(_data);
                    AudioController.PlayClick();
                })
                .AddTo(_disposablesForAwake);
        }

        protected virtual void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
            Clear();
        }

        #endregion

        public virtual void Pop(T data)
        {
            if (ReferenceEquals(data, null))
            {
                return;
            }

            AudioController.PlayPopup();
            SetData(data);
            base.Show();
        }

        private void SetData(T data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }
            
            _disposablesForSetData.DisposeAllAndClear();
            _data = data;
            _data.titleText.Subscribe(value => titleText.text = value).AddTo(_disposablesForSetData);
            _data.item.Value.count.Subscribe(SetCount).AddTo(_disposablesForSetData);
            _data.countEnabled.Subscribe(countEnabled =>
            {
                minusButton.gameObject.SetActive(countEnabled);
                plusButton.gameObject.SetActive(countEnabled);
            }).AddTo(_disposablesForSetData);
            _data.submitText.Subscribe(value => submitButtonText.text = value).AddTo(_disposablesForSetData);
            itemView.SetData(_data.item.Value);
            
            UpdateView();
        }
        
        private void Clear()
        {
            itemView.Clear();
            _data = null;
            _disposablesForSetData.DisposeAllAndClear();
            
            UpdateView();
        }

        private void UpdateView()
        {
            if (ReferenceEquals(_data, null))
            {
                SetCount(0);
                return;
            }
            
            SetCount(_data.item.Value.count.Value);
        }
        
        private void SetCount(int count)
        {
            countText.text = string.Format(CountStringFormat, count);
        }
    }
}
