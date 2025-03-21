using Nekoyume.UI.Module;
using System;
using System.Linq;
using Nekoyume.State;
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
#if UNITY_ANDROID || UNITY_IOS
            var petCount = States.Instance.PetStates.GetPetStatesAll()
                .Where(petState => petState != null);
            if (!petCount.Any())
            {
                onSelected.Invoke(null);
                return;
            }
#endif
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
                    if (petId != null)
                    {
                        var petStates = States.Instance.PetStates;
                        var petStateList = petStates.GetPetStatesAll();
                            
                        var equippablePet = petStateList.Where(petState => 
                            petState != null && 
                            petState.PetId == petId &&
                            !IsEquippedPet(petStates, petState, petId.Value));
                        if (!equippablePet.Any())
                        {
                            return;
                        }
                    }
                    
                    onSelected?.Invoke(petId);
                    Close();
                })
                .AddTo(gameObject);
            _coroutineOnShowanimation = StartCoroutine(CoFixScrollPosition());
        }

        private bool IsEquippedPet(PetStates petStates, Nekoyume.Model.State.PetState petState, int petId)
        {
            return petStates.IsLocked(petId) || petState.UnlockedBlockIndex > Game.Game.instance.Agent.BlockIndex;
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
