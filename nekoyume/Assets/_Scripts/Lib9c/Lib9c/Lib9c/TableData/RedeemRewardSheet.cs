using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.State;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class RedeemRewardSheet : Sheet<int, RedeemRewardSheet.Row>
    {
        [Serializable]
        public class RewardInfo : IState
        {
            public readonly RewardType Type;
            public readonly int Quantity;
            public readonly int? ItemId;

            public RewardInfo(params string[] fields)
            {
                Type = (RewardType) Enum.Parse(typeof(RewardType), fields[0]);
                Quantity = ParseInt(fields[1]);
                if (Type == RewardType.Item && fields.Length > 2 && !string.IsNullOrEmpty(fields[2]))
                {
                    ItemId = ParseInt(fields[2]);
                }
            }

            public RewardInfo(Dictionary serialized)
            {
                if (serialized.TryGetValue((Text) "type", out var type))
                {
                    Type = type.ToEnum<RewardType>();
                }

                if (serialized.TryGetValue((Text) "quantity", out var quantity))
                {
                    Quantity = quantity.ToInteger();
                }

                if (serialized.TryGetValue((Text) "item_id", out var itemId))
                {
                    ItemId = itemId.ToInteger();
                }
            }

            public IValue Serialize()
            {
                var dict = new Dictionary<IKey, IValue>
                {
                    [(Text) "type"] = ((int) Type).Serialize(),
                    [(Text) "quantity"] = Quantity.Serialize(),
                };
                if (ItemId.HasValue)
                {
                    dict[(Text) "item_id"] = ItemId.Serialize();
                }
                return new Dictionary(dict);
            }

            protected bool Equals(RewardInfo other)
            {
                return Type == other.Type && Quantity == other.Quantity && ItemId == other.ItemId;
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
                    var hashCode = (int) Type;
                    hashCode = (hashCode * 397) ^ Quantity;
                    hashCode = (hashCode * 397) ^ ItemId.GetHashCode();
                    return hashCode;
                }
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public List<RewardInfo> Rewards { get; private set; }
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                var info = new RewardInfo(fields.Skip(1).ToArray());
                Rewards = new List<RewardInfo> {info};
            }
        }

        public RedeemRewardSheet() : base(nameof(RedeemRewardSheet))
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

    public enum RewardType
    {
        Item,
        Gold,
    }
}
