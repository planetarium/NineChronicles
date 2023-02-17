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

        public readonly Subject<PetStates> PetStatesSubject = new();

        public bool TryGetPetState(int id, out PetState pet)
        {
            pet = GetPetState(id);
            return pet != null;
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
            _lockedPets.Clear();
            PetStatesSubject.OnNext(this);
        }

        public void LockPetTemporarily(int petID)
        {
            if (!_lockedPets.Contains(petID))
            {
                _lockedPets.Add(petID);
            }
        }

        public bool IsLocked(int petId)
        {
            return _lockedPets.Contains(petId);
        }
    }
}
