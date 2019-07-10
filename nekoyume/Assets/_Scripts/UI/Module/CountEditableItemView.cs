using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CountEditableItemView<T> : CountableItemView<T> where T : Model.CountEditableItem
    {
        public Button editButton;
        public Button deleteButton;

        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            this.ComponentFieldsNotNullTest();

            editButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    Model?.onEdit.OnNext(Model);
                    AudioController.PlayClick();
                })
                .AddTo(_disposablesForAwake);

            deleteButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    Model?.onDelete.OnNext(Model);
                    AudioController.PlayClick();
                })
                .AddTo(_disposablesForAwake);
        }

        protected override void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();

            base.OnDestroy();
        }

        #endregion

        public override void SetData(T model)
        {
            if (ReferenceEquals(model, null))
            {
                Clear();
                return;
            }

            _disposablesForSetData.DisposeAllAndClear();
            base.SetData(model);
            Model.count.Subscribe(SetCount).AddTo(_disposablesForSetData);
        }

        public override void Clear()
        {
            _disposablesForSetData.DisposeAllAndClear();
            base.Clear();
        }
    }
}
