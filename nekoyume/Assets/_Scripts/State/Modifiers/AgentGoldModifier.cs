using System;
using Nekoyume.JsonConvertibles;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AgentGoldModifier : AgentStateModifier
    {
        [SerializeField]
        private JsonConvertibleDecimal gold;

        public override bool IsEmpty => gold == 0m;
        
        public AgentGoldModifier(decimal gold)
        {
            this.gold = new JsonConvertibleDecimal(gold);
        }

        public override void Add(IAccumulatableStateModifier<AgentState> modifier)
        {
            if (!(modifier is AgentGoldModifier m))
                return;
            
            gold += m.gold;
        }

        public override void Remove(IAccumulatableStateModifier<AgentState> modifier)
        {
            if (!(modifier is AgentGoldModifier m))
                return;

            gold -= m.gold;
        }

        public override AgentState Modify(AgentState state)
        {
            if (state is null)
                return null;
            
            state.gold += gold.Value;
            return state;
        }

        public override string ToString()
        {
            return $"{nameof(gold)}: {gold.Value}";
        }
    }
}
