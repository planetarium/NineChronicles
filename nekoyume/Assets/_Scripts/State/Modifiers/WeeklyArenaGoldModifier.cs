using System;
using Nekoyume.JsonConvertibles;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class WeeklyArenaGoldModifier : WeeklyArenaStateModifier
    {
        [SerializeField]
        private JsonConvertibleDecimal gold;

        public override bool IsEmpty => gold == 0m;
        
        public WeeklyArenaGoldModifier(decimal gold)
        {
            this.gold = new JsonConvertibleDecimal(gold);
        }

        public override void Add(IAccumulatableStateModifier<WeeklyArenaState> modifier)
        {
            if (!(modifier is WeeklyArenaGoldModifier m))
                return;
            
            gold += m.gold;
        }

        public override void Remove(IAccumulatableStateModifier<WeeklyArenaState> modifier)
        {
            if (!(modifier is WeeklyArenaGoldModifier m))
                return;

            gold -= m.gold;
        }

        public override WeeklyArenaState Modify(WeeklyArenaState state)
        {
            if (state is null)
                return null;
            
            state.Gold += gold.Value;
            return state;
        }

        public override string ToString()
        {
            return $"{nameof(gold)}: {gold.Value}";
        }
    }
}
