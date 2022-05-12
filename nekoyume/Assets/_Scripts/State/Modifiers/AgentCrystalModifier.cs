using System;
using System.Numerics;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.Helper;
using Nekoyume.Model.State;
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
            if (!crystal.Currency.Equals(CrystalCalculator.CRYSTAL))
            {
                this.crystal = CrystalCalculator.CRYSTAL * 0;
                return;
            }

            this.crystal = crystal;
        }

        public void Add(IAccumulatableValueModifier<FungibleAssetValue> modifier)
        {
            if (!(modifier is AgentCrystalModifier m) ||
                !crystal.Currency.Equals(CrystalCalculator.CRYSTAL))
            {
                return;
            }

            crystal += m.crystal;
        }

        public void Remove(IAccumulatableValueModifier<FungibleAssetValue> modifier)
        {
            if (!(modifier is AgentCrystalModifier m) ||
                !crystal.Currency.Equals(CrystalCalculator.CRYSTAL))
            {
                return;
            }

            crystal -= m.crystal;
        }

        public FungibleAssetValue Modify(FungibleAssetValue value)
        {
            //if (!crystal.Currency.Equals(CrystalCalculator.CRYSTAL))
            //{
            //    return value;
            //}

            //return value + crystal;
            return value;
        }

        public override string ToString()
        {
            return $"{nameof(crystal)}: {crystal.MajorUnit}";
        }
    }
}
