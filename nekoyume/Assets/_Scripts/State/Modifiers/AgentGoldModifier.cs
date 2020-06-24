using System;
using System.Globalization;
using System.Numerics;
using Nekoyume.JsonConvertibles;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AgentGoldModifier : IAccumulatableStateModifier<GoldBalanceState>
    {
        [SerializeField]
        private string goldString;

        public bool dirty { get; set; }

        public bool IsEmpty => Gold == 0;

        private BigInteger Gold
        {
            get => BigInteger.Parse(goldString, CultureInfo.InvariantCulture);
            set => goldString = Gold.ToString(CultureInfo.InvariantCulture);
        }

        public AgentGoldModifier(BigInteger gold)
        {
            Gold = gold;
        }

        public void Add(IAccumulatableStateModifier<GoldBalanceState> modifier)
        {
            if (!(modifier is AgentGoldModifier m))
                return;

            Gold += m.Gold;
        }

        public void Remove(IAccumulatableStateModifier<GoldBalanceState> modifier)
        {
            if (!(modifier is AgentGoldModifier m))
                return;

            Gold -= m.Gold;
        }

        public GoldBalanceState Modify(GoldBalanceState state) =>
            state?.Add(Gold);

        public override string ToString()
        {
            return $"{nameof(Gold)}: {Gold}";
        }
    }
}
