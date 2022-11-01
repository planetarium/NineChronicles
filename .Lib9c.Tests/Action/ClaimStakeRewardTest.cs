namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class ClaimStakeRewardTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Currency _currency;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly TableSheets _tableSheets;
        private readonly Address _signerAddress;
        private readonly Address _avatarAddress;
        private readonly Address _avatarAddressForBackwardCompatibility;

        public ClaimStakeRewardTest(ITestOutputHelper outputHelper)
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

            _tableSheets = new TableSheets(sheets);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            _goldCurrencyState = new GoldCurrencyState(_currency);

            _signerAddress = new PrivateKey().ToAddress();
            var stakeStateAddress = StakeState.DeriveAddress(_signerAddress);
            var agentState = new AgentState(_signerAddress);
            _avatarAddress = _signerAddress.Derive("0");
            agentState.avatarAddresses.Add(0, _avatarAddress);
            var avatarState = new AvatarState(
                _avatarAddress,
                _signerAddress,
                0,
                _tableSheets.GetAvatarSheets(),
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
                _tableSheets.GetAvatarSheets(),
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
                .SetState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .SetState(stakeStateAddress, new StakeState(stakeStateAddress, 0).Serialize())
                .MintAsset(stakeStateAddress, _currency * 100);
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
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_Success(bool useOldTable)
        {
            Execute(_avatarAddress, useOldTable);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_With_Old_AvatarState_Success(bool useOldTable)
        {
            Execute(_avatarAddressForBackwardCompatibility, useOldTable);
        }

        [Fact]
        public void Execute_Throw_ActionObsoletedException()
        {
            var action = new ClaimStakeReward(_avatarAddress);
            Assert.Throws<ActionObsoletedException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Signer = _signerAddress,
                BlockIndex = ClaimStakeReward.ObsoletedIndex + 1,
            }));
        }

        private void Execute(Address avatarAddress, bool useOldTable)
        {
            var state = _initialState;
            if (useOldTable)
            {
                var sheet = @"level,required_gold,item_id,rate
1,50,400000,10
1,50,500000,800
2,500,400000,8
2,500,500000,800
3,5000,400000,5
3,5000,500000,800
4,50000,400000,5
4,50000,500000,800
5,500000,400000,5
5,500000,500000,800".Serialize();
                state = state.SetState(Addresses.GetSheetAddress<StakeRegularRewardSheet>(), sheet);
            }

            var action = new ClaimStakeReward(avatarAddress);
            var states = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _signerAddress,
                BlockIndex = StakeState.LockupInterval,
            });

            AvatarState avatarState = states.GetAvatarStateV2(avatarAddress);
            // regular (100 / 10) * 4
            Assert.Equal(40, avatarState.inventory.Items.First(x => x.item.Id == 400000).count);
            // regular ((100 / 800) + 1) * 4
            // It must be never added into the inventory if the amount is 0.
            Assert.Equal(4, avatarState.inventory.Items.First(x => x.item.Id == 500000).count);

            Assert.True(states.TryGetStakeState(_signerAddress, out StakeState stakeState));
            Assert.Equal(StakeState.LockupInterval, stakeState.ReceivedBlockIndex);
        }
    }
}
