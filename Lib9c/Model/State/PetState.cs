using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;

namespace Nekoyume.Model.State
{
    public class PetState : IState
    {
        public static Address DeriveAddress(Address avatarAddress, int petId) =>
            avatarAddress.Derive($"{petId}");

        public int PetId { get; }
        public int Level { get; private set; }

        public PetState(int petId)
        {
            PetId = petId;
        }

        public PetState(List serialized)
        {
            PetId = serialized[0].ToInteger();
            Level = serialized[1].ToInteger();
        }

        public IValue Serialize()
        {
            var result = List.Empty
                .Add(PetId.Serialize())
                .Add(Level.Serialize());
            return result;
        }

        public void LevelUp()
        {
            Level++;
        }
    }
}
