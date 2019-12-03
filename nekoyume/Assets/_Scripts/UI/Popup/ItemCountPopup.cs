using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ItemCountPopup<T> : PopupWidget where T : Model.ItemCountPopup<T>
    {
        public Text titleText;
        public Text countText;
        public Button minusButton;
        public Button plusButton;
        public Button cancelButton;
        public Text cancelButtonText;
        public Button submitButton;
        public Text submitButtonText;
        public SimpleCountableItemView itemView;
        
        private T _data;
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        private string _countStringFormat;
        
        #region Mono

        protected override void Awake()
        {
            base.Awake();

            _countStringFormat = LocalizationManager.Localize("UI_TOTAL_COUNT_N");
            cancelButtonText.text = LocalizationManager.Localize("UI_CANCEL");
            submitButtonText.text = LocalizationManager.Localize("UI_OK");
            
            minusButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data.OnClickMinus.OnNext(_data);
                    AudioController.PlayClick();
                })
                .AddTo(_disposablesForAwake);

            plusButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data.OnClickPlus.OnNext(_data);
                    AudioController.PlayClick();
                })
                .AddTo(_disposablesForAwake);

            cancelButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data.OnClickCancel.OnNext(_data);
                    AudioController.PlayCancel();
                })
                .AddTo(_disposablesForAwake);

            submitButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data.OnClickSubmit.OnNext(_data);
                    AudioController.PlayClick();
                })
                .AddTo(_disposablesForAwake);
        }

        protected override void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
            Clear();
            base.OnDestroy();
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
            _data.TitleText.Subscribe(value => titleText.text = value).AddTo(_disposablesForSetData);
            _data.Item.Value.Count.Subscribe(SetCount).AddTo(_disposablesForSetData);
            _data.CountEnabled.Subscribe(countEnabled =>
            {
                minusButton.gameObject.SetActive(countEnabled);
                plusButton.gameObject.SetActive(countEnabled);
            }).AddTo(_disposablesForSetData);
            _data.SubmitText.Subscribe(value => submitButtonText.text = value).AddTo(_disposablesForSetData);
            itemView.SetData(_data.Item.Value);
            
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
            
            SetCount(_data.Item.Value.Count.Value);
        }
        
        private void SetCount(int count)
        {
            countText.text = string.Format(_countStringFormat, count);
        }
    }
}
