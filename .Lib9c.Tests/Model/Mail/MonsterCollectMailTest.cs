namespace Lib9c.Tests.Model.Mail
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class MonsterCollectMailTest
    {
        private readonly TableSheets _tableSheets;

        public MonsterCollectMailTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Serialize()
        {
            Guid guid = new Guid("F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4");
            Address address = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            List<MonsterCollectionRewardSheet.RewardInfo> rewards = _tableSheets.MonsterCollectionRewardSheet.First!.Rewards;
            Assert.Equal(2, rewards.Count);

            MonsterCollectionResult result = new MonsterCollectionResult(guid, address, rewards);
            MonsterCollectionMail mail = new MonsterCollectionMail(result, 1, guid, 2);
            Dictionary serialized = (Dictionary)mail.Serialize();
            MonsterCollectionMail deserialized = new MonsterCollectionMail(serialized);

            Assert.Equal(1, deserialized.blockIndex);
            Assert.Equal(2, deserialized.requiredBlockIndex);
            Assert.Equal(guid, deserialized.id);
            MonsterCollectionResult attachment = (MonsterCollectionResult)deserialized.attachment;
            Assert.Equal(2, attachment.rewards.Count);
            Assert.Equal(rewards.First(), attachment.rewards.First());
        }

        [Fact]
        public void Serialize_DotNet_API()
        {
            Guid guid = new Guid("F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4");
            Address address = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            List<MonsterCollectionRewardSheet.RewardInfo> rewards = _tableSheets.MonsterCollectionRewardSheet.First!.Rewards;
            Assert.Equal(2, rewards.Count);

            MonsterCollectionResult result = new MonsterCollectionResult(guid, address, rewards);
            MonsterCollectionMail mail = new MonsterCollectionMail(result, 1, guid, 2);
            BinaryFormatter formatter = new BinaryFormatter();
            using MemoryStream ms = new MemoryStream();
            formatter.Serialize(ms, mail);
            ms.Seek(0, SeekOrigin.Begin);

            MonsterCollectionMail deserialized = (MonsterCollectionMail)formatter.Deserialize(ms);

            Assert.Equal(1, deserialized.blockIndex);
            Assert.Equal(2, deserialized.requiredBlockIndex);
            Assert.Equal(guid, deserialized.id);
            MonsterCollectionResult attachment = (MonsterCollectionResult)deserialized.attachment;
            Assert.Equal(2, attachment.rewards.Count);
            Assert.Equal(rewards.First(), attachment.rewards.First());
        }
    }
}
