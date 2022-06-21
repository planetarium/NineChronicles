namespace Lib9c.Tests.Model.Arena
{
    using Bencodex.Types;
    using Nekoyume.Model.Arena;
    using Xunit;

    public class ArenaParticipantsTest
    {
        [Fact]
        public void Serialize()
        {
            var state = new ArenaParticipants(1, 1);
            var serialized = (List)state.Serialize();
            var deserialized = new ArenaParticipants(serialized);

            Assert.Equal(state.Address, deserialized.Address);
            Assert.Equal(state.AvatarAddresses, deserialized.AvatarAddresses);
        }
    }
}
