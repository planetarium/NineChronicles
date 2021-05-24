using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// Agent의 상태 모델이다.
    /// </summary>
    [Serializable]
    public class AgentState : State, ICloneable
    {
        public readonly Dictionary<int, Address> avatarAddresses;
        public HashSet<int> unlockedOptions;
        public int MonsterCollectionRound { get; private set; }

        public AgentState(Address address) : base(address)
        {
            avatarAddresses = new Dictionary<int, Address>();
            unlockedOptions = new HashSet<int>();
        }

        public AgentState(Dictionary serialized)
            : base(serialized)
        {
#pragma warning disable LAA1002
            avatarAddresses = ((Dictionary)serialized["avatarAddresses"])
                .Where(kv => kv.Key is Binary)
                .ToDictionary(
                    kv => BitConverter.ToInt32(((Binary)kv.Key).ToByteArray(), 0),
                    kv => kv.Value.ToAddress()
                );
#pragma warning restore LAA1002
            unlockedOptions = serialized.ContainsKey((IKey)(Text) "unlockedOptions")
                ? serialized["unlockedOptions"].ToHashSet(StateExtensions.ToInteger)
                : new HashSet<int>();
            MonsterCollectionRound = serialized.ContainsKey((IKey) (Text) MonsterCollectionRoundKey)
                ? serialized[MonsterCollectionRoundKey].ToInteger()
                : 0;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public void IncreaseCollectionRound()
        {
            MonsterCollectionRound++;
        }

        public override IValue Serialize()
        {
            var innerDict = new Dictionary<IKey, IValue>
            {
#pragma warning disable LAA1002
                [(Text) "avatarAddresses"] = new Dictionary(
                    avatarAddresses.Select(kv =>
                        new KeyValuePair<IKey, IValue>(
                            new Binary(BitConverter.GetBytes(kv.Key)),
                            kv.Value.Serialize()
                        )
                    )
                ),
                [(Text) "unlockedOptions"] = unlockedOptions.Select(i => i.Serialize()).Serialize(),
            };
            if (MonsterCollectionRound > 0)
            {
                innerDict.Add((Text) MonsterCollectionRoundKey, MonsterCollectionRound.Serialize());
            }
            return new Dictionary(innerDict.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }
    }
}
