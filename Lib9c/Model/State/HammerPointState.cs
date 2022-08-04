using Bencodex.Types;
using Libplanet;

namespace Nekoyume.Model.State
{
    public class HammerPointState : IState
    {
        public Address Address { get; }
        public int ItemId { get; }
        public int HammerPoint { get; private set; }

        public HammerPointState(Address address, int itemId)
        {
            Address = address;
            ItemId = itemId;
            HammerPoint = 0;
        }

        public HammerPointState(Address address, List serialized)
        {
            Address = address;
            ItemId = serialized[0].ToInteger();
            HammerPoint = serialized[1].ToInteger();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(ItemId.Serialize())
                .Add(HammerPoint.Serialize());
        }

        public void UpdateHammerPoint(int point)
        {
            HammerPoint += point;
        }

        public void ResetHammerPoint()
        {
            HammerPoint = 0;
        }
    }
}
