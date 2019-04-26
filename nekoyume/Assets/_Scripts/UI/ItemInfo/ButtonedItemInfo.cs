using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI.ItemInfo
{
    public class ButtonedItemInfo : ItemInfo
    {
        public Button button;
        public Text buttonText;
        
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
            _disposables.DisposeAllAndClear();
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
            
            _disposables.DisposeAllAndClear();

            _data.ButtonEnabled.Subscribe(SetButtonActive).AddTo(_disposables);
            _data.ButtonText.Subscribe(SetButtonText).AddTo(_disposables);

            button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data.OnClick.OnNext(_data);
                    AudioController.instance.PlaySfx(AudioController.SfxCode.InputItem);
                })
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
            
            _disposables.DisposeAllAndClear();

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
