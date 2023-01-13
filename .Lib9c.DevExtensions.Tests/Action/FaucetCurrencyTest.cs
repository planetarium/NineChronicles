using System.Collections.Immutable;
using Lib9c.DevExtensions.Action;
using Lib9c.Tests.Action;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Lib9c.DevExtensions.Tests.Action
{
    public class FaucetCurrencyTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Address _agentAddress;
        private readonly Currency _ncg;
        private readonly Currency _crystal;

        public FaucetCurrencyTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

#pragma warning disable CS0618
            _ncg = Currency.Legacy("NCG", 2, null);
            _crystal = Currency.Legacy("CRYSTAL", 18, null);
#pragma warning restore CS0618

            var balance =
                ImmutableDictionary<(Address Address, Currency Currency), FungibleAssetValue>.Empty
                    .Add((GoldCurrencyState.Address, _ncg), _ncg * int.MaxValue);
            _initialState = new Lib9c.Tests.Action.State(balance: balance);

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
        public void Execute_FaucetCurrency(
            int faucetNcg,
            int faucetCrystal,
            int expectedNcg,
            int expectedCrystal
        )
        {
            var action = new FaucetCurrency
            {
                AgentAddress = _agentAddress,
                FaucetNcg = faucetNcg,
                FaucetCrystal = faucetCrystal,
            };
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
