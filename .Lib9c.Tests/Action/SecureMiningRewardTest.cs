namespace Lib9c.Tests.Action
{
    using System.Collections.Immutable;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class SecureMiningRewardTest
    {
        private static readonly Currency NCG = SecureMiningReward.NCG;

        // Just for sake of checking, chosen arbitrary addresses statically.
        private static readonly Address _admin =
            new Address("0x765781BB7B4FA2598Cb09383EBb9bEe8b1aE10bF");

        private static readonly Address _recipient =
            new Address("0xC63c1dDCeB5054015bC01dE352b42234d4a25be5");

        private static readonly Address _treasury =
            new Address("0xB3bCa3b3c6069EF5Bdd6384bAD98F11378Dc360E");

        private static readonly ImmutableList<Address> _authMiners = new[]
        {
            new Address("ab1dce17dCE1Db1424BB833Af6cC087cd4F5CB6d"),
            new Address("3217f757064Cd91CAba40a8eF3851F4a9e5b4985"),
            new Address("474CB59Dea21159CeFcC828b30a8D864e0b94a6B"),
            new Address("636d187B4d434244A92B65B06B5e7da14b3810A9"),
        }.ToImmutableList();

        private static readonly State _previousState = new State(
            state: ImmutableDictionary<Address, IValue>.Empty
                .Add(AdminState.Address, new AdminState(_admin, 100).Serialize())
                .Add(GoldCurrencyState.Address, new GoldCurrencyState(NCG).Serialize()),
            balance: ImmutableDictionary<(Address, Currency), FungibleAssetValue>.Empty
                .Add((_authMiners[0], NCG), NCG * 1000)
                .Add((_authMiners[1], NCG), NCG * 2000)
                .Add((_authMiners[2], NCG), NCG * 3000)
                .Add((_authMiners[3], NCG), NCG * 4000)
        );

        [Fact]
        public void Execute()
        {
            var action = new SecureMiningReward(recipient: _recipient);
            IAccountStateDelta nextState = action.Execute(
                new ActionContext
                {
                    PreviousStates = _previousState,
                    Signer = _admin,
                    Rehearsal = false,
                    BlockIndex = 1,
                }
            );

            Assert.Equal(NCG * 0, nextState.GetBalance(_authMiners[0], NCG));
            Assert.Equal(NCG * 0, nextState.GetBalance(_authMiners[1], NCG));
            Assert.Equal(NCG * 0, nextState.GetBalance(_authMiners[2], NCG));
            Assert.Equal(NCG * 0, nextState.GetBalance(_authMiners[3], NCG));

            // (1000 + 2000 + 3000 + 4000) * 0.2
            Assert.Equal(NCG * 2000, nextState.GetBalance(_recipient, NCG));

            // (1000 + 2000 + 3000 + 4000) * 0.4
            Assert.Equal(NCG * 4000, nextState.GetBalance(_treasury, NCG));
        }

        [Fact]
        public void Execute_InvalidSigner()
        {
            var invalidSigner = new Address("0x94cde435616875310f0739FAf2c8671c58987bf0");
            var action = new SecureMiningReward(recipient: _recipient);
            Assert.Throws<PermissionDeniedException>(() => action.Execute(
                new ActionContext
                {
                    PreviousStates = _previousState,
                    Signer = invalidSigner,
                    Rehearsal = false,
                    BlockIndex = 1,
                }
            ));
        }
    }
}
