using System;
using System.Collections.Immutable;
using System.Numerics;
using Bencodex.Types;
using Libplanet;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class GoldBalanceState : State, ICloneable
    {
        public readonly BigInteger gold;

        public GoldBalanceState(Address address) : this(address, 0)
        {
        }

        public GoldBalanceState(Address address, BigInteger gold) : base(address) =>
            this.gold = gold;

        public GoldBalanceState(Bencodex.Types.Dictionary serialized) : base(serialized) =>
            gold = (Bencodex.Types.Integer)serialized["gold"];

        public GoldBalanceState(IValue serialized) : this((Bencodex.Types.Dictionary)serialized)
        {
        }

        public GoldBalanceState Add(BigInteger adder) =>
            new GoldBalanceState(address, gold + adder);

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
                .SetItem((Text) "gold", (Bencodex.Types.Integer) gold);
            return (Bencodex.Types.Dictionary) serialized;
        }
    }
}
