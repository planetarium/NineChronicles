using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class TextButton : MonoBehaviour
    {
        public Button button;
        public Image backgroundImage;
        public Text text;
        
        private Model.TextButton _data;
        
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        #region Mono

        protected void Awake()
        {
            button.onClick.AsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                _data?.onClick.OnNext(_data);
            }).AddTo(_disposablesForAwake);
        }

        protected void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
            Clear();
        }

        #endregion

        public void SetData(Model.TextButton data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }
            
            _disposablesForSetData.DisposeAllAndClear();
            _data = data;
            _data.buttonType.Subscribe(UpdateButtonType).AddTo(_disposablesForSetData);
            _data.text.Subscribe(value => text.text = value).AddTo(_disposablesForSetData);
        }

        public void Clear()
        {
            _disposablesForSetData.DisposeAllAndClear();
            _data = null;
        }

        private void UpdateButtonType(Model.TextButton.ButtonType buttonType)
        {
            switch (buttonType)
            {
                case Model.TextButton.ButtonType.Cancel:
                    backgroundImage.sprite = Resources.Load<Sprite>("UI/Textures/button_gray_01");
                    break;
                case Model.TextButton.ButtonType.Default:
                    backgroundImage.sprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");
                    break;
                case Model.TextButton.ButtonType.Black:
                    backgroundImage.sprite = Resources.Load<Sprite>("UI/Textures/button_black_01");
                    break;
                case Model.TextButton.ButtonType.Violet:
                    backgroundImage.sprite = Resources.Load<Sprite>("UI/Textures/button_violet_01");
                    break;
                case Model.TextButton.ButtonType.Yellow:
                    backgroundImage.sprite = Resources.Load<Sprite>("UI/Textures/button_yellow_01");
                    break;
            }
        }
    }
}
