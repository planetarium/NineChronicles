#define TEST_9c

namespace Lib9c.Tests.Action
{
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Factory;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class FaucetTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Currency _ncg;
        private readonly Currency _crystal;
        private readonly Address _agentAddress;

        public FaucetTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new State();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState =
                    _initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var tableSheets = new TableSheets(sheets);
            _ncg = Currency.Legacy("NCG", 2, null);
            _crystal = Currency.Legacy("CRYSTAL", 18, null);
            var goldCurrencyState = new GoldCurrencyState(_ncg);
            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);

            _initialState = _initialState
                    .SetState(_agentAddress, agentState.Serialize())
                    .SetState(GoldCurrencyState.Address, goldCurrencyState.Serialize())
                ;
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(10, 0, 10, 0)]
        [InlineData(0, 10, 0, 10)]
        [InlineData(10, 10, 10, 10)]
        [InlineData(-10, 0, 0, 0)]
        [InlineData(0, -10, 0, 0)]
        public void Execute_Faucet(int faucetNcg, int faucetCrystal, int expectedNcg, int expectedCrystal)
        {
            var action = FaucetFactory.CreateFaucet(_agentAddress, faucetNcg, faucetCrystal);
            var state = action.Execute(new ActionContext { PreviousStates = _initialState });
            AgentState agentState = state.GetAgentState(_agentAddress);
            FungibleAssetValue expectedNcgAsset =
                new FungibleAssetValue(_ncg, expectedNcg, 0);
            FungibleAssetValue ncg = state.GetBalance(_agentAddress, state.GetGoldCurrency());
            Assert.Equal(expectedNcgAsset, ncg);

            FungibleAssetValue expectedCrystalAsset =
                new FungibleAssetValue(_crystal, expectedCrystal, 0);
            FungibleAssetValue crystal = state.GetBalance(_agentAddress, _crystal);
            Assert.Equal(expectedCrystalAsset, crystal);
        }
    }
}
