using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.ItemInfo
{
    public class ButtonedItemInfo : ItemInfo
    {
        [SerializeField] public Button button;
        [SerializeField] public Text buttonText;
        
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            if (ReferenceEquals(button, null) ||
                ReferenceEquals(buttonText, null))
            {
                throw new SerializeFieldNullException();
            }
        }

        private void OnDestroy()
        {
            _disposables.ForEach(d => d.Dispose());
        }

        #endregion

        #region override

        public override void SetData(Model.ItemInfo data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }

            base.SetData(data);
            
            _disposables.ForEach(d => d.Dispose());

            _data.ButtonEnabled.Subscribe(SetButtonActive).AddTo(_disposables);
            _data.ButtonText.Subscribe(SetButtonText).AddTo(_disposables);

            button.OnClickAsObservable()
                .Subscribe(_ => { _data.OnClick.OnNext(_data); })
                .AddTo(_disposables);

            UpdateView();
        }

        public override void UpdateView()
        {
            base.UpdateView();
            
            if (ReferenceEquals(_data, null) ||
                ReferenceEquals(_data.Item.Value, null))
            {
                button.gameObject.SetActive(false);
                return;
            }

            SetButtonText(_data.ButtonText.Value);
            SetButtonActive(_data.ButtonEnabled.Value);
        }

        public override void Clear()
        {
            base.Clear();
            
            _disposables.ForEach(d => d.Dispose());

            UpdateView();
        }

        #endregion

        public void SetButtonActive(bool isActive)
        {
            button.gameObject.SetActive(isActive);
        }

        public void SetButtonText(string text)
        {
            buttonText.text = text;
        }
    }
}
