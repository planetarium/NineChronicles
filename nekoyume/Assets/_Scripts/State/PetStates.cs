using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.State;

namespace Nekoyume.State
{
    public class PetStates
    {
        private readonly Dictionary<int, PetState> _petDict = new();

        public bool TryGetPetState(int id, out PetState pet)
        {
            return _petDict.TryGetValue(id, out pet);
        }

        public List<PetState> GetPetStatesAll()
        {
            return _petDict.Values.ToList();
        }

        public void UpdatePetState(int id, PetState petState)
        {
            _petDict[id] = petState;
        }
    }
}
