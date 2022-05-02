namespace Lib9c.Tests.Action.Scenario
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
    using State = Lib9c.Tests.Action.State;

    public class StakeAndClaimStakeRewardScenarioTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Currency _currency;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly TableSheets _tableSheets;
        private readonly Address _signerAddress;
        private readonly Address _avatarAddress;

        public StakeAndClaimStakeRewardScenarioTest(ITestOutputHelper outputHelper)
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
                .MintAsset(_signerAddress, _currency * 100);
        }

        [Fact]
        public void StakeAndClaimStakeReward()
        {
            IAction action = new Stake(100);
            var states = action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Signer = _signerAddress,
                BlockIndex = 0,
            });

            action = new ClaimStakeReward(_avatarAddress);
            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = StakeState.RewardInterval,
            });

            var avatarState = states.GetAvatarStateV2(_avatarAddress);
            // regular (100 / 50)
            Assert.Equal(2, avatarState.inventory.Items.First(x => x.item.Id == 400000).count);
            // regular (100 / 50)
            Assert.Equal(2, avatarState.inventory.Items.First(x => x.item.Id == 500000).count);
        }

        [Fact]
        public void StakeAndClaimStakeRewardBeforeRewardInterval()
        {
            IAction action = new Stake(100);
            var states = action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Signer = _signerAddress,
                BlockIndex = 0,
            });

            action = new ClaimStakeReward(_avatarAddress);
            Assert.Throws<RequiredBlockIndexException>(() => states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = StakeState.RewardInterval - 1,
            }));

            var avatarState = states.GetAvatarStateV2(_avatarAddress);
            Assert.Empty(avatarState.inventory.Items.Where(x => x.item.Id == 400000));
            Assert.Empty(avatarState.inventory.Items.Where(x => x.item.Id == 500000));
        }
    }
}
