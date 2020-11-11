namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class ShopItemTest
    {
        private readonly TableSheets _tableSheets;

        public ShopItemTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Test()
        {
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var productId = Guid.NewGuid();
            var weaponRow = new EquipmentItemSheet.Row();
            weaponRow.Set(new[]
            {
                "10100000", "Weapon", "0", "Normal", "0", "ATK", "1", "2", "10100000",
            });
            var itemUsable = new Weapon(weaponRow, Guid.NewGuid(), 0);
            var row = _tableSheets.CostumeItemSheet.Values.First();
            var costume = ItemFactory.CreateCostume(row, default);

            var price = new FungibleAssetValue(new Currency("NCG", 2, minter: null));
            var shopItem = new ShopItem(
                agentAddress,
                avatarAddress,
                productId,
                price,
                itemUsable);

            var beforeSerialized = shopItem.SerializeBackup1();
            var beforeDeserialized = new ShopItem((Dictionary)beforeSerialized);

            var serialized = shopItem.Serialize();
            var deserialized = new ShopItem((Dictionary)serialized);

            Assert.Equal(beforeSerialized, serialized);
            Assert.Equal(beforeDeserialized, deserialized);
        }
    }
}