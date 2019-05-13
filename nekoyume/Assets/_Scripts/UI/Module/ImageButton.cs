using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ImageButton : MonoBehaviour
    {
        public Button button;
        public Image backgroundImage;
        
        private Model.ImageButton _data;
        
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        #region Mono

        protected void Awake()
        {
            this.ComponentFieldsNotNullTest();

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

        public void SetData(Model.ImageButton data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }
            
            _disposablesForSetData.DisposeAllAndClear();
            _data = data;
            _data.buttonType.Subscribe(UpdateButtonType).AddTo(_disposablesForSetData);
        }

        public void Clear()
        {
            _disposablesForSetData.DisposeAllAndClear();
            _data = null;
        }

        private void UpdateButtonType(Model.ImageButton.ButtonType buttonType)
        {
            switch (buttonType)
            {
                case Model.ImageButton.ButtonType.BlackClose:
                    backgroundImage.sprite = Resources.Load<Sprite>("UI/Textures/button_black_close");
                    break;
                case Model.ImageButton.ButtonType.BlueMinus:
                    backgroundImage.sprite = Resources.Load<Sprite>("UI/Textures/button_blue_minus");
                    break;
                case Model.ImageButton.ButtonType.BluePlus:
                    backgroundImage.sprite = Resources.Load<Sprite>("UI/Textures/button_blue_plus");
                    break;
                case Model.ImageButton.ButtonType.GrayMinus:
                    backgroundImage.sprite = Resources.Load<Sprite>("UI/Textures/button_gray_minus");
                    break;
                case Model.ImageButton.ButtonType.GrayPlus:
                    backgroundImage.sprite = Resources.Load<Sprite>("UI/Textures/button_gray_plus");
                    break;
            }
            
            backgroundImage.SetNativeSize();
        }
    }
}
