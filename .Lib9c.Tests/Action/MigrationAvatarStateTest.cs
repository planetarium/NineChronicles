namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class MigrationAvatarStateTest
    {
        private readonly TableSheets _tableSheets;

        public MigrationAvatarStateTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Execute()
        {
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var admin = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            var state = new State(ImmutableDictionary<Address, IValue>.Empty
                .Add(AdminState.Address, new AdminState(admin, 100).Serialize())
                .Add(avatarAddress, avatarState.SerializeV2())
            );

            var action = new MigrationAvatarState
            {
                avatarStates = new List<Dictionary>
                {
                    (Dictionary)avatarState.Serialize(),
                },
            };

            IAccountStateDelta nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = admin,
                BlockIndex = 1,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(avatarAddress);
            Assert.NotNull(nextAvatarState.inventory);
            Assert.NotNull(nextAvatarState.worldInformation);
            Assert.NotNull(nextAvatarState.questList);
        }
    }
}
