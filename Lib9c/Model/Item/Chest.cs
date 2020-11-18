using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Chest : Material
    {
        public List<RedeemRewardSheet.RewardInfo> Rewards { get; }
        public Chest(MaterialItemSheet.Row data, List<RedeemRewardSheet.RewardInfo> rewards) : base(data)
        {
            Rewards = rewards ?? new List<RedeemRewardSheet.RewardInfo>();
            ItemId = Hashcash.Hash(Serialize().EncodeIntoChunks().SelectMany(b => b).ToArray());
        }

        public Chest(Dictionary serialized) : base(serialized)
        {
            if (serialized.TryGetValue((Text) "rewards", out var rewards))
            {
                Rewards = rewards.ToList(i => new RedeemRewardSheet.RewardInfo((Dictionary) i));
            }
            // ItemFactory.CreateMaterial로 생성된 케이스
            else
            {
                Rewards = new List<RedeemRewardSheet.RewardInfo>();
                ItemId = Hashcash.Hash(Serialize().EncodeIntoChunks().SelectMany(b => b).ToArray());
            }
        }

        public Chest(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public sealed override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>()
            {
                [(Text) "rewards"] = new List(Rewards.Select(r => r.Serialize())),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002

        protected bool Equals(Chest other)
        {
            return base.Equals(other) && Rewards.SequenceEqual(other.Rewards);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Chest) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (Rewards != null ? Rewards.GetHashCode() : 0);
            }
        }
    }
}
