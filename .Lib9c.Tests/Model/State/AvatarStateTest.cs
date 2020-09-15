namespace Lib9c.Tests.Model.State
{
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using Bencodex;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Model.State;
    using Xunit;

    public class AvatarStateTest
    {
        private Dictionary<string, string> _sheets;
        private TableSheets _tableSheets;

        public AvatarStateTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        public async Task ConstructDeterministic(int waitMilliseconds)
        {
            var rankingState = new RankingState();
            Address avatarAddress = new PrivateKey().ToAddress();
            Address agentAddress = new PrivateKey().ToAddress();
            AvatarState AvatarStateConstructor() =>
                new AvatarState(
                    avatarAddress,
                    agentAddress,
                    0,
                    _tableSheets.GetAvatarSheets(),
                    new GameConfigState(),
                    rankingState.UpdateRankingMap(avatarAddress));

            AvatarState avatarStateA = AvatarStateConstructor();
            await Task.Delay(waitMilliseconds);
            AvatarState avatarStateB = AvatarStateConstructor();

            HashDigest<SHA256> Hash(AvatarState avatarState) => Hashcash.Hash(new Codec().Encode(avatarState.Serialize()));
            Assert.Equal(Hash(avatarStateA), Hash(avatarStateB));
        }
    }
}
