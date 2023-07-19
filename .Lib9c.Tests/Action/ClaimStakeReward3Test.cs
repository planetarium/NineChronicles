#nullable enable

namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class ClaimStakeReward3Test
    {
        private const string AgentAddressHex = "0x0000000001000000000100000000010000000001";
        private readonly Address _agentAddr = new Address(AgentAddressHex);
        private readonly Address _avatarAddr;
        private readonly IAccountStateDelta _initialStatesWithAvatarStateV1;
        private readonly IAccountStateDelta _initialStatesWithAvatarStateV2;
        private readonly Currency _ncg;

        public ClaimStakeReward3Test(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
            (
                _,
                _,
                _avatarAddr,
                _initialStatesWithAvatarStateV1,
                _initialStatesWithAvatarStateV2) = InitializeUtil.InitializeStates(
                agentAddr: _agentAddr);
            _ncg = _initialStatesWithAvatarStateV1.GetGoldCurrency();
        }

        [Fact]
        public void Serialization()
        {
            var action = new ClaimStakeReward3(_avatarAddr);
            var deserialized = new ClaimStakeReward3();
            deserialized.LoadPlainValue(action.PlainValue);
            Assert.Equal(action.AvatarAddress, deserialized.AvatarAddress);
        }

        [Theory]
        [InlineData(ClaimStakeReward2.ObsoletedIndex)]
        [InlineData(ClaimStakeReward2.ObsoletedIndex - 1)]
        public void Execute_Throw_ActionUnAvailableException(long blockIndex)
        {
            var action = new ClaimStakeReward3(_avatarAddr);
            Assert.Throws<ActionUnavailableException>(() => action.Execute(new ActionContext
            {
                PreviousState = _initialStatesWithAvatarStateV2,
                Signer = _agentAddr,
                BlockIndex = blockIndex,
            }));
        }

        [Theory]
        [InlineData(
            ClaimStakeReward2.ObsoletedIndex,
            100L,
            null,
            ClaimStakeReward2.ObsoletedIndex + StakeState.LockupInterval,
            40,
            4,
            0
        )]
        [InlineData(
            ClaimStakeReward2.ObsoletedIndex,
            6000L,
            null,
            ClaimStakeReward2.ObsoletedIndex + StakeState.LockupInterval,
            4800,
            36,
            4
        )]
        // Calculate rune start from hard fork index
        [InlineData(
            0L,
            6000L,
            0L,
            ClaimStakeReward2.ObsoletedIndex + StakeState.LockupInterval,
            136800,
            1026,
            4
        )]
        // Stake reward v2
        // Stake before v2, prev. receive v1, receive v1 & v2
        [InlineData(
            StakeState.StakeRewardSheetV2Index - StakeState.RewardInterval * 2,
            50L,
            StakeState.StakeRewardSheetV2Index - StakeState.RewardInterval,
            StakeState.StakeRewardSheetV2Index + 1,
            5,
            1,
            0
        )]
        // Stake before v2, prev. receive v2, receive v2
        [InlineData(
            StakeState.StakeRewardSheetV2Index - StakeState.RewardInterval,
            50L,
            StakeState.StakeRewardSheetV2Index,
            StakeState.StakeRewardSheetV2Index + StakeState.RewardInterval,
            5,
            1,
            0
        )]
        // Stake after v2, no prev. receive, receive v2
        [InlineData(
            StakeState.StakeRewardSheetV2Index,
            6000L,
            null,
            StakeState.StakeRewardSheetV2Index + StakeState.RewardInterval,
            1200,
            9,
            1
        )]
        // stake after v2, prev. receive v2, receive v2
        [InlineData(
            StakeState.StakeRewardSheetV2Index,
            50L,
            StakeState.StakeRewardSheetV2Index + StakeState.RewardInterval,
            StakeState.StakeRewardSheetV2Index + StakeState.RewardInterval * 2,
            5,
            1,
            0
        )]
        // stake before currency as reward, non prev.
        [InlineData(
            StakeState.CurrencyAsRewardStartIndex - StakeState.RewardInterval * 2,
            10_000_000L,
            null,
            StakeState.CurrencyAsRewardStartIndex + StakeState.RewardInterval,
            3_000_000,
            37_506,
            4_998
        )]
        // stake before currency as reward, prev.
        [InlineData(
            StakeState.CurrencyAsRewardStartIndex - StakeState.RewardInterval * 2,
            10_000_000L,
            StakeState.CurrencyAsRewardStartIndex - StakeState.RewardInterval,
            StakeState.CurrencyAsRewardStartIndex + StakeState.RewardInterval,
            2_000_000,
            25_004,
            3_332
        )]
        public void Execute_Success(
            long startedBlockIndex,
            long stakeAmount,
            long? previousRewardReceiveIndex,
            long blockIndex,
            int expectedHourglass,
            int expectedApStone,
            int expectedRune)
        {
            Execute(
                _initialStatesWithAvatarStateV1,
                _agentAddr,
                _avatarAddr,
                startedBlockIndex,
                stakeAmount,
                previousRewardReceiveIndex,
                blockIndex,
                expectedHourglass,
                expectedApStone,
                expectedRune);

            Execute(
                _initialStatesWithAvatarStateV2,
                _agentAddr,
                _avatarAddr,
                startedBlockIndex,
                stakeAmount,
                previousRewardReceiveIndex,
                blockIndex,
                expectedHourglass,
                expectedApStone,
                expectedRune);
        }

        private void Execute(
            IAccountStateDelta prevState,
            Address agentAddr,
            Address avatarAddr,
            long startedBlockIndex,
            long stakeAmount,
            long? previousRewardReceiveIndex,
            long blockIndex,
            int expectedHourglass,
            int expectedApStone,
            int expectedRune)
        {
            var context = new ActionContext();
            var stakeStateAddr = StakeState.DeriveAddress(agentAddr);
            var initialStakeState = new StakeState(stakeStateAddr, startedBlockIndex);
            if (!(previousRewardReceiveIndex is null))
            {
                initialStakeState.Claim((long)previousRewardReceiveIndex);
            }

            prevState = prevState
                .SetState(stakeStateAddr, initialStakeState.Serialize())
                .MintAsset(context, stakeStateAddr, _ncg * stakeAmount);

            var action = new ClaimStakeReward3(avatarAddr);
            var states = action.Execute(new ActionContext
            {
                PreviousState = prevState,
                Signer = agentAddr,
                BlockIndex = blockIndex,
            });

            AvatarState avatarState = states.GetAvatarStateV2(avatarAddr);
            Assert.Equal(
                expectedHourglass,
                avatarState.inventory.Items.First(x => x.item.Id == 400000).count);
            // It must be never added into the inventory if the amount is 0.
            Assert.Equal(
                expectedApStone,
                avatarState.inventory.Items.First(x => x.item.Id == 500000).count);
            Assert.Equal(
                expectedRune * RuneHelper.StakeRune,
                states.GetBalance(avatarAddr, RuneHelper.StakeRune));

            Assert.True(states.TryGetStakeState(agentAddr, out StakeState stakeState));
            Assert.Equal(blockIndex, stakeState.ReceivedBlockIndex);
        }
    }
}
