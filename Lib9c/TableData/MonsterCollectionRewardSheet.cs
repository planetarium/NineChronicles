using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.State;
using static Nekoyume.TableData.TableExtensions;
using static Lib9c.SerializeKeys;

namespace Nekoyume.TableData
{
    [Serializable]
    public class MonsterCollectionRewardSheet : Sheet<int, MonsterCollectionRewardSheet.Row>
    {
        [Serializable]
        public class RewardInfo
        {
            protected bool Equals(RewardInfo other)
            {
                return ItemId == other.ItemId && Quantity == other.Quantity;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((RewardInfo) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (ItemId * 397) ^ Quantity;
                }
            }

            public readonly int ItemId;
            public readonly int Quantity;

            public RewardInfo(params string[] fields)
            {
                ItemId = ParseInt(fields[0]);
                Quantity = ParseInt(fields[1]);
            }

            public RewardInfo(int itemId, int quantity)
            {
                ItemId = itemId;
                Quantity = quantity;
            }

            public RewardInfo(Dictionary dictionary)
            {
                ItemId = dictionary[IdKey].ToInteger();
                Quantity = dictionary[QuantityKey].ToInteger();
            }
            public IValue Serialize()
            {
                return Dictionary.Empty
                    .Add(IdKey, ItemId.Serialize())
                    .Add(QuantityKey, Quantity.Serialize());
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => MonsterCollectionLevel;
            public int MonsterCollectionLevel { get; private set; }
            public List<RewardInfo> Rewards { get; private set; }
            public override void Set(IReadOnlyList<string> fields)
            {
                MonsterCollectionLevel = ParseInt(fields[0]);
                var info = new RewardInfo(fields.Skip(1).ToArray());
                Rewards = new List<RewardInfo> {info};
            }
        }

        public MonsterCollectionRewardSheet() : base(nameof(MonsterCollectionRewardSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (!value.Rewards.Any())
            {
                return;
            }

            row.Rewards.Add(value.Rewards[0]);
        }

    }
}
