namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class ClaimStakeReward3Test
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Currency _currency;
        private readonly Address _signerAddress;
        private readonly Address _avatarAddress;
        private readonly Address _avatarAddressForBackwardCompatibility;
        private readonly Address _stakeStateAddress;

        public ClaimStakeReward3Test(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new State();

            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var tableSheets = new TableSheets(sheets);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(_currency);

            _signerAddress = new PrivateKey().ToAddress();
            _stakeStateAddress = StakeState.DeriveAddress(_signerAddress);
            var agentState = new AgentState(_signerAddress);
            _avatarAddress = _signerAddress.Derive("0");
            agentState.avatarAddresses.Add(0, _avatarAddress);
            var avatarState = new AvatarState(
                _avatarAddress,
                _signerAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(sheets[nameof(GameConfigSheet)]),
                new PrivateKey().ToAddress()
            )
            {
                level = 100,
            };

            _avatarAddressForBackwardCompatibility = _signerAddress.Derive("1");
            agentState.avatarAddresses.Add(1, _avatarAddressForBackwardCompatibility);
            var avatarStateForBackwardCompatibility = new AvatarState(
                _avatarAddressForBackwardCompatibility,
                _signerAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(sheets[nameof(GameConfigSheet)]),
                new PrivateKey().ToAddress()
            )
            {
                level = 100,
            };

            _initialState = _initialState
                .SetState(_signerAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(
                    _avatarAddress.Derive(LegacyInventoryKey),
                    avatarState.inventory.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyQuestListKey),
                    avatarState.questList.Serialize())
                .SetState(
                    _avatarAddressForBackwardCompatibility,
                    avatarStateForBackwardCompatibility.Serialize())
                .SetState(GoldCurrencyState.Address, goldCurrencyState.Serialize());
        }

        [Fact]
        public void Serialization()
        {
            var action = new ClaimStakeReward(_avatarAddress);
            var deserialized = new ClaimStakeReward();
            deserialized.LoadPlainValue(action.PlainValue);
            Assert.Equal(action.AvatarAddress, deserialized.AvatarAddress);
        }

        [Theory]
        [InlineData(ClaimStakeReward2.ObsoletedIndex)]
        [InlineData(ClaimStakeReward2.ObsoletedIndex - 1)]
        public void Execute_Throw_ActionUnAvailableException(long blockIndex)
        {
            var action = new ClaimStakeReward(_avatarAddress);
            Assert.Throws<ActionUnavailableException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Signer = _signerAddress,
                BlockIndex = blockIndex,
            }));
        }

        [Theory]
        [InlineData(
            ClaimStakeReward2.ObsoletedIndex,
            100,
            null,
            ClaimStakeReward2.ObsoletedIndex + StakeState.LockupInterval,
            40,
            4,
            0
        )]
        [InlineData(
            ClaimStakeReward2.ObsoletedIndex,
            6000,
            null,
            ClaimStakeReward2.ObsoletedIndex + StakeState.LockupInterval,
            4800,
            36,
            4
        )]
        // Calculate rune start from hard fork index
        [InlineData(
            0L,
            6000,
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
            50,
            StakeState.StakeRewardSheetV2Index - StakeState.RewardInterval,
            StakeState.StakeRewardSheetV2Index + 1,
            5,
            1,
            0
        )]
        // Stake before v2, prev. receive v2, receive v2
        [InlineData(
            StakeState.StakeRewardSheetV2Index - StakeState.RewardInterval,
            50,
            StakeState.StakeRewardSheetV2Index,
            StakeState.StakeRewardSheetV2Index + StakeState.RewardInterval,
            5,
            1,
            0
        )]
        // Stake after v2, no prev. receive, receive v2
        [InlineData(
            StakeState.StakeRewardSheetV2Index,
            6000,
            null,
            StakeState.StakeRewardSheetV2Index + StakeState.RewardInterval,
            1200,
            9,
            1
        )]
        // stake after v2, prev. receive v2, receive v2
        [InlineData(
            StakeState.StakeRewardSheetV2Index,
            50,
            StakeState.StakeRewardSheetV2Index + StakeState.RewardInterval,
            StakeState.StakeRewardSheetV2Index + StakeState.RewardInterval * 2,
            5,
            1,
            0
        )]
        public void Execute_Success(
            long startedBlockIndex,
            int stakeAmount,
            long? previousRewardReceiveIndex,
            long blockIndex,
            int expectedHourglass,
            int expectedApStone,
            int expectedRune)
        {
            Execute(
                _avatarAddress,
                startedBlockIndex,
                stakeAmount,
                previousRewardReceiveIndex,
                blockIndex,
                expectedHourglass,
                expectedApStone,
                expectedRune);
        }

        [Fact]
        public void Execute_With_Old_AvatarState_Success()
        {
            Execute(_avatarAddressForBackwardCompatibility, ClaimStakeReward2.ObsoletedIndex, 100, null, ClaimStakeReward2.ObsoletedIndex + StakeState.LockupInterval, 40, 4, 0);
        }

        private void Execute(Address avatarAddress, long startedBlockIndex, int stakeAmount, long? previousRewardReceiveIndex, long blockIndex, int expectedHourglass, int expectedApStone, int expectedRune)
        {
            var state = _initialState;
            var initialStakeState = new StakeState(_stakeStateAddress, startedBlockIndex);
            if (!(previousRewardReceiveIndex is null))
            {
                initialStakeState.Claim((long)previousRewardReceiveIndex);
            }

            state = state
                    .SetState(_stakeStateAddress, initialStakeState.Serialize())
                    .MintAsset(_stakeStateAddress, _currency * stakeAmount);

            var action = new ClaimStakeReward(avatarAddress);
            var states = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _signerAddress,
                BlockIndex = blockIndex,
            });

            AvatarState avatarState = states.GetAvatarStateV2(avatarAddress);
            Assert.Equal(expectedHourglass, avatarState.inventory.Items.First(x => x.item.Id == 400000).count);
            // It must be never added into the inventory if the amount is 0.
            Assert.Equal(expectedApStone, avatarState.inventory.Items.First(x => x.item.Id == 500000).count);
            Assert.Equal(expectedRune * RuneHelper.StakeRune, states.GetBalance(avatarAddress, RuneHelper.StakeRune));

            Assert.True(states.TryGetStakeState(_signerAddress, out StakeState stakeState));
            Assert.Equal(blockIndex, stakeState.ReceivedBlockIndex);
        }
    }
}
