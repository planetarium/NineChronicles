namespace Lib9c.Tests.Model.State
{
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class PetStateTest
    {
        [Fact]
        public void Serialize()
        {
            var state = new PetState(1);
            var serialized = state.Serialize();
            var deserialized = new PetState((List)serialized);

            Assert.Equal(state.PetId, deserialized.PetId);
            Assert.Equal(state.Level, deserialized.Level);
        }

        [Fact]
        public void LevelUp()
        {
            var state = new PetState(1);
            var prevLevel = state.Level;
            state.LevelUp();
            Assert.Equal(prevLevel + 1, state.Level);
            var serialized = state.Serialize();
            var deserialized = new PetState((List)serialized);

            Assert.Equal(state.PetId, deserialized.PetId);
            Assert.Equal(prevLevel + 1, deserialized.Level);
        }

        [Fact]
        public void DeriveAddress()
        {
            var avatarAddress = new PrivateKey().ToAddress();
            var expectedAddress = avatarAddress.Derive($"{1}");
            Assert.Equal(expectedAddress, PetState.DeriveAddress(avatarAddress, 1));
        }
    }
}
