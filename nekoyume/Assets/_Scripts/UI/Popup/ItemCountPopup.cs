using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ItemCountPopup<T> : PopupWidget where T : Model.ItemCountPopup<T>
    {
        [SerializeField]
        private TextMeshProUGUI titleText = null;
        [SerializeField]
        private TextMeshProUGUI informationText = null;
        [SerializeField]
        private Button cancelButton = null;
        [SerializeField]
        private TextMeshProUGUI cancelButtonText = null;
        [SerializeField]
        private SimpleCountableItemView itemView = null;
        [SerializeField]
        protected SubmitButton submitButton = null;

        protected T _data;
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            cancelButtonText.text = LocalizationManager.Localize("UI_CANCEL");
            submitButton.SetSubmitText(LocalizationManager.Localize("UI_OK"));
            informationText.text = LocalizationManager.Localize("UI_RETRIEVE_INFO");

            cancelButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data?.OnClickCancel.OnNext(_data);
                    AudioController.PlayCancel();
                })
                .AddTo(_disposablesForAwake);

            submitButton.OnSubmitClick
                .Subscribe(_ =>
                {
                    _data?.OnClickSubmit.OnNext(_data);
                    AudioController.PlayClick();
                })
                .AddTo(_disposablesForAwake);

            CloseWidget = cancelButton.onClick.Invoke;
            SubmitWidget = () => submitButton.OnSubmitClick.OnNext(submitButton);
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
            Show();
        }

        protected virtual void SetData(T data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }

            _disposablesForSetData.DisposeAllAndClear();
            _data = data;
            _data.TitleText.Subscribe(value => titleText.text = value)
                .AddTo(_disposablesForSetData);
            _data.SubmitText.Subscribe(submitButton.SetSubmitText)
                .AddTo(_disposablesForSetData);
            _data.Submittable.Subscribe(value => submitButton.SetSubmittable(value))
                .AddTo(_disposablesForSetData);
            _data.InfoText.Subscribe(value => informationText.text = value)
                .AddTo(_disposablesForSetData);
            itemView.SetData(_data.Item.Value);
        }

        protected virtual void Clear()
        {
            _disposablesForSetData.DisposeAllAndClear();
            _data = null;
            itemView.Clear();
        }
    }
}
