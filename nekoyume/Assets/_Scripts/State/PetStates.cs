using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.State;

namespace Nekoyume.State
{
    using UniRx;

    public class PetStates
    {
        private readonly Dictionary<int, PetState> _petDict = new();

        private readonly HashSet<int> _lockedPets = new();

        private readonly Subject<PetStates> petStatesInternal;
        
        public readonly IObservable<PetStates> PetStatesSubject;
        
        public PetStates()
        {
            petStatesInternal = new Subject<PetStates>();
            PetStatesSubject = petStatesInternal.ObserveOnMainThread();
        }

        public bool TryGetPetState(int id, out PetState pet)
        {
            return _petDict.TryGetValue(id, out pet) && pet is not null;
        }

        public PetState GetPetState(int id)
        {
            return _petDict.TryGetValue(id, out var petState) ? petState : null;
        }

        public List<PetState> GetPetStatesAll()
        {
            return _petDict.Values.ToList();
        }

        public void UpdatePetState(int id, PetState petState)
        {
            _petDict[id] = petState;
            if (_lockedPets.Contains(id))
            {
                _lockedPets.Remove(id);
            }

            petStatesInternal.OnNext(this);
        }

        public void LockPetTemporarily(int? petId)
        {
            if (petId.HasValue)
            {
                _lockedPets.Add(petId.Value);
            }
        }

        public bool IsLocked(int petId)
        {
            return _lockedPets.Contains(petId);
        }
    }
}
