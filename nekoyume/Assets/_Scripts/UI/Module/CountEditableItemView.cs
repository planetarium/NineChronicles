using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CountEditableItemView<T> : CountableItemView<T> where T : Model.CountEditableItem
    {
        public Image backgroundImage;
        public Button itemButton;
        public Button minusButton;
        public Button plusButton;
        public Button deleteButton;
        public Text deleteButtonText;

        private readonly List<IDisposable> _disposablesForClear = new List<IDisposable>();

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            this.ComponentFieldsNotNullTest();

            deleteButtonText.text = LocalizationManager.Localize("UI_DELETE");

            itemButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    Model?.OnClick.OnNext(Model);
                    AudioController.PlayClick();
                })
                .AddTo(gameObject);
            minusButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    Model?.OnMinus.OnNext(Model);
                    AudioController.PlayClick();
                })
                .AddTo(gameObject);
            
            plusButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    Model?.OnPlus.OnNext(Model);
                    AudioController.PlayClick();
                })
                .AddTo(gameObject);
            
            deleteButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    Model?.OnDelete.OnNext(Model);
                    AudioController.PlayClick();
                })
                .AddTo(gameObject);
        }

        #endregion

        public override void SetData(T model)
        {
            if (ReferenceEquals(model, null))
            {
                Clear();
                return;
            }

            _disposablesForClear.DisposeAllAndClear();
            base.SetData(model);
            Model.Count.Subscribe(SetCount).AddTo(_disposablesForClear);
        }

        public override void Clear()
        {
            _disposablesForClear.DisposeAllAndClear();
            base.Clear();
        }
    }
}
