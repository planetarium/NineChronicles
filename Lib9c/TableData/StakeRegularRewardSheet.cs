using System;
using System.Collections.Generic;
using System.Linq;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    /// <summary>
    /// This sheet is used for setting the regular rewards for staking.
    /// The difference between this sheet and <see cref="StakeRegularFixedRewardSheet"/> is that
    /// the <see cref="RewardInfo"/> of this sheet has a rate and a reward type.
    /// The count of reward is calculated by the rate and the amount of staked currency.
    /// </summary>
    [Serializable]
    public class StakeRegularRewardSheet :
        Sheet<int, StakeRegularRewardSheet.Row>,
        IStakeRewardSheet
    {
        [Serializable]
        public class RewardInfo
        {
            public readonly int ItemId;

            /// <summary>
            /// The rate of reward.
            /// </summary>
            [Obsolete(
                "This field is used from `claim_stake_reward5` or earlier. Use `DecimalRate` instead.")]
            public readonly int Rate;

            public readonly StakeRewardType Type;

            /// <summary>
            /// The ticker of currency.
            /// This field is only used when <see cref="Type"/> is <see cref="StakeRewardType.Currency"/>.
            /// </summary>
            public readonly string CurrencyTicker;

            /// <summary>
            /// The decimal rate of reward.
            /// This field is used from `claim_stake_reward6` or later.
            /// </summary>
            public readonly decimal DecimalRate;

            public RewardInfo(params string[] fields)
            {
                ItemId = ParseInt(fields[0], 0);
                Rate = ParseInt(fields[1], 0);
                if (fields.Length == 2)
                {
                    Type = StakeRewardType.Item;
                    return;
                }

                Type = (StakeRewardType)Enum.Parse(typeof(StakeRewardType), fields[2]);
                if (fields.Length == 3)
                {
                    CurrencyTicker = null;
                    return;
                }

                CurrencyTicker = fields[3];

                if (fields.Length == 4)
                {
                    DecimalRate = Rate;
                    return;
                }

                DecimalRate = ParseDecimal(fields[4], 0m);
            }

            public RewardInfo(
                int itemId,
                int rate = 0,
                StakeRewardType type = StakeRewardType.Item,
                string currencyTicker = null,
                decimal decimalRate = 0m)
            {
                ItemId = itemId;
                Rate = rate;
                Type = type;
                CurrencyTicker = currencyTicker;
                DecimalRate = decimalRate;
            }

            protected bool Equals(RewardInfo other)
            {
                return ItemId == other.ItemId &&
                       Rate == other.Rate &&
                       Type == other.Type &&
                       (CurrencyTicker is null
                           ? other.CurrencyTicker is null
                           : CurrencyTicker == other.CurrencyTicker) &&
                       DecimalRate == other.DecimalRate;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((RewardInfo)obj);
            }

            public override int GetHashCode()
            {
                return (ItemId * 397) ^
                       Rate ^
                       (int)Type ^
                       (CurrencyTicker?.GetHashCode() ?? 0) ^
                       DecimalRate.GetHashCode();
            }
        }

        [Serializable]
        public class Row : SheetRow<int>, IStakeRewardRow
        {
            public override int Key => Level;

            public int Level { get; private set; }

            public long RequiredGold { get; private set; }

            public List<RewardInfo> Rewards { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Level = ParseInt(fields[0]);
                RequiredGold = ParseInt(fields[1]);
                var info = new RewardInfo(fields.Skip(2).ToArray());
                Rewards = new List<RewardInfo> { info };
            }
        }

        public StakeRegularRewardSheet() : base(nameof(StakeRegularRewardSheet))
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

        public IReadOnlyList<IStakeRewardRow> OrderedRows => OrderedList;

        // NOTE: This enum does not consider the hard fork when a new element is added.
        //       In other words, the elements of this enum have been expanded
        //       without versioning.
        //       The only case that the csv file of this sheet is changed
        //       is PatchTableSheet action and it can be incorporated into
        //       the block by admin only. And there were no cases below.
        //         - Failed to cast the csv value to the enum.
        //         - Use the whole element of this enum at once in actions.(e.g., count, loop)
        /// <summary>
        /// The reward type of stake.
        /// Do not use the whole element of this enum at once in actions.(e.g., count, loop)
        /// Do versioning when you change or remove the elements of this enum.
        /// </summary>
        public enum StakeRewardType
        {
            Item,
            Rune,
            Currency,
        }
    }
}
