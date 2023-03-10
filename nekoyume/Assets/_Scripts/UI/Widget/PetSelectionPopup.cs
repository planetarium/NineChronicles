using Nekoyume.UI.Module;
using System;
using UnityEngine;

namespace Nekoyume.UI
{
    using System.Collections;
    using UniRx; 

    public class PetSelectionPopup : PopupWidget
    {
        [SerializeField]
        private PetInventory petInventory;

        private IDisposable _disposableOnDisable;

        private Coroutine _coroutineOnShowanimation;

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

        public void Show(
            Craft.CraftInfo craftInfo,
            Action<int?> onSelected,
            bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            petInventory.Show(craftInfo);

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
            _coroutineOnShowanimation = StartCoroutine(CoFixScrollPosition());
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
            if (_coroutineOnShowanimation != null)
            {
                StopCoroutine(_coroutineOnShowanimation);
                _coroutineOnShowanimation = null;
            }
        }

        private IEnumerator CoFixScrollPosition()
        {
            while (gameObject.activeSelf)
            {
                petInventory.InitScrollPosition();
                yield return null;
            }

            _coroutineOnShowanimation = null;
        }
    }
}
