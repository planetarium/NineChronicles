using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;

namespace Nekoyume.State
{
    /// <summary>
    /// Agent의 상태 모델이다.
    /// </summary>
    [Serializable]
    public class AgentState : State, ICloneable
    {
        //F&F 테스트용 노마이너 기본 소지 골드
        public decimal gold = 1000;
        public readonly Dictionary<int, Address> avatarAddresses;

        public AgentState(Address address) : base(address)
        {
            avatarAddresses = new Dictionary<int, Address>();
        }

        public AgentState(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
            avatarAddresses = ((Bencodex.Types.Dictionary) serialized[(Text) "avatarAddresses"])
                .Where(kv => kv.Key is Binary)
                .ToDictionary(
                    kv => BitConverter.ToInt32(((Binary) kv.Key).Value, 0),
                    kv => kv.Value.ToAddress()
                );
            gold = serialized[(Text) "gold"].ToDecimal();
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "avatarAddresses"] = new Bencodex.Types.Dictionary(
                    avatarAddresses.Select(kv =>
                        new KeyValuePair<IKey, IValue>(
                            new Binary(BitConverter.GetBytes(kv.Key)),
                            kv.Value.Serialize()
                        )
                    )
                ),
                [(Text) "gold"] = gold.Serialize(),
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
    }
}
