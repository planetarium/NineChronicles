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
                Model?.OnClick.OnNext(this);
                AudioController.instance.PlaySfx(AudioController.SfxCode.Click);
            }).AddTo(_disposablesForAwake);
        }

        protected override void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
            base.OnDestroy();
        }

        #endregion

        public override void SetData(ShopItem model)
        {
            if (ReferenceEquals(model, null))
            {
                Clear();
                return;
            }
            
            base.SetData(model);
            _disposablesForSetData.DisposeAllAndClear();
            Model.Selected.Subscribe(_ => selectedImage.enabled = _).AddTo(_disposablesForSetData);

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
            if (ReferenceEquals(Model, null))
            {
                selectedImage.enabled = false;
                button.enabled = false;
                return;
            }

            selectedImage.enabled = Model.Selected.Value;
            button.enabled = true;
        }
    }
}
