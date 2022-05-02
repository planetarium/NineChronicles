using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Assets;
using Nekoyume.Model.State;
using static Nekoyume.TableData.TableExtensions;
using static Lib9c.SerializeKeys;

namespace Nekoyume.TableData
{
    [Serializable]
    public class StakeAchievementRewardSheet : Sheet<int, StakeAchievementRewardSheet.Row>
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

        public class Step
        {
            public long RequiredGold { get; private set; }
            public long RequiredBlockIndex { get; private set; }
            public List<RewardInfo> Rewards { get; private set; }

            public Step(long requiredGold, long requiredBlockIndex, List<RewardInfo> rewards)
            {
                RequiredGold = requiredGold;
                RequiredBlockIndex = requiredBlockIndex;
                Rewards = rewards;
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Level;
            public int Level { get; private set; }
            public List<Step> Steps { get; private set; }
            public override void Set(IReadOnlyList<string> fields)
            {
                Level = ParseInt(fields[0]);
                Steps = new List<Step>
                {
                    new Step(
                        ParseInt(fields[1]),
                        ParseInt(fields[2]),
                        new List<RewardInfo>
                        {
                            new RewardInfo(fields.Skip(3).ToArray()),
                        }),
                };
            }
        }

        public StakeAchievementRewardSheet() : base(nameof(StakeAchievementRewardSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (!value.Steps.Any())
            {
                return;
            }

            Step step = row.Steps.Find(x =>
                x.RequiredGold == value.Steps[0].RequiredGold &&
                x.RequiredBlockIndex == value.Steps[0].RequiredBlockIndex);

            if (step is null)
            {
                row.Steps.Add(value.Steps[0]);
                return;
            }

            step.Rewards.Add(value.Steps[0].Rewards[0]);
        }

        public int FindLevel(FungibleAssetValue balance)
        {
            var orderedRows = Values.OrderBy(row => row.Steps[0].RequiredGold).ToList();
            for (int i = 0; i < orderedRows.Count - 1; ++i)
            {
                if (balance.Currency * orderedRows[i].Steps[0].RequiredGold < balance &&
                    balance < balance.Currency * orderedRows[i + 1].Steps[0].RequiredGold)
                {
                    return orderedRows[i].Level;
                }
            }

            return orderedRows.Last().Level;
        }

        public int FindStep(int level, long stakedBlockPeriod)
        {
            var steps = this[level].Steps;
            int step = 0;
            for (; step < steps.Count; ++step)
            {
                if (stakedBlockPeriod < steps[step].RequiredBlockIndex)
                {
                    break;
                }
            }

            return step;
        }
    }
}
