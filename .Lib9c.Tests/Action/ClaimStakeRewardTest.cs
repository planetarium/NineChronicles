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

    public class ClaimStakeRewardTest
    {
        private const string AgentAddressHex = "0x0000000001000000000100000000010000000001";

        // VALUE: 6_692_400L
        // - receive v1 reward * 1
        // - receive v2(w/o currency) reward * 4
        // - receive v2(w/ currency) reward * 14
        // - receive v3 reward * n
        private const long BlockIndexForTest =
            StakeState.StakeRewardSheetV3Index -
            ((StakeState.StakeRewardSheetV3Index - StakeState.StakeRewardSheetV2Index) / StakeState.RewardInterval + 1) *
            StakeState.RewardInterval;

        private readonly Address _agentAddr = new Address(AgentAddressHex);
        private readonly Address _avatarAddr;
        private readonly IAccount _initialStatesWithAvatarStateV1;
        private readonly IAccount _initialStatesWithAvatarStateV2;
        private readonly Currency _ncg;

        public ClaimStakeRewardTest(ITestOutputHelper outputHelper)
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
            var action = new ClaimStakeReward(_avatarAddr);
            var deserialized = new ClaimStakeReward();
            deserialized.LoadPlainValue(action.PlainValue);
            Assert.Equal(action.AvatarAddress, deserialized.AvatarAddress);
        }

        [Theory]
        [InlineData(
            ClaimStakeReward2.ObsoletedIndex,
            100L,
            null,
            ClaimStakeReward2.ObsoletedIndex + StakeState.LockupInterval,
            40,
            4,
            0,
            null,
            null,
            0L
        )]
        [InlineData(
            ClaimStakeReward2.ObsoletedIndex,
            6000L,
            null,
            ClaimStakeReward2.ObsoletedIndex + StakeState.LockupInterval,
            4800,
            36,
            4,
            null,
            null,
            0L
        )]
        // Calculate rune start from hard fork index
        [InlineData(
            0L,
            6000L,
            0L,
            ClaimStakeReward2.ObsoletedIndex + StakeState.LockupInterval,
            136800,
            1026,
            3,
            null,
            null,
            0L
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
            0,
            null,
            null,
            0L
        )]
        // Stake before v2, prev. receive v2, receive v2
        [InlineData(
            StakeState.StakeRewardSheetV2Index - StakeState.RewardInterval,
            50L,
            StakeState.StakeRewardSheetV2Index,
            StakeState.StakeRewardSheetV2Index + StakeState.RewardInterval,
            5,
            1,
            0,
            null,
            null,
            0L
        )]
        // Stake after v2, no prev. receive, receive v2
        [InlineData(
            StakeState.StakeRewardSheetV2Index,
            6000L,
            null,
            StakeState.StakeRewardSheetV2Index + StakeState.RewardInterval,
            3000,
            17,
            1,
            null,
            null,
            0L
        )]
        // stake after v2, prev. receive v2, receive v2
        [InlineData(
            StakeState.StakeRewardSheetV2Index,
            50L,
            StakeState.StakeRewardSheetV2Index + StakeState.RewardInterval,
            StakeState.StakeRewardSheetV2Index + StakeState.RewardInterval * 2,
            5,
            1,
            0,
            null,
            null,
            0L
        )]
        // stake before currency as reward, non prev.
        // receive v2(w/o currency) * 2, receive v2(w/ currency). check GARAGE.
        [InlineData(
            StakeState.CurrencyAsRewardStartIndex - StakeState.RewardInterval * 2,
            10_000_000L,
            null,
            StakeState.CurrencyAsRewardStartIndex + StakeState.RewardInterval,
            15_000_000,
            75_006,
            4_998,
            AgentAddressHex,
            "GARAGE",
            100_000L
        )]
        // stake before currency as reward, prev.
        // receive v2(w/o currency), receive v2(w/ currency). check GARAGE.
        [InlineData(
            StakeState.CurrencyAsRewardStartIndex - StakeState.RewardInterval * 2,
            10_000_000L,
            StakeState.CurrencyAsRewardStartIndex - StakeState.RewardInterval,
            StakeState.CurrencyAsRewardStartIndex + StakeState.RewardInterval,
            10_000_000,
            50_004,
            3_332,
            AgentAddressHex,
            "GARAGE",
            100_000L
        )]
        // stake before v3(crystal), non prev. receive v2. check CRYSTAL.
        [InlineData(
            StakeState.StakeRewardSheetV3Index - 1,
            500L,
            null,
            StakeState.StakeRewardSheetV3Index - 1 + StakeState.RewardInterval,
            125,
            2,
            0,
            AgentAddressHex,
            "CRYSTAL",
            0L
        )]
        // stake after v3(crystal), non prev. receive v3. check CRYSTAL.
        [InlineData(
            StakeState.StakeRewardSheetV3Index,
            500L,
            null,
            StakeState.StakeRewardSheetV3Index + StakeState.RewardInterval,
            125,
            2,
            0,
            AgentAddressHex,
            "CRYSTAL",
            5_000L
        )]
        // stake before v3(crystal), non prev. receive v2 * 2, receive v3. check CRYSTAL.
        [InlineData(
            StakeState.StakeRewardSheetV3Index - StakeState.RewardInterval * 2,
            10_000_000L,
            null,
            StakeState.StakeRewardSheetV3Index + StakeState.RewardInterval,
            35_000_000,
            175_006,
            11_665,
            AgentAddressHex,
            "CRYSTAL",
            1_000_000_000L
        )]
        // stake before v3(crystal), prev. receive v2, receive v3. check CRYSTAL.
        [InlineData(
            StakeState.StakeRewardSheetV3Index - StakeState.RewardInterval * 2,
            10_000_000L,
            StakeState.StakeRewardSheetV3Index - StakeState.RewardInterval,
            StakeState.StakeRewardSheetV3Index + StakeState.RewardInterval,
            30_000_000,
            150_004,
            9_999,
            AgentAddressHex,
            "CRYSTAL",
            1_000_000_000L
        )]
        // stake after v3(crystal), non prev. receive v2 * 2, receive v3. check CRYSTAL.
        [InlineData(
            StakeState.StakeRewardSheetV3Index,
            10_000_000L,
            null,
            StakeState.StakeRewardSheetV3Index + StakeState.RewardInterval * 3,
            75_000_000,
            375_006,
            24_999,
            AgentAddressHex,
            "CRYSTAL",
            3_000_000_000L
        )]
        // stake after v3(crystal), prev. receive v2, receive v3. check CRYSTAL.
        [InlineData(
            StakeState.StakeRewardSheetV3Index,
            10_000_000L,
            StakeState.StakeRewardSheetV3Index + StakeState.RewardInterval,
            StakeState.StakeRewardSheetV3Index + StakeState.RewardInterval * 3,
            50_000_000,
            250_004,
            16_666,
            AgentAddressHex,
            "CRYSTAL",
            2_000_000_000L
        )]
        // stake before v2(w/o currency), non prev.
        // receive v1.
        [InlineData(
            BlockIndexForTest,
            500L,
            null,
            BlockIndexForTest + StakeState.RewardInterval,
            62,
            2,
            0,
            null,
            null,
            0L
        )]
        // stake before v2(w/o currency), non prev.
        // receive v1, do not receive v2(w/o currency).
        [InlineData(
            BlockIndexForTest,
            500L,
            null,
            StakeState.StakeRewardSheetV2Index + StakeState.RewardInterval - 1,
            62,
            2,
            0,
            null,
            null,
            0L
        )]
        // stake before v2(w/o currency), non prev.
        // receive v1, receive v2(w/o currency).
        [InlineData(
            BlockIndexForTest,
            500L,
            null,
            StakeState.StakeRewardSheetV2Index + StakeState.RewardInterval * 2 - 1,
            187,
            4,
            0,
            null,
            null,
            0L
        )]
        // stake before v2(w/o currency), non prev.
        // receive v1, receive v2(w/o currency) * 3, do not receive v2(w/ currency).
        [InlineData(
            BlockIndexForTest,
            500L,
            null,
            StakeState.CurrencyAsRewardStartIndex + StakeState.RewardInterval - 1,
            562,
            10,
            0,
            null,
            null,
            0L
        )]
        // stake before v2(w/o currency), non prev.
        // receive v1, receive v2(w/o currency) * 3, receive v2(w/ currency).
        // check GARAGE is 0 when stake 500.
        [InlineData(
            BlockIndexForTest,
            500L,
            null,
            StakeState.CurrencyAsRewardStartIndex + StakeState.RewardInterval * 2 - 1,
            687,
            12,
            0,
            AgentAddressHex,
            "GARAGE",
            0L
        )]
        // stake before v2(w/o currency), non prev.
        // receive v1, receive v2(w/o currency) * 3, receive v2(w/ currency).
        // check GARAGE is 100,000 when stake 10,000,000.
        [InlineData(
            BlockIndexForTest,
            10_000_000L,
            null,
            StakeState.CurrencyAsRewardStartIndex + StakeState.RewardInterval * 2 - 1,
            27_000_000,
            137_512,
            9_996,
            AgentAddressHex,
            "GARAGE",
            100_000L
        )]
        // stake before v2(w/o currency), non prev.
        // receive v1, receive v2(w/o currency) * 3, receive v2(w/ currency) * ???, no receive v3.
        // check CRYSTAL is 0.
        [InlineData(
            BlockIndexForTest,
            500L,
            null,
            StakeState.StakeRewardSheetV3Index + StakeState.RewardInterval - 1,
            2_312,
            38,
            0,
            AgentAddressHex,
            "CRYSTAL",
            0L
        )]
        // stake before v2(w/o currency), non prev.
        // receive v1, receive v2(w/o currency) * 3, receive v2(w/ currency) * ???, receive v3.
        // check CRYSTAL is ???.
        [InlineData(
            BlockIndexForTest,
            500L,
            null,
            StakeState.StakeRewardSheetV3Index + StakeState.RewardInterval * 2 - 1,
            2_437,
            40,
            0,
            AgentAddressHex,
            "CRYSTAL",
            5_000L
        )]
        public void Execute_Success(
            long startedBlockIndex,
            long stakeAmount,
            long? previousRewardReceiveIndex,
            long blockIndex,
            int expectedHourglass,
            int expectedApStone,
            int expectedRune,
            string expectedCurrencyAddrHex,
            string expectedCurrencyTicker,
            long expectedCurrencyAmount)
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
                expectedRune,
                expectedCurrencyAddrHex,
                expectedCurrencyTicker,
                expectedCurrencyAmount);

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
                expectedRune,
                expectedCurrencyAddrHex,
                expectedCurrencyTicker,
                expectedCurrencyAmount);
        }

        private void Execute(
            IAccount prevState,
            Address agentAddr,
            Address avatarAddr,
            long startedBlockIndex,
            long stakeAmount,
            long? previousRewardReceiveIndex,
            long blockIndex,
            int expectedHourglass,
            int expectedApStone,
            int expectedRune,
            string expectedCurrencyAddrHex,
            string expectedCurrencyTicker,
            long expectedCurrencyAmount)
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

            var action = new ClaimStakeReward(avatarAddr);
            var states = action.Execute(new ActionContext
            {
                PreviousState = prevState,
                Signer = agentAddr,
                BlockIndex = blockIndex,
            });

            var avatarState = states.GetAvatarStateV2(avatarAddr);
            if (expectedHourglass > 0)
            {
                Assert.Equal(
                    expectedHourglass,
                    avatarState.inventory.Items.First(x => x.item.Id == 400000).count);
            }
            else
            {
                Assert.DoesNotContain(avatarState.inventory.Items, x => x.item.Id == 400000);
            }

            if (expectedApStone > 0)
            {
                Assert.Equal(
                    expectedApStone,
                    avatarState.inventory.Items.First(x => x.item.Id == 500000).count);
            }
            else
            {
                Assert.DoesNotContain(avatarState.inventory.Items, x => x.item.Id == 500000);
            }

            if (expectedRune > 0)
            {
                Assert.Equal(
                    expectedRune * RuneHelper.StakeRune,
                    states.GetBalance(avatarAddr, RuneHelper.StakeRune));
            }
            else
            {
                Assert.Equal(
                    0 * RuneHelper.StakeRune,
                    states.GetBalance(avatarAddr, RuneHelper.StakeRune));
            }

            if (!string.IsNullOrEmpty(expectedCurrencyAddrHex))
            {
                var addr = new Address(expectedCurrencyAddrHex);
                var currency = Currencies.GetMinterlessCurrency(expectedCurrencyTicker);
                Assert.Equal(
                    expectedCurrencyAmount * currency,
                    states.GetBalance(addr, currency));
            }

            Assert.True(states.TryGetStakeState(agentAddr, out StakeState stakeState));
            Assert.Equal(blockIndex, stakeState.ReceivedBlockIndex);
        }
    }
}
