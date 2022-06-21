using System;
using Bencodex.Types;
using JetBrains.Annotations;
using Libplanet;
using Libplanet.Assets;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class CrystalCostState : IState
    {
        public const long DailyIntervalIndex = 7200L;
        public Address Address;
        public FungibleAssetValue CRYSTAL;
        public int Count;

        public CrystalCostState(Address address, FungibleAssetValue crystal)
        {
            Address = address;
            CRYSTAL = crystal;
        }

        public CrystalCostState(Address address, List serialized)
        {
            Address = address;
            CRYSTAL = serialized[0].ToFungibleAssetValue();
            Count = serialized[1].ToInteger();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(CRYSTAL.Serialize())
                .Add(Count.Serialize());
        }
    }
}
