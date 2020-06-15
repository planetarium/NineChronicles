using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class StateExtensionsTest
    {
        [Test]
        public void SerializePublicKey()
        {
            var key = new PrivateKey().PublicKey;
            var serialized = key.Serialize();
            Assert.AreEqual(key, serialized.ToPublicKey());
        }

        [Test]
        public void SerializedItemId()
        {
            var tableSheets = TableSheetsHelper.MakeTableSheets();
            var row = tableSheets.MaterialItemSheet.Values.First();
            var serialized = row.ItemId.Serialize();
            Assert.AreEqual(row.ItemId, serialized.ToItemId());
        }

        [Test]
        public void SerializeChestList()
        {
            var tableSheets = TableSheetsHelper.MakeTableSheets();
            var row = tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Chest);
            var chest = ItemFactory.CreateChest(row, null);
            var rewards = tableSheets.RedeemRewardSheet.Values.First().Rewards;
            var chest2 = ItemFactory.CreateChest(row, rewards);
            var chestList = new Dictionary<HashDigest<SHA256>, int>
            {
                [chest.ItemId] = 3,
                [chest2.ItemId] = 4,
            };
            var serialized = chestList.Serialize();
            Assert.AreEqual(chestList, serialized.Deserialize());
        }
    }
}
