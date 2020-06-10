using System;
using System.Collections.Generic;
using System.Linq;
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
        }

        public sealed override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>()
            {
                [(Text) "rewards"] = new List(Rewards.Select(r => r.Serialize())),
            }.Union((Dictionary) base.Serialize()));

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
