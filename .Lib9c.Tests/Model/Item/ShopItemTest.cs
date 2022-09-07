namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume.Model.Item;
    using Xunit;
    using BxDictionary = Bencodex.Types.Dictionary;

    public class ShopItemTest
    {
        private static Currency _currency;
        private static TableSheets _tableSheets;

        public ShopItemTest()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Serialize()
        {
            foreach (var shopItem in GetShopItems())
            {
                var serialized = shopItem.Serialize();
                var deserialized = new ShopItem((BxDictionary)serialized);

                Assert.Equal(shopItem, deserialized);
            }
        }

        [Fact]
        public void Serialize_With_DotNet_Api()
        {
            foreach (var shopItem in GetShopItems())
            {
                var formatter = new BinaryFormatter();
                using var ms = new MemoryStream();
                formatter.Serialize(ms, shopItem);
                ms.Seek(0, SeekOrigin.Begin);

                var deserialized = (ShopItem)formatter.Deserialize(ms);

                Assert.Equal(shopItem, deserialized);
            }
        }

        // NOTE: `SerializeBackup1()` only tests with `ShopItem` containing `Equipment`.
        [Fact]
        public void SerializeBackup1()
        {
            var shopItem = GetShopItemWithFirstEquipment();
            var serializedBackup1 = shopItem.SerializeBackup1();
            var deserializedBackup1 = new ShopItem((BxDictionary)serializedBackup1);
            var serialized = shopItem.Serialize();
            var deserialized = new ShopItem((BxDictionary)serialized);
            Assert.Equal(serializedBackup1, serialized);
            Assert.Equal(deserializedBackup1, deserialized);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(10, true)]
        public void Serialize_With_ExpiredBlockIndex(long expiredBlockIndex, bool contain)
        {
            var equipmentRow = _tableSheets.EquipmentItemSheet.First;
            var equipment = new Equipment(equipmentRow, Guid.NewGuid(), 0);
            var shopItem = new ShopItem(
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                Guid.NewGuid(),
                new FungibleAssetValue(_currency, 100, 0),
                expiredBlockIndex,
                equipment);
            Assert.Null(shopItem.Costume);
            Assert.NotNull(shopItem.ItemUsable);
            var serialized = (BxDictionary)shopItem.Serialize();

            Assert.Equal(contain, serialized.ContainsKey(ShopItem.ExpiredBlockIndexKey));

            var deserialized = new ShopItem(serialized);
            Assert.Equal(shopItem, deserialized);
        }

        [Fact]
        public void ThrowArgumentOurOfRangeException()
        {
            var equipmentRow = _tableSheets.EquipmentItemSheet.First;
            var equipment = new Equipment(equipmentRow, Guid.NewGuid(), 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShopItem(
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                Guid.NewGuid(),
                new FungibleAssetValue(_currency, 100, 0),
                -1,
                equipment));
        }

        [Fact]
        public void DeserializeThrowArgumentOurOfRangeException()
        {
            var equipmentRow = _tableSheets.EquipmentItemSheet.First;
            var equipment = new Equipment(equipmentRow, Guid.NewGuid(), 0);
            var shopItem = new ShopItem(
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                Guid.NewGuid(),
                new FungibleAssetValue(_currency, 100, 0),
                0,
                equipment);
            Dictionary serialized = (Dictionary)shopItem.Serialize();
            serialized = serialized.SetItem(ShopItem.ExpiredBlockIndexKey, "-1");
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShopItem(serialized));
        }

        private static ShopItem[] GetShopItems() => new[]
        {
            GetShopItemWithFirstCostume(),
            GetShopItemWithFirstEquipment(),
            GetShopItemWithFirstMaterial(),
        };

        private static ShopItem GetShopItemWithFirstCostume()
        {
            var costumeRow = _tableSheets.CostumeItemSheet.First;
            var costume = new Costume(costumeRow, Guid.NewGuid());
            return new ShopItem(
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                Guid.NewGuid(),
                new FungibleAssetValue(_currency, 100, 0),
                costume);
        }

        private static ShopItem GetShopItemWithFirstEquipment()
        {
            var equipmentRow = _tableSheets.EquipmentItemSheet.First;
            var equipment = new Equipment(equipmentRow, Guid.NewGuid(), 0);
            return new ShopItem(
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                Guid.NewGuid(),
                new FungibleAssetValue(_currency, 100, 0),
                equipment);
        }

        private static ShopItem GetShopItemWithFirstMaterial()
        {
            var row = _tableSheets.MaterialItemSheet.First;
            var tradableMaterial = new TradableMaterial(row);
            return new ShopItem(
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                Guid.NewGuid(),
                new FungibleAssetValue(_currency, 100, 0),
                1,
                tradableMaterial,
                0);
        }

        private static IEnumerable<object[]> GetShopItemsWithTradableMaterial()
        {
            var objects = new object[2];
            var index = 0;
            foreach (var row in _tableSheets.MaterialItemSheet.OrderedList
                .Where(e => e.ItemSubType == ItemSubType.Hourglass || e.ItemSubType == ItemSubType.ApStone))
            {
                var tradableMaterial = new TradableMaterial(row);
                var shopItem = new ShopItem(
                    new PrivateKey().ToAddress(),
                    new PrivateKey().ToAddress(),
                    Guid.NewGuid(),
                    new FungibleAssetValue(_currency, 100, 0),
                    1,
                    tradableMaterial,
                    0);
                objects[index++] = shopItem;
            }

            return new List<object[]>
            {
                objects,
            };
        }
    }
}
