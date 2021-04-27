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

    public class ShopItemTest
    {
        private static readonly Currency Currency;
        private static readonly TableSheets TableSheets;

        static ShopItemTest()
        {
            Currency = new Currency("NCG", 2, minters: null);
            TableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        public static IEnumerable<object[]> GetShopItems() => new List<object[]>
        {
            new object[]
            {
                GetShopItemWithFirstCostume(),
                GetShopItemWithFirstEquipment(),
                GetShopItemWithFirstMaterial(),
            },
        };

        [Theory]
        [MemberData(nameof(GetShopItems))]
        public void Serialize(params ShopItem[] shopItems)
        {
            foreach (var shopItem in shopItems)
            {
                var serialized = shopItem.Serialize();
                var deserialized = new ShopItem((Bencodex.Types.Dictionary)serialized);

                Assert.Equal(shopItem, deserialized);
            }
        }

        [Theory]
        [MemberData(nameof(GetShopItems))]
        public void SerializeWithDotNetAPI(params ShopItem[] shopItems)
        {
            foreach (var shopItem in shopItems)
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
            var deserializedBackup1 = new ShopItem((Dictionary)serializedBackup1);
            var serialized = shopItem.Serialize();
            var deserialized = new ShopItem((Dictionary)serialized);
            Assert.Equal(serializedBackup1, serialized);
            Assert.Equal(deserializedBackup1, deserialized);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(10, true)]
        public void SerializeWithExpiredBlockIndex(long expiredBlockIndex, bool contain)
        {
            var equipmentRow = TableSheets.EquipmentItemSheet.First;
            var equipment = new Equipment(equipmentRow, Guid.NewGuid(), 0);
            var shopItem = new ShopItem(
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                Guid.NewGuid(),
                new FungibleAssetValue(Currency, 100, 0),
                expiredBlockIndex,
                equipment);
            Assert.Null(shopItem.Costume);
            Assert.NotNull(shopItem.ItemUsable);
            Dictionary serialized = (Dictionary)shopItem.Serialize();

            Assert.Equal(contain, serialized.ContainsKey(ShopItem.ExpiredBlockIndexKey));

            var deserialized = new ShopItem(serialized);
            Assert.Equal(shopItem, deserialized);
        }

        [Fact]
        public void Equals_With_Or_Without_Material_When_Material_Is_Null()
        {
            var shopItems = new List<ShopItem>
            {
                new ShopItem(
                    new PrivateKey().ToAddress(),
                    new PrivateKey().ToAddress(),
                    Guid.NewGuid(),
                    new FungibleAssetValue(Currency, 100, 0),
                    null,
                    1,
                    0),
                new ShopItem(
                    new PrivateKey().ToAddress(),
                    new PrivateKey().ToAddress(),
                    Guid.NewGuid(),
                    new FungibleAssetValue(Currency, 100, 0),
                    null,
                    0,
                    0),
            };

            foreach (var shopItem in shopItems)
            {
                var serialized = (Dictionary)shopItem.Serialize();
                var serializedWithoutMaterial = serialized.ContainsKey("material")
                    ? new Dictionary(serialized.Remove((Text)"material"))
                    : serialized;
                Assert.Equal(serialized, serializedWithoutMaterial);

                var deserialized = new ShopItem(serialized);
                var deserializedWithoutMaterial = new ShopItem(serializedWithoutMaterial);
                Assert.Equal(deserialized, deserializedWithoutMaterial);
                Assert.Equal(shopItem, deserializedWithoutMaterial);
            }
        }

        [Fact]
        public void ThrowArgumentOurOfRangeException()
        {
            var equipmentRow = TableSheets.EquipmentItemSheet.First;
            var equipment = new Equipment(equipmentRow, Guid.NewGuid(), 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShopItem(
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                Guid.NewGuid(),
                new FungibleAssetValue(Currency, 100, 0),
                -1,
                equipment));
        }

        [Fact]
        public void DeserializeThrowArgumentOurOfRangeException()
        {
            var equipmentRow = TableSheets.EquipmentItemSheet.First;
            var equipment = new Equipment(equipmentRow, Guid.NewGuid(), 0);
            var shopItem = new ShopItem(
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                Guid.NewGuid(),
                new FungibleAssetValue(Currency, 100, 0),
                0,
                equipment);
            Dictionary serialized = (Dictionary)shopItem.Serialize();
            serialized = serialized.SetItem(ShopItem.ExpiredBlockIndexKey, "-1");
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShopItem(serialized));
        }

        private static ShopItem GetShopItemWithFirstCostume()
        {
            var costumeRow = TableSheets.CostumeItemSheet.First;
            var costume = new Costume(costumeRow, Guid.NewGuid());
            return new ShopItem(
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                Guid.NewGuid(),
                new FungibleAssetValue(Currency, 100, 0),
                costume);
        }

        private static ShopItem GetShopItemWithFirstEquipment()
        {
            var equipmentRow = TableSheets.EquipmentItemSheet.First;
            var equipment = new Equipment(equipmentRow, Guid.NewGuid(), 0);
            return new ShopItem(
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                Guid.NewGuid(),
                new FungibleAssetValue(Currency, 100, 0),
                equipment);
        }

        private static ShopItem GetShopItemWithFirstMaterial()
        {
            var row = TableSheets.MaterialItemSheet.First;
            var material = new Material(row);
            return new ShopItem(
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                Guid.NewGuid(),
                new FungibleAssetValue(Currency, 100, 0),
                material,
                1,
                0);
        }

        private static IEnumerable<object[]> GetShopItemsWithTradableMaterial()
        {
            var objects = new object[2];
            var index = 0;
            foreach (var row in TableSheets.MaterialItemSheet.OrderedList
                .Where(e => e.ItemSubType == ItemSubType.Hourglass || e.ItemSubType == ItemSubType.ApStone))
            {
                var material = new Material(row, true);
                var shopItem = new ShopItem(
                    new PrivateKey().ToAddress(),
                    new PrivateKey().ToAddress(),
                    Guid.NewGuid(),
                    new FungibleAssetValue(Currency, 100, 0),
                    material,
                    1,
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
