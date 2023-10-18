using System;
using Lib9c;
using Libplanet.Types.Assets;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AgentCrystalModifier : IAccumulatableValueModifier<FungibleAssetValue>
    {
        [SerializeField]
        private FungibleAssetValue crystal;

        public bool IsEmpty => crystal.Sign == 0;

        public AgentCrystalModifier(FungibleAssetValue crystal)
        {
            if (!crystal.Currency.Equals(Currencies.Crystal))
            {
                this.crystal = Currencies.Crystal * 0;
                return;
            }

            this.crystal = crystal;
        }

        public void Add(IAccumulatableValueModifier<FungibleAssetValue> modifier)
        {
            if (modifier is not AgentCrystalModifier m)
            {
                return;
            }

            crystal += m.crystal;
        }

        public void Remove(IAccumulatableValueModifier<FungibleAssetValue> modifier)
        {
            if (modifier is not AgentCrystalModifier m)
            {
                return;
            }

            crystal -= m.crystal;
        }

        public FungibleAssetValue Modify(FungibleAssetValue value)
        {
            //return value + crystal;
            return value;
        }

        public override string ToString() => crystal.ToString();
    }
}
