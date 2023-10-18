using System;
using Libplanet.Types.Assets;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AgentNCGModifier : IAccumulatableValueModifier<FungibleAssetValue>
    {
        [SerializeField]
        private FungibleAssetValue ncg;

        public bool IsEmpty => ncg.Sign == 0;

        public AgentNCGModifier(FungibleAssetValue ncg)
        {
            this.ncg = ncg;
        }

        public void Add(IAccumulatableValueModifier<FungibleAssetValue> modifier)
        {
            if (modifier is not AgentNCGModifier m)
            {
                return;
            }

            ncg += m.ncg;
        }

        public void Remove(IAccumulatableValueModifier<FungibleAssetValue> modifier)
        {
            if (modifier is not AgentNCGModifier m)
            {
                return;
            }

            ncg -= m.ncg;
        }

        public FungibleAssetValue Modify(FungibleAssetValue value)
        {
            //return value + ncg;
            return value;
        }

        public override string ToString() => ncg.ToString();
    }
}
