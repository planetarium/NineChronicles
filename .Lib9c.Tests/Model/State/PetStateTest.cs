namespace Lib9c.Tests.Model.State
{
    using Bencodex.Types;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class PetStateTest
    {
        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void Serialize(int petId)
        {
            var state = new PetState(petId);
            Assert.Equal(petId, state.PetId);
            Assert.Equal(0, state.Level);
            var serialized = state.Serialize();
            var deserialized = new PetState((List)serialized);
            Assert.Equal(state.PetId, deserialized.PetId);
            Assert.Equal(state.Level, deserialized.Level);

            var serialized2 = deserialized.Serialize();
            Assert.Equal(serialized, serialized2);
        }

        [Theory]
        [InlineData(int.MinValue, false)]
        [InlineData(0, false)]
        [InlineData(int.MaxValue, true)]
        public void LevelUp(int initialLevel, bool shouldThrow)
        {
            const int petId = 1001;
            const long blockIndex = 0;
            var serialized = new List(
                petId.Serialize(),
                initialLevel.Serialize(),
                blockIndex.Serialize());
            var state = new PetState(serialized);
            Assert.Equal(petId, state.PetId);
            Assert.Equal(initialLevel, state.Level);
            if (shouldThrow)
            {
                Assert.Throws<System.InvalidOperationException>(() =>
                    state.LevelUp());
            }
            else
            {
                state.LevelUp();
                Assert.Equal(initialLevel + 1, state.Level);
            }
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void DeriveAddress(int petId)
        {
            var avatarAddress = new PrivateKey().ToAddress();
            var expectedAddress = avatarAddress.Derive($"pet-{petId}");
            Assert.Equal(expectedAddress, PetState.DeriveAddress(avatarAddress, petId));
        }
    }
}
