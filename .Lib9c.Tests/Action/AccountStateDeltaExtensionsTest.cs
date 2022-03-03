namespace Lib9c.Tests.Action
{
    using System.Globalization;
    using Bencodex.Types;
    using Libplanet;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;
    using static SerializeKeys;

    public class AccountStateDeltaExtensionsTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AgentState _agentState;
        private readonly AvatarState _avatarState;

        public AccountStateDeltaExtensionsTest()
        {
            _agentAddress = default;
            _avatarAddress = _agentAddress.Derive(string.Format(CultureInfo.InvariantCulture, CreateAvatar2.DeriveFormat, 0));
            _agentState = new AgentState(_agentAddress);
            _agentState.avatarAddresses[0] = _avatarAddress;
            var sheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                sheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
        }

        [Fact]
        public void TryGetAvatarState()
        {
            var states = new State();
            states = (State)states.SetState(_avatarAddress, _avatarState.Serialize());

            Assert.True(states.TryGetAvatarState(_agentAddress, _avatarAddress, out var avatarState2));
            Assert.Equal(_avatarAddress, avatarState2.address);
            Assert.Equal(_agentAddress, avatarState2.agentAddress);
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
            var states = new State().SetState(default, _avatarState.Serialize());

            Assert.False(states.TryGetAvatarState(Addresses.GameConfig, _avatarAddress, out _));
        }

        [Fact]
        public void GetAvatarStateV2()
        {
            var states = new State();
            states = (State)states
                .SetState(_avatarAddress, _avatarState.SerializeV2())
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), _avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), _avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), _avatarState.questList.Serialize());

            var v2 = states.GetAvatarStateV2(_avatarAddress);
            Assert.NotNull(v2.inventory);
            Assert.NotNull(v2.worldInformation);
            Assert.NotNull(v2.questList);
        }

        [Theory]
        [InlineData(LegacyInventoryKey)]
        [InlineData(LegacyWorldInformationKey)]
        [InlineData(LegacyQuestListKey)]
        public void GetAvatarStateV2_Throw_FailedLoadStateException(string key)
        {
            var states = new State();
            states = (State)states
                .SetState(_avatarAddress, _avatarState.SerializeV2())
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), _avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), _avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), _avatarState.questList.Serialize());
            states = (State)states.SetState(_avatarAddress.Derive(key), null);
            var exc = Assert.Throws<FailedLoadStateException>(() => states.GetAvatarStateV2(_avatarAddress));
            Assert.Contains(key, exc.Message);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TryGetAvatarStateV2(bool backward)
        {
            var states = new State();
            if (backward)
            {
                states = (State)states
                    .SetState(_avatarAddress, _avatarState.Serialize());
            }
            else
            {
                states = (State)states
                    .SetState(_avatarAddress, _avatarState.SerializeV2())
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), _avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), _avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), _avatarState.questList.Serialize());
            }

            Assert.True(states.TryGetAvatarStateV2(_agentAddress, _avatarAddress, out _, out bool migrationRequired));
            Assert.Equal(backward, migrationRequired);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TryGetAgentAvatarStatesV2(bool backward)
        {
            var states = new State().SetState(_agentAddress, _agentState.Serialize());

            if (backward)
            {
                states = (State)states
                    .SetState(_avatarAddress, _avatarState.Serialize());
            }
            else
            {
                states = (State)states
                    .SetState(_avatarAddress, _avatarState.SerializeV2())
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), _avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), _avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), _avatarState.questList.Serialize());
            }

            Assert.True(states.TryGetAgentAvatarStatesV2(_agentAddress, _avatarAddress, out _, out _));
        }
    }
}
