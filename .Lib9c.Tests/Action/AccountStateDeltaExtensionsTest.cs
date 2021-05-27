namespace Lib9c.Tests.Action
{
    using Bencodex.Types;
    using Libplanet;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;
    using static SerializeKeys;

    public class AccountStateDeltaExtensionsTest
    {
        [Fact]
        public void TryGetAvatarState()
        {
            var states = new State();
            var sheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var avatarState = new AvatarState(
                default,
                default,
                0,
                sheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
            states = (State)states.SetState(default, avatarState.Serialize());

            Assert.True(states.TryGetAvatarState(default, default, out var avatarState2));
            Assert.Equal(avatarState.address, avatarState2.address);
            Assert.Equal(avatarState.agentAddress, avatarState2.agentAddress);
        }

        [Fact]
        public void TryGetAvatarStateEmptyAddress()
        {
            var states = new State();

            Assert.False(states.TryGetAvatarState(default, default, out _));
        }

        [Fact]
        public void TryGetAvatarStateAddressKeyNotFoundException()
        {
            var states = new State().SetState(default, Dictionary.Empty);

            Assert.False(states.TryGetAvatarState(default, default, out _));
        }

        [Fact]
        public void TryGetAvatarStateKeyNotFoundException()
        {
            var states = new State()
                .SetState(
                default,
                Dictionary.Empty
                    .Add("agentAddress", default(Address).Serialize())
            );

            Assert.False(states.TryGetAvatarState(default, default, out _));
        }

        [Fact]
        public void TryGetAvatarStateInvalidCastException()
        {
            var states = new State().SetState(default, default(Text));

            Assert.False(states.TryGetAvatarState(default, default, out _));
        }

        [Fact]
        public void TryGetAvatarStateInvalidAddress()
        {
            var sheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var avatarState = new AvatarState(
                default,
                default,
                0,
                sheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
            var states = new State().SetState(default, avatarState.Serialize());

            Assert.False(states.TryGetAvatarState(Addresses.GameConfig, default, out _));
        }

        [Fact]
        public void GetAvatarStateV2()
        {
            var states = new State();
            var sheets = new TableSheets(TableSheetsImporter.ImportSheets());
            Address avatarAddress = default;
            var avatarState = new AvatarState(
                avatarAddress,
                default,
                0,
                sheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
            states = (State)states
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                .SetState(avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize());

            var v2 = states.GetAvatarStateV2(default);
            Assert.NotNull(v2.inventory);
            Assert.NotNull(v2.worldInformation);
            Assert.NotNull(v2.questList);
        }
    }
}
