using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;

namespace Nekoyume.Model.State
{
    public class PetState : IState
    {
        public static Address DeriveAddress(Address avatarAddress, int petId) =>
            avatarAddress.Derive($"pet-{petId}");

        public int PetId { get; }
        public int Level { get; private set; }

        public PetState(int petId)
        {
            PetId = petId;
            Level = 0;
        }

        public PetState(List serialized)
        {
            PetId = serialized[0].ToInteger();
            Level = serialized[1].ToInteger();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(PetId.Serialize())
                .Add(Level.Serialize());
        }

        public void LevelUp()
        {
            if (Level == int.MaxValue)
            {
                throw new System.InvalidOperationException("Pet level is already max.");
            }

            Level++;
        }
    }
}
