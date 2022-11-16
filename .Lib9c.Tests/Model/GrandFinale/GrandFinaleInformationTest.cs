namespace Lib9c.Tests.Model.GrandFinale
{
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Model.GrandFinale;
    using Xunit;

    public class GrandFinaleInformationTest
    {
        [Fact]
        public void Serialize()
        {
            var avatarAddress = new PrivateKey().ToAddress();
            var addr1 = new PrivateKey().ToAddress();
            var addr2 = new PrivateKey().ToAddress();
            var state = new GrandFinaleInformation(avatarAddress, 1);
            state.UpdateRecord(addr1, true);
            state.UpdateRecord(addr2, false);
            var serialized = (List)state.Serialize();
            var deserialized = new GrandFinaleInformation(serialized);

            Assert.Equal(state.Address, deserialized.Address);
            Assert.True(state.TryGetBattleRecord(addr1, out var win));
            Assert.True(deserialized.TryGetBattleRecord(addr1, out var deserializedWin));
            Assert.Equal(win, deserializedWin);

            Assert.True(state.TryGetBattleRecord(addr2, out win));
            Assert.True(deserialized.TryGetBattleRecord(addr2, out deserializedWin));
            Assert.Equal(win, deserializedWin);
        }

        [Fact]
        public void UpdateBattleRecord()
        {
            var avatarAddress = new PrivateKey().ToAddress();
            var state = new GrandFinaleInformation(avatarAddress, 1);
            var enemyAddr = new PrivateKey().ToAddress();
            state.UpdateRecord(enemyAddr, true);
            var serialized = (List)state.Serialize();
            var deserialized = new GrandFinaleInformation(serialized);

            Assert.Equal(state.Address, deserialized.Address);
            Assert.True(state.TryGetBattleRecord(enemyAddr, out var win));
            Assert.True(win);
            Assert.False(state.TryGetBattleRecord(new PrivateKey().ToAddress(), out _));
        }
    }
}
