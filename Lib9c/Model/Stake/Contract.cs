using System;
using Bencodex.Types;

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

        public Contract(
            string stakeRegularFixedRewardSheetTableName,
            string stakeRegularRewardSheetTableName,
            long rewardInterval,
            long lockupInterval)
        {
            if (!stakeRegularFixedRewardSheetTableName.StartsWith(
                    StakeRegularFixedRewardSheetPrefix))
            {
                throw new ArgumentException(nameof(stakeRegularFixedRewardSheetTableName));
            }

            if (!stakeRegularRewardSheetTableName.StartsWith(StakeRegularRewardSheetPrefix))
            {
                throw new ArgumentException(nameof(stakeRegularRewardSheetTableName));
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
    }
}
