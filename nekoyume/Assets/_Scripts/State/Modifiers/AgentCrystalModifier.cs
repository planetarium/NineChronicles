using System;
using System.Numerics;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AgentCrystalModifier : AgentStateModifier
    {
        [SerializeField]
        private FungibleAssetValue crystal;

        public override bool IsEmpty => crystal.Sign == 0;

        public AgentCrystalModifier(FungibleAssetValue crystal)
        {
            this.crystal = crystal;
        }

        public override void Add(IAccumulatableStateModifier<AgentState> modifier)
        {
            if (!(modifier is AgentCrystalModifier m))
            {
                return;
            }

            crystal += m.crystal;
        }

        public override void Remove(IAccumulatableStateModifier<AgentState> modifier)
        {
            if (!(modifier is AgentCrystalModifier m))
            {
                return;
            }

            crystal -= m.crystal;
        }

        public override AgentState Modify(AgentState state)
        {
            return state;
        }

        public override string ToString()
        {
            return $"{nameof(crystal)}: {crystal.MajorUnit}";
        }
    }
}
