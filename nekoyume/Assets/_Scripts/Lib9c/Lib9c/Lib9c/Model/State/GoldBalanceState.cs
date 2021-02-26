using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class GoldBalanceState : State, ICloneable
    {
        public readonly FungibleAssetValue Gold;

        public GoldBalanceState(Address address, FungibleAssetValue gold) : base(address) =>
            Gold = gold;

        public GoldBalanceState(Bencodex.Types.Dictionary serialized) : base(serialized) =>
            Gold = serialized[nameof(Gold)].ToFungibleAssetValue();

        public GoldBalanceState(IValue serialized) : this((Bencodex.Types.Dictionary)serialized)
        {
        }

        public GoldBalanceState Add(FungibleAssetValue adder) =>
            new GoldBalanceState(address, Gold + adder);

        public object Clone() =>
            MemberwiseClone();

        public override IValue Serialize()
        {
            // Add("gold", (Bencodex.Types.Integer) gold)처럼 호출하면 Bencodex.Types.Integer에 int/long으로의
            // implicit 연산자가 구현되어 있어서 아래 오버로드 중에 어느 쪽을 호출하는 것인지 모호하다고 컴파일 오류가 나버림.
            // - Add(string, int)
            // - Add(string, long)
            // - Add(string, IValue)
            // 그래서 아래처럼 온몸비틀기. 그냥 Bencodex.Types.Dictionary에 Add(string, BigInteger) 하나 넣는 게 좋을 듯...
            // -> https://github.com/planetarium/bencodex.net/issues/21
            var @base = (Bencodex.Types.Dictionary) base.Serialize();
            var serialized = ((IImmutableDictionary<IKey, IValue>) @base)
                .SetItem((Text) nameof(Gold), Gold.Serialize());
            return (Bencodex.Types.Dictionary) serialized;
        }
    }
}
