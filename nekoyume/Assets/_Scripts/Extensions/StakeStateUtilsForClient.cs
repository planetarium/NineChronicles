using Bencodex.Types;
using Nekoyume.Model.Stake;
using Nekoyume.Model.State;

namespace Nekoyume
{
    public static class StakeStateUtilsForClient
    {
        /// NOTE: This method is copy version of <see cref="StakeStateUtils.TryMigrate"/>.
        public static bool TryMigrate(
            IValue serialized,
            GameConfigState gameConfigState,
            out StakeState stakeStateV2)
        {
            if (serialized is null or Null)
            {
                stakeStateV2 = default;
                return false;
            }

            // NOTE: StakeStateV2 is serialized as Bencodex List.
            if (serialized is List list)
            {
                stakeStateV2 = new StakeState(list);
                return true;
            }

            // NOTE: StakeState is serialized as Bencodex Dictionary.
            if (serialized is not Dictionary dict)
            {
                stakeStateV2 = default;
                return false;
            }

            // NOTE: Migration needs GameConfigState.
            if (gameConfigState is null)
            {
                stakeStateV2 = default;
                return false;
            }

            // NOTE: Below is the migration logic from StakeState to StakeStateV2.
            //       The migration logic is based on the following assumptions:
            //       - The migration target is StakeState which is serialized as Bencodex Dictionary.
            //       - The started block index of StakeState is less than or equal to ActionObsoleteConfig.V200080ObsoleteIndex.
            //       - Migrated StakeStateV2 will be contracted by StakeStateV2.Contract.
            //       - StakeStateV2.Contract.StakeRegularFixedRewardSheetTableName is one of the following:
            //         - "StakeRegularFixedRewardSheet_V1"
            //         - "StakeRegularFixedRewardSheet_V2"
            //       - StakeStateV2.Contract.StakeRegularRewardSheetTableName is one of the following:
            //         - "StakeRegularRewardSheet_V1"
            //         - "StakeRegularRewardSheet_V2"
            //         - "StakeRegularRewardSheet_V3"
            //         - "StakeRegularRewardSheet_V4"
            //         - "StakeRegularRewardSheet_V5"
            //       - StakeStateV2.Contract.RewardInterval is StakeState.RewardInterval.
            //       - StakeStateV2.Contract.LockupInterval is StakeState.LockupInterval.
            //       - StakeStateV2.StartedBlockIndex is StakeState.StartedBlockIndex.
            //       - StakeStateV2.ReceivedBlockIndex is StakeState.ReceivedBlockIndex.
            var stakeStateV1 = new LegacyStakeState(dict);
            var stakeRegularFixedRewardSheetTableName =
                stakeStateV1.StartedBlockIndex <
                gameConfigState.StakeRegularFixedRewardSheet_V2_StartBlockIndex
                    ? "StakeRegularFixedRewardSheet_V1"
                    : "StakeRegularFixedRewardSheet_V2";
            string stakeRegularRewardSheetTableName;
            if (stakeStateV1.StartedBlockIndex <
                gameConfigState.StakeRegularRewardSheet_V2_StartBlockIndex)
            {
                stakeRegularRewardSheetTableName = "StakeRegularRewardSheet_V1";
            }
            else if (stakeStateV1.StartedBlockIndex <
                gameConfigState.StakeRegularRewardSheet_V3_StartBlockIndex)
            {
                stakeRegularRewardSheetTableName = "StakeRegularRewardSheet_V2";
            }
            else if (stakeStateV1.StartedBlockIndex <
                gameConfigState.StakeRegularRewardSheet_V4_StartBlockIndex)
            {
                stakeRegularRewardSheetTableName = "StakeRegularRewardSheet_V3";
            }
            else if (stakeStateV1.StartedBlockIndex <
                gameConfigState.StakeRegularRewardSheet_V5_StartBlockIndex)
            {
                stakeRegularRewardSheetTableName = "StakeRegularRewardSheet_V4";
            }
            else
            {
                stakeRegularRewardSheetTableName = "StakeRegularRewardSheet_V5";
            }

            stakeStateV2 = new StakeState(
                stakeStateV1,
                new Contract(
                    stakeRegularFixedRewardSheetTableName,
                    stakeRegularRewardSheetTableName,
                    LegacyStakeState.RewardInterval,
                    LegacyStakeState.LockupInterval));
            return true;
        }
    }
}
