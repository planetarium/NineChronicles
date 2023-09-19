using System;
using Bencodex.Types;
using Nekoyume.TableData.Stake;

namespace Nekoyume.Model.Stake
{
    public class Contract
    {
        public const string StateTypeName = "stake_contract";
        public const long StateTypeVersion = 1;

        public const string StakeRegularFixedRewardSheetPrefix
            = "StakeRegularFixedRewardSheet_";

        public const string StakeRegularRewardSheetPrefix
            = "StakeRegularRewardSheet_";

        public string StakeRegularFixedRewardSheetTableName { get; }
        public string StakeRegularRewardSheetTableName { get; }
        public long RewardInterval { get; }
        public long LockupInterval { get; }

        public Contract(StakePolicySheet stakePolicySheet) : this(
            stakePolicySheet?.StakeRegularFixedRewardSheetValue ?? throw new ArgumentNullException(
                nameof(stakePolicySheet),
                $"{nameof(stakePolicySheet)} is null"),
            stakePolicySheet.StakeRegularRewardSheetValue,
            stakePolicySheet.RewardIntervalValue,
            stakePolicySheet.LockupIntervalValue)
        {
        }

        public Contract(
            string stakeRegularFixedRewardSheetTableName,
            string stakeRegularRewardSheetTableName,
            long rewardInterval,
            long lockupInterval)
        {
            if (string.IsNullOrEmpty(stakeRegularFixedRewardSheetTableName))
            {
                throw new ArgumentException(
                    $"{nameof(stakeRegularFixedRewardSheetTableName)} is null or empty");
            }

            if (!stakeRegularFixedRewardSheetTableName.StartsWith(
                    StakeRegularFixedRewardSheetPrefix))
            {
                throw new ArgumentException(nameof(stakeRegularFixedRewardSheetTableName));
            }

            if (string.IsNullOrEmpty(stakeRegularRewardSheetTableName))
            {
                throw new ArgumentException(
                    $"{nameof(stakeRegularRewardSheetTableName)} is null or empty");
            }

            if (!stakeRegularRewardSheetTableName.StartsWith(StakeRegularRewardSheetPrefix))
            {
                throw new ArgumentException(nameof(stakeRegularRewardSheetTableName));
            }

            if (rewardInterval <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rewardInterval),
                    $"{nameof(rewardInterval)} must be greater than 0");
            }

            if (lockupInterval <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lockupInterval),
                    $"{nameof(lockupInterval)} must be greater than 0");
            }

            StakeRegularFixedRewardSheetTableName = stakeRegularFixedRewardSheetTableName;
            StakeRegularRewardSheetTableName = stakeRegularRewardSheetTableName;
            RewardInterval = rewardInterval;
            LockupInterval = lockupInterval;
        }

        public Contract(IValue serialized)
        {
            if (serialized is not List list)
            {
                throw new ArgumentException(
                    nameof(serialized),
                    $"{nameof(serialized)} is not List");
            }

            if (list[0] is not Text typeName ||
                (string)typeName != StateTypeName)
            {
                throw new ArgumentException(
                    nameof(serialized),
                    $"State type name is not {StateTypeName}");
            }

            if (list[1] is not Integer typeVersion ||
                (long)typeVersion != StateTypeVersion)
            {
                throw new ArgumentException(
                    nameof(serialized),
                    $"State type version is not {StateTypeVersion}");
            }

            const int reservedCount = 2;
            StakeRegularFixedRewardSheetTableName = (Text)list[reservedCount];
            StakeRegularRewardSheetTableName = (Text)list[reservedCount + 1];
            RewardInterval = (Integer)list[reservedCount + 2];
            LockupInterval = (Integer)list[reservedCount + 3];
        }

        public List Serialize()
        {
            return new List(
                (Text)StateTypeName,
                (Integer)StateTypeVersion,
                (Text)StakeRegularFixedRewardSheetTableName,
                (Text)StakeRegularRewardSheetTableName,
                (Integer)RewardInterval,
                (Integer)LockupInterval
            );
        }

        protected bool Equals(Contract other)
        {
            return StakeRegularFixedRewardSheetTableName ==
                   other.StakeRegularFixedRewardSheetTableName &&
                   StakeRegularRewardSheetTableName == other.StakeRegularRewardSheetTableName &&
                   RewardInterval == other.RewardInterval &&
                   LockupInterval == other.LockupInterval;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Contract)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = StakeRegularFixedRewardSheetTableName != null
                    ? StakeRegularFixedRewardSheetTableName.GetHashCode()
                    : 0;
                hashCode = (hashCode * 397) ^ (StakeRegularRewardSheetTableName != null
                    ? StakeRegularRewardSheetTableName.GetHashCode()
                    : 0);
                hashCode = (hashCode * 397) ^ RewardInterval.GetHashCode();
                hashCode = (hashCode * 397) ^ LockupInterval.GetHashCode();
                return hashCode;
            }
        }
    }
}
