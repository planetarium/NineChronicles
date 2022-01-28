using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class ItemCountPopup<T> : PopupWidget where T : Model.ItemCountPopup<T>
    {
        [SerializeField]
        private TextMeshProUGUI titleText = null;
        [SerializeField]
        private TextMeshProUGUI informationText = null;
        [SerializeField]
        private TextButton cancelButton = null;
        [SerializeField]
        private SimpleCountableItemView itemView = null;
        [SerializeField]
        protected ConditionalButton submitButton = null;

        protected T _data;
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            submitButton.Text = L10nManager.Localize("UI_OK");
            if (informationText != null)
            {
                informationText.text = L10nManager.Localize("UI_RETRIEVE_INFO");
            }

            if (cancelButton != null)
            {
                cancelButton.OnClick = () =>
                    _data?.OnClickCancel.OnNext(_data);
                cancelButton.Text = L10nManager.Localize("UI_CANCEL");
                CloseWidget = cancelButton.OnClick;
            }

            submitButton.OnSubmitSubject
                .Subscribe(_ =>
                {
                    _data?.OnClickSubmit.OnNext(_data);
                })
                .AddTo(_disposablesForAwake);

            SubmitWidget = () => submitButton.OnSubmitSubject.OnNext(default);

            L10nManager.OnLanguageChange.Subscribe(_ =>
            {
                submitButton.Text = L10nManager.Localize("UI_OK");
                if (informationText != null)
                {
                    informationText.text = L10nManager.Localize("UI_RETRIEVE_INFO");
                }
                if (cancelButton != null)
                {
                    cancelButton.Text = L10nManager.Localize("UI_CANCEL");
                }
            }).AddTo(_disposablesForAwake);
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
            _data.SubmitText.Subscribe(text => submitButton.Text = text)
                .AddTo(_disposablesForSetData);
            _data.Submittable.Subscribe(value => submitButton.SetState(value ?
                ConditionalButton.State.Normal : ConditionalButton.State.Conditional))
                .AddTo(_disposablesForSetData);
            _data.InfoText.Subscribe(value =>
                {
                    if (informationText != null)
                    {
                        informationText.text = value;
                    }
                })
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
