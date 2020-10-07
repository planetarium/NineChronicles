namespace Lib9c.Tests.Model.State
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using Bencodex;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Model.Quest;
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

        [Fact]
        public void Serialize()
        {
            Address avatarAddress = new PrivateKey().ToAddress();
            Address agentAddress = new PrivateKey().ToAddress();
            var avatarState = GetNewAvatarState(avatarAddress, agentAddress);

            var serialized = avatarState.Serialize();
            var deserialized = new AvatarState((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(avatarState.address, deserialized.address);
            Assert.Equal(avatarState.agentAddress, deserialized.agentAddress);
            Assert.Equal(avatarState.blockIndex, deserialized.blockIndex);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        public async Task ConstructDeterministic(int waitMilliseconds)
        {
            Address avatarAddress = new PrivateKey().ToAddress();
            Address agentAddress = new PrivateKey().ToAddress();
            AvatarState avatarStateA = GetNewAvatarState(avatarAddress, agentAddress);
            await Task.Delay(waitMilliseconds);
            AvatarState avatarStateB = GetNewAvatarState(avatarAddress, agentAddress);

            HashDigest<SHA256> Hash(AvatarState avatarState) => Hashcash.Hash(new Codec().Encode(avatarState.Serialize()));
            Assert.Equal(Hash(avatarStateA), Hash(avatarStateB));
        }

        [Fact]
        public void UpdateFromQuestRewardDeterministic()
        {
            var rankingState = new RankingState();
            Address avatarAddress = new PrivateKey().ToAddress();
            Address agentAddress = new PrivateKey().ToAddress();
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingState.UpdateRankingMap(avatarAddress));
            var itemIds = avatarState.questList.OfType<ItemTypeCollectQuest>().First().ItemIds;
            var map = new Dictionary<int, int>()
            {
                [400000] = 1,
                [302002] = 1,
                [302003] = 1,
                [302001] = 1,
                [306023] = 1,
                [302000] = 1,
            };

            var serialized = (Dictionary)avatarState.questList.OfType<WorldQuest>().First().Serialize();
            serialized = serialized.SetItem("reward", new QuestReward(map).Serialize());

            var quest = new WorldQuest(serialized);

            avatarState.UpdateFromQuestReward(quest, _tableSheets.MaterialItemSheet);
            Assert.Equal(
                avatarState.questList.OfType<ItemTypeCollectQuest>().First().ItemIds,
                new List<int>()
                {
                    302000,
                    302001,
                    302002,
                    302003,
                    306023,
                }
            );
        }

        private AvatarState GetNewAvatarState(Address avatarAddress, Address agentAddress)
        {
            var rankingState = new RankingState();
            return new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingState.UpdateRankingMap(avatarAddress));
        }
    }
}
