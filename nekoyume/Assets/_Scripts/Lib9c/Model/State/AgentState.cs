using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;

namespace Nekoyume.Model.State
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
        public HashSet<int> unlockedOptions;

        public AgentState(Address address) : base(address)
        {
            avatarAddresses = new Dictionary<int, Address>();
            unlockedOptions = new HashSet<int>();
        }

        public AgentState(Dictionary serialized)
            : base(serialized)
        {
            avatarAddresses = ((Dictionary)serialized["avatarAddresses"])
                .Where(kv => kv.Key is Binary)
                .ToDictionary(
                    kv => BitConverter.ToInt32(((Binary)kv.Key).Value, 0),
                    kv => kv.Value.ToAddress()
                );
            gold = serialized["gold"].ToDecimal();
            unlockedOptions = serialized.ContainsKey((Text) "unlockedOptions")
                ? serialized["unlockedOptions"].ToHashSet(StateExtensions.ToInteger)
                : new HashSet<int>();
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"avatarAddresses"] = new Dictionary(
                    avatarAddresses.Select(kv =>
                        new KeyValuePair<IKey, IValue>(
                            new Binary(BitConverter.GetBytes(kv.Key)),
                            kv.Value.Serialize()
                        )
                    )
                ),
                [(Text)"gold"] = gold.Serialize(),
                [(Text)"unlockedOptions"] = unlockedOptions.Select(i => i.Serialize()).Serialize(),
            }.Union((Dictionary)base.Serialize()));
    }
}
