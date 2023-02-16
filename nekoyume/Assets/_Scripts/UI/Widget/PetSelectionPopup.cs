using Cysharp.Threading.Tasks;
using Nekoyume.UI.Module;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Subjects;
using UnityEngine;

namespace Nekoyume.UI
{
    using UniRx;

    public class PetSelectionPopup : PopupWidget
    {
        [SerializeField]
        private PetInventory petInventory;

        private IDisposable _disposableOnDisable;

        protected override void OnDisable()
        {
            base.OnDisable();
            _disposableOnDisable?.Dispose();
            _disposableOnDisable = null;
        }

        public override void Initialize()
        {
            petInventory.Initialize(true);
        }

        public void Show(Action<int?> onSelected, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            petInventory.Show();

            if (_disposableOnDisable != null)
            {
                _disposableOnDisable.Dispose();
                _disposableOnDisable = null;
            }

            _disposableOnDisable = petInventory.OnSelectedSubject
                .Subscribe(petId =>
                {
                    onSelected?.Invoke(petId);
                    Close();
                })
                .AddTo(gameObject);
        }
    }
}
