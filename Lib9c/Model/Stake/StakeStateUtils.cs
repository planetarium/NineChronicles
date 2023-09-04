using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Stake
{
    public static class StakeStateUtils
    {
        // FIXME: Use StakePolicySheet instead of hard-coding.
        public static bool TryMigrate(
            IAccountState state,
            Address stakeStateAddr,
            out StakeStateV2 stakeStateV2)
        {
            var serialized = state.GetState(stakeStateAddr);
            if (serialized is null or Null)
            {
                stakeStateV2 = default;
                return false;
            }

            if (serialized is List list)
            {
                stakeStateV2 = new StakeStateV2(list);
                return true;
            }

            if (serialized is not Dictionary dict)
            {
                stakeStateV2 = default;
                return false;
            }

            var gameConfigState = state.GetGameConfigState();
            if (gameConfigState is null)
            {
                stakeStateV2 = default;
                return false;
            }

            var stakeStateV1 = new StakeState(dict);
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

            stakeStateV2 = new StakeStateV2(
                stakeStateV1,
                new Contract(
                    stakeRegularFixedRewardSheetTableName: stakeRegularFixedRewardSheetTableName,
                    stakeRegularRewardSheetTableName: stakeRegularRewardSheetTableName,
                    rewardInterval: StakeState.RewardInterval,
                    lockupInterval: StakeState.LockupInterval));
            return true;
        }
    }
}
