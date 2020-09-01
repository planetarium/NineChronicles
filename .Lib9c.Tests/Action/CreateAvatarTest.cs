namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class CreateAvatarTest
    {
        [Fact]
        public void Execute()
        {
            var agentAddress = default(Address);
            var avatarAddress = agentAddress.Derive("avatar");

            var action = new CreateAvatar()
            {
                avatarAddress = avatarAddress,
                index = 0,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = "test",
            };

            var gold = new GoldCurrencyState(new Currency("NCG", 2, minter: null));

            var sheets = TableSheetsImporter.ImportSheets();
            var state = new State()
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .SetState(
                    Addresses.GoldDistribution,
                    GoldDistributionTest.Fixture.Select(v => v.Serialize()).Serialize()
                )
                .SetState(
                    Addresses.GameConfig,
                    new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize()
                )
                .MintAsset(GoldCurrencyState.Address, gold.Currency * 100000000000);

            foreach (var (key, value) in sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), Dictionary.Empty.Add("csv", value));
            }

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = agentAddress,
                BlockIndex = 0,
            });

            Assert.Equal(
                CreateAvatar.InitialGoldBalance,
                nextState.GetBalance(default, gold.Currency).MajorUnit
            );
            Assert.True(nextState.TryGetAgentAvatarStates(
                default,
                avatarAddress,
                out var agentState,
                out var nextAvatarState)
            );
            Assert.True(agentState.avatarAddresses.Any());
            Assert.Equal("test", nextAvatarState.name);
        }
    }
}
