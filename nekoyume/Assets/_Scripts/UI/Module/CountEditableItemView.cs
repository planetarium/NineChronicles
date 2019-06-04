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
                    Data?.onEdit.OnNext(Data);
                    AudioController.PlayClick();
                })
                .AddTo(_disposablesForAwake);

            deleteButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    Data?.onDelete.OnNext(Data);
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

        public override void SetData(T value)
        {
            if (ReferenceEquals(value, null))
            {
                Clear();
                return;
            }

            _disposablesForSetData.DisposeAllAndClear();
            base.SetData(value);
            Data.count.Subscribe(SetCount).AddTo(_disposablesForSetData);
        }

        public override void Clear()
        {
            _disposablesForSetData.DisposeAllAndClear();
            base.Clear();
        }
    }
}
