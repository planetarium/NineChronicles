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
    using static SerializeKeys;

    public class ClaimStakeRewardTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Currency _currency;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly TableSheets _tableSheets;
        private readonly Address _signerAddress;
        private readonly Address _avatarAddress;

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

            _currency = new Currency("NCG", 2, minters: null);
            _goldCurrencyState = new GoldCurrencyState(_currency);

            _signerAddress = new PrivateKey().ToAddress();
            var stakeStateAddress = StakeState.DeriveAddress(_signerAddress);
            var agentState = new AgentState(_signerAddress);
            _avatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = _avatarAddress.Derive("ranking_map");
            agentState.avatarAddresses.Add(0, _avatarAddress);
            var avatarState = new AvatarState(
                _avatarAddress,
                _signerAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(sheets[nameof(GameConfigSheet)]),
                rankingMapAddress
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
                .SetState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .SetState(stakeStateAddress, new StakeState(stakeStateAddress, 0).Serialize())
                .MintAsset(stakeStateAddress, _currency * 100);
        }

        [Fact]
        public void Execute()
        {
            var action = new ClaimStakeReward(_avatarAddress);
            var states = action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Signer = _signerAddress,
                BlockIndex = StakeState.LockupInterval,
            });

            AvatarState avatarState = states.GetAvatarStateV2(_avatarAddress);
            // regular (100 / 8) * 4
            Assert.Equal(48, avatarState.inventory.Items.First(x => x.item.Id == 400000).count);
            // regular (100 / 200) * 4
            // It must be never added into the inventory if the amount is 0.
            Assert.Null(avatarState.inventory.Items.FirstOrDefault(x => x.item.Id == 500000));

            Assert.True(states.TryGetStakeState(_signerAddress, out StakeState stakeState));
            Assert.Equal(StakeState.LockupInterval, stakeState.ReceivedBlockIndex);
        }

        [Fact]
        public void Serialization()
        {
            var action = new ClaimStakeReward(_avatarAddress);
            var deserialized = new ClaimStakeReward();
            deserialized.LoadPlainValue(action.PlainValue);
            Assert.Equal(action.AvatarAddress, deserialized.AvatarAddress);
        }
    }
}
