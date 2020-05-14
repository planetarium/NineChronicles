using System;
using System.Collections.Generic;
using System.Linq;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class RedeemRewardSheet : Sheet<int, RedeemRewardSheet.Row>
    {
        [Serializable]
        public class RewardInfo
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
                return;

            row.Rewards.Add(value.Rewards[0]);
        }

    }

    public enum RewardType
    {
        Item,
        Gold,
    }
}
