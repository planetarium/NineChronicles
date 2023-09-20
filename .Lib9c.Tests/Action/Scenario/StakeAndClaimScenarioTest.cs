namespace Lib9c.Tests.Action.Scenario
{
    using System.Collections.Generic;
    using Bencodex.Types;
    using Lib9c.Tests.Fixtures.TableCSV;
    using Lib9c.Tests.Fixtures.TableCSV.Stake;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Stake;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Stake;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// This class is used for testing the stake and claim scenario with patching table sheets.
    /// This class considers only AvatarStateV2 not AvatarState.
    /// </summary>
    public class StakeAndClaimScenarioTest
    {
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly IAccount _initialStateWithoutStakePolicySheet;
        private readonly Currency _ncg;

        public StakeAndClaimScenarioTest(ITestOutputHelper output)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(output)
                .CreateLogger();

            var tuple = InitializeUtil.InitializeStates(
                sheetsOverride: new Dictionary<string, string>
                {
                    {
                        nameof(GameConfigSheet),
                        GameConfigSheetFixtures.Default
                    },
                    {
                        "StakeRegularFixedRewardSheet_V1",
                        StakeRegularFixedRewardSheetFixtures.V1
                    },
                    {
                        "StakeRegularFixedRewardSheet_V2",
                        StakeRegularFixedRewardSheetFixtures.V2
                    },
                    {
                        "StakeRegularRewardSheet_V1",
                        StakeRegularRewardSheetFixtures.V1
                    },
                    {
                        "StakeRegularRewardSheet_V2",
                        StakeRegularRewardSheetFixtures.V2
                    },
                    {
                        nameof(StakePolicySheet),
                        StakePolicySheetFixtures.V2
                    },
                });
            _agentAddr = tuple.agentAddr;
            _avatarAddr = tuple.avatarAddr;
            _initialStateWithoutStakePolicySheet = tuple.initialStatesWithAvatarStateV2;
            _ncg = _initialStateWithoutStakePolicySheet.GetGoldCurrency();
        }

        [Fact]
        public void Test()
        {
            // Mint NCG to agent.
            var state = MintAsset(
                _initialStateWithoutStakePolicySheet,
                _agentAddr,
                10_000_000 * _ncg,
                0);

            // Stake 50 NCG via stake2.
            const long stakedAmount = 50;
            const int stake2BlockIndex = 1;
            state = Stake2(state, _agentAddr, stakedAmount, stake2BlockIndex);

            // Validate staked.
            var stakedNCG = stakedAmount * _ncg;
            ValidateStakedState(state, _agentAddr, stakedNCG, stake2BlockIndex);

            // Claim stake reward via claim_stake_reward9.
            state = ClaimStakeReward9(
                state,
                _agentAddr,
                _avatarAddr,
                stake2BlockIndex + StakeState.LockupInterval);

            // Validate staked.
            ValidateStakedStateV2(
                state,
                _agentAddr,
                stakedNCG,
                stake2BlockIndex,
                "StakeRegularFixedRewardSheet_V1",
                "StakeRegularRewardSheet_V1",
                StakeState.RewardInterval,
                StakeState.LockupInterval);

            // Withdraw stake via stake3.
            state = Stake3(state, _agentAddr, 0, stake2BlockIndex + StakeState.LockupInterval + 1);

            // Stake 50 NCG via stake3 before patching.
            const long firstStake3BlockIndex = stake2BlockIndex + StakeState.LockupInterval + 1;
            state = Stake3(
                state,
                _agentAddr,
                stakedAmount,
                firstStake3BlockIndex);

            // Validate staked.
            ValidateStakedStateV2(
                state,
                _agentAddr,
                stakedNCG,
                firstStake3BlockIndex,
                "StakeRegularFixedRewardSheet_V2",
                "StakeRegularRewardSheet_V2",
                50_400,
                201_600);

            // Patch StakePolicySheet and so on.
            state = state.SetState(
                Addresses.GetSheetAddress(nameof(StakePolicySheet)),
                StakePolicySheetFixtures.V3.Serialize());

            // Stake 50 NCG via stake3 after patching.
            state = Stake3(
                state,
                _agentAddr,
                stakedAmount,
                firstStake3BlockIndex + 1);

            // Validate staked.
            ValidateStakedStateV2(
                state,
                _agentAddr,
                stakedNCG,
                firstStake3BlockIndex + 1,
                "StakeRegularFixedRewardSheet_V1",
                "StakeRegularRewardSheet_V1",
                40,
                150);
        }

        private static IAccount MintAsset(
            IAccount state,
            Address recipient,
            FungibleAssetValue amount,
            long blockIndex)
        {
            return state.MintAsset(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = Addresses.Admin,
                    BlockIndex = blockIndex,
                    Rehearsal = false,
                },
                recipient,
                amount);
        }

        private static IAccount Stake2(
            IAccount state,
            Address agentAddr,
            long stakingAmount,
            long blockIndex)
        {
            var stake2 = new Stake2(stakingAmount);
            return stake2.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = agentAddr,
                BlockIndex = blockIndex,
                Rehearsal = false,
            });
        }

        private static IAccount Stake3(
            IAccount state,
            Address agentAddr,
            long stakingAmount,
            long blockIndex)
        {
            var stake3 = new Stake(stakingAmount);
            return stake3.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = agentAddr,
                BlockIndex = blockIndex,
                Rehearsal = false,
            });
        }

        private static IAccount ClaimStakeReward9(
            IAccount state,
            Address agentAddr,
            Address avatarAddr,
            long blockIndex)
        {
            var claimStakingReward9 = new ClaimStakeReward(avatarAddr);
            return claimStakingReward9.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = agentAddr,
                BlockIndex = blockIndex,
                Rehearsal = false,
            });
        }

        private static void ValidateStakedState(
            IAccountState state,
            Address agentAddr,
            FungibleAssetValue expectStakedAmount,
            long expectStartedBlockIndex)
        {
            var stakeAddr = StakeState.DeriveAddress(agentAddr);
            var actualStakedAmount = state.GetBalance(stakeAddr, expectStakedAmount.Currency);
            Assert.Equal(expectStakedAmount, actualStakedAmount);
            var stakeState = new StakeState((Dictionary)state.GetState(stakeAddr));
            Assert.Equal(expectStartedBlockIndex, stakeState.StartedBlockIndex);
        }

        private static void ValidateStakedStateV2(
            IAccountState state,
            Address agentAddr,
            FungibleAssetValue expectStakedAmount,
            long expectStartedBlockIndex,
            string expectStakeRegularFixedRewardSheetName,
            string expectStakeRegularRewardSheetName,
            long expectRewardInterval,
            long expectLockupInterval)
        {
            var stakeAddr = StakeState.DeriveAddress(agentAddr);
            var actualStakedAmount = state.GetBalance(stakeAddr, expectStakedAmount.Currency);
            Assert.Equal(expectStakedAmount, actualStakedAmount);
            var stakeState = new StakeStateV2(state.GetState(stakeAddr));
            Assert.Equal(expectStartedBlockIndex, stakeState.StartedBlockIndex);
            Assert.Equal(
                expectStakeRegularFixedRewardSheetName,
                stakeState.Contract.StakeRegularFixedRewardSheetTableName);
            Assert.Equal(
                expectStakeRegularRewardSheetName,
                stakeState.Contract.StakeRegularRewardSheetTableName);
            Assert.Equal(expectRewardInterval, stakeState.Contract.RewardInterval);
            Assert.Equal(expectLockupInterval, stakeState.Contract.LockupInterval);
        }
    }
}
