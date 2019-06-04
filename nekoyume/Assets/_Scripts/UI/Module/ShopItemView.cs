using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ShopItemView : CountableItemView<ShopItem>
    {
        public Image selectedImage;
        public Button button;
        
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            button.OnClickAsObservable().Subscribe(_ =>
            {
                Data?.onClick.OnNext(Data);
                AudioController.instance.PlaySfx(AudioController.SfxCode.Click);
            }).AddTo(_disposablesForAwake);
        }

        protected override void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
            base.OnDestroy();
        }

        #endregion

        public override void SetData(ShopItem value)
        {
            if (ReferenceEquals(value, null))
            {
                Clear();
                return;
            }
            
            base.SetData(value);
            _disposablesForSetData.DisposeAllAndClear();
            Data.selected.Subscribe(_ => selectedImage.enabled = _).AddTo(_disposablesForSetData);

            UpdateView();
        }

        public override void Clear()
        {
            _disposablesForSetData.DisposeAllAndClear();
            base.Clear();

            UpdateView();
        }
        
        private void UpdateView()
        {
            if (ReferenceEquals(Data, null))
            {
                selectedImage.enabled = false;
                button.enabled = false;
                return;
            }

            selectedImage.enabled = Data.selected.Value;
            button.enabled = true;
        }
    }
}
