using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.JsonConvertibles;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AgentGoldModifier : IAccumulatableStateModifier<GoldBalanceState>
    {
        [SerializeField]
        private string hex;

        public bool dirty { get; set; }

        public bool IsEmpty => Gold.Sign == 0;

        private FungibleAssetValue Gold
        {
            get
            {
                var serialized = (Bencodex.Types.List) new Codec().Decode(ByteUtil.ParseHex(hex));
                return new FungibleAssetValue(
                    CurrencyExtensions.Deserialize(
                        (Bencodex.Types.Dictionary) serialized.ElementAt(0)),
                    serialized.ElementAt(1).ToBigInteger(),
                    serialized.ElementAt(2).ToBigInteger());
            }
            set
            {
                var serialized = new Bencodex.Types.List(new IValue[]
                {
                    value.Currency.Serialize(),
                    (Integer) value.MajorUnit.Serialize(),
                    (Integer) value.MinorUnit.Serialize(),
                });

                hex = ByteUtil.Hex(new Codec().Encode(serialized));
            }
        }

        public AgentGoldModifier(FungibleAssetValue gold)
        {
            Gold = gold;
        }

        public AgentGoldModifier(Currency currency, int gold) : this(
            new FungibleAssetValue(currency, gold, 0))
        {
        }

        public void Add(IAccumulatableStateModifier<GoldBalanceState> modifier)
        {
            if (!(modifier is AgentGoldModifier m))
            {
                return;
            }

            Gold += m.Gold;
        }

        public void Remove(IAccumulatableStateModifier<GoldBalanceState> modifier)
        {
            if (!(modifier is AgentGoldModifier m))
            {
                return;
            }

            Gold -= m.Gold;
        }

        public GoldBalanceState Modify(ref GoldBalanceState state) =>
            state is null
                ? null
                : state = state.Add(Gold);

        public override string ToString()
        {
            return $"{nameof(Gold)}: {Gold}";
        }
    }
}
