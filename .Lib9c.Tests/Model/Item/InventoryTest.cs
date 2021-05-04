namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Nekoyume.Model.Item;
    using Xunit;
    using BxList = Bencodex.Types.List;

    public class InventoryTest
    {
        private static readonly TableSheets TableSheets;

        static InventoryTest()
        {
            TableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Serialize()
        {
            var inventory = new Inventory();
            var serialized = (BxList)inventory.Serialize();
            var deserialized = new Inventory(serialized);
            Assert.Equal(inventory, deserialized);
        }

        [Fact]
        public void Serialize_With_DotNet_Api()
        {
            var inventory = new Inventory();
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, inventory);
            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (Inventory)formatter.Deserialize(ms);
            Assert.Equal(inventory, deserialized);
        }

        [Fact]
        public void SameMaterials_Which_NotSame_IsTradableField_Are_Contained_Separately()
        {
            var row = TableSheets.MaterialItemSheet.First;
            Assert.NotNull(row);
            var material = ItemFactory.CreateMaterial(row);
            var tradableMaterial = ItemFactory.CreateTradableMaterial(row);
            var inventory = new Inventory();
            Assert.Empty(inventory.Items);
            inventory.AddItem(material);
            Assert.Single(inventory.Items);
            Assert.Single(inventory.Materials);
            inventory.AddItem(tradableMaterial);
            Assert.Equal(2, inventory.Items.Count);
            Assert.Equal(2, inventory.Materials.Count());
            inventory.RemoveFungibleItem(row.ItemId);
            Assert.Single(inventory.Items);
            Assert.Single(inventory.Materials);
        }

        [Fact]
        public void RemoveFungibleItem_NonTradableItem_Removed_Faster_Than_TradableItem()
        {
            var row = TableSheets.MaterialItemSheet.First;
            Assert.NotNull(row);
            var material = ItemFactory.CreateMaterial(row);
            var tradableMaterial = ItemFactory.CreateTradableMaterial(row);
            var inventory = new Inventory();
            Assert.Empty(inventory.Items);
            inventory.AddItem(material);
            inventory.AddItem(tradableMaterial);
            inventory.RemoveFungibleItem(row.ItemId);
            Assert.True(inventory.Materials.First() is ITradableFungibleItem);
            inventory.RemoveFungibleItem(row.ItemId);
            Assert.Empty(inventory.Materials);
            inventory.AddItem(tradableMaterial);
            inventory.AddItem(material);
            inventory.RemoveFungibleItem(row.ItemId);
            Assert.True(inventory.Materials.First() is ITradableFungibleItem);
            inventory.RemoveFungibleItem(row.ItemId);
            Assert.Empty(inventory.Materials);
        }

        [Fact]
        public void RemoveTradableItem_IFungibleItem()
        {
            var row = TableSheets.MaterialItemSheet.First;
            Assert.NotNull(row);
            var material = ItemFactory.CreateMaterial(row);
            var tradableMaterial = ItemFactory.CreateTradableMaterial(row);
            var tradableItem = (ITradableItem)tradableMaterial;
            Assert.NotNull(tradableItem);
            var inventory = new Inventory();
            Assert.Empty(inventory.Items);
            inventory.AddItem(material);
            inventory.AddItem(tradableMaterial);
            Assert.True(inventory.RemoveTradableItem(tradableItem));
            Assert.False(inventory.Materials.First() is ITradableFungibleItem);
            Assert.False(inventory.RemoveTradableItem(tradableItem));
            Assert.Single(inventory.Materials);
        }

        [Fact]
        public void RemoveTradableItem_INonFungibleItem()
        {
            var row = TableSheets.EquipmentItemSheet.First;
            Assert.NotNull(row);
            var itemUsable = ItemFactory.CreateItemUsable(row, Guid.NewGuid(), 0);
            var nonFungibleItem = (INonFungibleItem)itemUsable;
            Assert.NotNull(nonFungibleItem);
            var inventory = new Inventory();
            Assert.Empty(inventory.Items);
            inventory.AddItem(itemUsable);
            Assert.Single(inventory.Equipments);
            Assert.True(inventory.RemoveTradableItem(nonFungibleItem));
            Assert.Empty(inventory.Equipments);
            Assert.False(inventory.RemoveTradableItem(nonFungibleItem));
        }

        [Fact]
        public void RemoveTradableFungibleItem()
        {
            var row = TableSheets.MaterialItemSheet.First;
            Assert.NotNull(row);
            var material = ItemFactory.CreateMaterial(row);
            var tradableMaterial = ItemFactory.CreateTradableMaterial(row);
            var inventory = new Inventory();
            Assert.Empty(inventory.Items);
            inventory.AddItem(material);
            inventory.AddItem(tradableMaterial);
            inventory.RemoveTradableFungibleItem(row.ItemId);
            Assert.False(inventory.Materials.First() is ITradableFungibleItem);
            Assert.False(inventory.RemoveTradableFungibleItem(row.ItemId));
            Assert.Single(inventory.Materials);
        }
    }
}
