using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class AvatarStateTest
    {
        private TableSheets _tableSheets;
        private Address _avatarAddress;

        [OneTimeSetUp]
        public void Init()
        {
            _tableSheets = TableSheetsHelper.MakeTableSheets();
            _avatarAddress = new PublicKey(
                ByteUtil.ParseHex("02eb997c654da6486d6e3e777caeba4b6880d57835b62f03b80df4c60431cdf412")).ToAddress();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _tableSheets = null;
        }

        [Test, Sequential]
        public void GetRandomSeed([Values(-599628938, -1717973115, 1534786375)] int expected, [Values(1, 2, 3)] int count)
        {
            var avatarState = new AvatarState(
                _avatarAddress,
                new Address(),
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                new Address()
            );
            Assert.AreEqual(0, avatarState.Nonce);
            var seed = 0;
            for (var i = 0; i < count; i++)
            {
                seed = avatarState.GetRandomSeed();
                Assert.AreEqual(i + 1, avatarState.Nonce);
            }
            Assert.AreEqual(expected, seed);
            var random1 = new System.Random(seed);
            var random2 = new System.Random(seed);
            Assert.AreEqual(random1.Next(100), random2.Next(100));
            Assert.AreEqual(count, avatarState.Nonce);
        }
    }
}
