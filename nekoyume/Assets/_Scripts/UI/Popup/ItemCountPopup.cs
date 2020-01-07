using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ItemCountPopup<T> : PopupWidget where T : Model.ItemCountPopup<T>
    {
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI informationText;
        public Button cancelButton;
        public TextMeshProUGUI cancelButtonText;
        public Button submitButton;
        public TextMeshProUGUI submitButtonText;
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
            informationText.text = LocalizationManager.Localize("UI_RETRIEVE_INFO");

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
            _data.SubmitText.Subscribe(value => submitButtonText.text = value).AddTo(_disposablesForSetData);
            _data.InfoText.Subscribe(value => informationText.text = value).AddTo(_disposablesForSetData);
            itemView.SetData(_data.Item.Value);
        }
        
        private void Clear()
        {
            itemView.Clear();
            _data = null;
            _disposablesForSetData.DisposeAllAndClear();
        }
    }
}
