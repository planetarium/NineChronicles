namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Lib9c.Tests.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
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
        public void RemoveTradableItem()
        {
            var random = new TestRandom();
            var inventory = new Inventory();
            Assert.Empty(inventory.Items);

            var tradableItems = new List<ITradableItem>();

            var materialRow = TableSheets.MaterialItemSheet.First;
            Assert.NotNull(materialRow);
            var tradableMaterial = ItemFactory.CreateTradableMaterial(materialRow);
            inventory.AddItem(tradableMaterial);
            Assert.Single(inventory.Items);
            tradableItems.Add(tradableMaterial);
            Assert.Single(tradableItems);

            var equipmentRow = TableSheets.EquipmentItemSheet.First;
            Assert.NotNull(equipmentRow);
            var equipment = (Equipment)ItemFactory.CreateItem(equipmentRow, random);
            inventory.AddItem(equipment);
            Assert.Equal(2, inventory.Items.Count);
            tradableItems.Add(equipment);
            Assert.Equal(2, tradableItems.Count);

            var costumeRow = TableSheets.CostumeItemSheet.First;
            Assert.NotNull(costumeRow);
            var costume = (Costume)ItemFactory.CreateItem(costumeRow, random);
            inventory.AddItem(costume);
            Assert.Equal(3, inventory.Items.Count);
            tradableItems.Add(costume);
            Assert.Equal(3, tradableItems.Count);

            for (var i = 0; i < tradableItems.Count; i++)
            {
                var tradableItem = tradableItems[i];
                Assert.NotNull(tradableItem);
                var tradableId = tradableItem.TradableId;
                Assert.True(inventory.RemoveTradableItem(tradableId));
                Assert.False(inventory.RemoveTradableItem(tradableId));
                Assert.Equal(2 - i, inventory.Items.Count);
            }
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

        [Fact]
        public void AddItem_TradableMaterial()
        {
            var row = TableSheets.MaterialItemSheet.First;
            Assert.NotNull(row);
            var tradableMaterial = ItemFactory.CreateTradableMaterial(row);
            tradableMaterial.RequiredBlockIndex = 0;
            var inventory = new Inventory();
            Assert.Empty(inventory.Items);

            inventory.AddItem(tradableMaterial);
            Assert.Single(inventory.Items);
            Assert.Single(inventory.Materials);

            // Add new tradable material
            var tradableMaterial2 = ItemFactory.CreateTradableMaterial(row);
            tradableMaterial2.RequiredBlockIndex = 1;
            inventory.AddItem(tradableMaterial2);
            Assert.Equal(2, inventory.Items.Count);
            Assert.Equal(2, inventory.Materials.Count());
            Assert.True(inventory.HasItem(row.Id, 2));
        }

        [Theory]
        [InlineData(ItemType.Equipment, 1)]
        [InlineData(ItemType.Costume, 2)]
        [InlineData(ItemType.Consumable, 3)]
        [InlineData(ItemType.Material, 4)]
        public void HasItem(ItemType itemType, int itemCount)
        {
            ItemSheet.Row row = null;
            var inventory = new Inventory();
            switch (itemType)
            {
                case ItemType.Consumable:
                    row = TableSheets.EquipmentItemSheet.First;
                    break;
                case ItemType.Costume:
                    row = TableSheets.CostumeItemSheet.First;
                    break;
                case ItemType.Equipment:
                    row = TableSheets.ConsumableItemSheet.First;
                    break;
                case ItemType.Material:
                    row = TableSheets.MaterialItemSheet.First;
                    break;
            }

            Assert.NotNull(row);
            for (int i = 0; i < itemCount; i++)
            {
                inventory.AddItem(ItemFactory.CreateItem(row, new TestRandom()));
            }

            Assert.True(inventory.HasItem(row.Id, itemCount));
        }

        [Theory]
        [InlineData(3, 3, 1)]
        [InlineData(3, 2, 2)]
        [InlineData(3, 1, 3)]
        public void SellItem_Material_Multiple_Slot(int totalCount, int sellCount, int expectedCount)
        {
            var inventory = new Inventory();
            var row = TableSheets.MaterialItemSheet.First;
            Assert.NotNull(row);
            for (int i = 1; i < totalCount + 1; i++)
            {
                var tradableItem = ItemFactory.CreateTradableMaterial(row);
                tradableItem.RequiredBlockIndex = i;
                inventory.AddItem(tradableItem, 1);
                Assert.True(inventory.TryGetTradableItems(tradableItem.TradableId, i, i, out _));
            }

            Assert.Equal(totalCount, inventory.Items.Count);
            Assert.True(inventory.HasItem(row.Id, totalCount));
            inventory.SellItem(TradableMaterial.DeriveTradableId(row.ItemId), totalCount, sellCount);
            Assert.Equal(expectedCount, inventory.Items.Count);
        }

        [Theory]
        [InlineData(ItemType.Equipment, 1, 1, 1)]
        [InlineData(ItemType.Costume, 1, 1, 1)]
        [InlineData(ItemType.Consumable, 1, 1, 1)]
        [InlineData(ItemType.Material, 1, 1, 1)]
        [InlineData(ItemType.Material, 2, 1, 2)]
        public void SellItem(ItemType itemType, int itemCount, int sellCount, int inventoryCount)
        {
            ItemSheet.Row row;
            switch (itemType)
            {
                case ItemType.Consumable:
                    row = TableSheets.EquipmentItemSheet.First;
                    break;
                case ItemType.Costume:
                    row = TableSheets.CostumeItemSheet.First;
                    break;
                case ItemType.Equipment:
                    row = TableSheets.ConsumableItemSheet.First;
                    break;
                case ItemType.Material:
                    row = TableSheets.MaterialItemSheet.First;
                    break;
                default:
                    throw new Exception();
            }

            var inventory = new Inventory();
            ITradableItem tradableItem;
            if (itemType == ItemType.Material)
            {
                tradableItem = ItemFactory.CreateTradableMaterial((MaterialItemSheet.Row)row);
            }
            else
            {
                tradableItem = (ITradableItem)ItemFactory.CreateItem(row, new TestRandom());
            }

            inventory.AddItem((ItemBase)tradableItem, itemCount);
            Assert.Single(inventory.Items);
            inventory.SellItem(tradableItem.TradableId, 1, sellCount);
            Assert.Equal(inventoryCount, inventory.Items.Count);
        }

        [Theory]
        [InlineData(ItemType.Equipment, 0, true)]
        [InlineData(ItemType.Costume, 0, true)]
        [InlineData(ItemType.Consumable, 0, true)]
        [InlineData(ItemType.Material, 0, true)]
        [InlineData(ItemType.Equipment, 1, false)]
        [InlineData(ItemType.Costume, 1, false)]
        [InlineData(ItemType.Consumable, 1, false)]
        [InlineData(ItemType.Material, 1, false)]
        public void TryGetTradableItems(ItemType itemType, long blockIndex, bool expected)
        {
            ItemSheet.Row row;
            switch (itemType)
            {
                case ItemType.Consumable:
                    row = TableSheets.EquipmentItemSheet.First;
                    break;
                case ItemType.Costume:
                    row = TableSheets.CostumeItemSheet.First;
                    break;
                case ItemType.Equipment:
                    row = TableSheets.ConsumableItemSheet.First;
                    break;
                case ItemType.Material:
                    row = TableSheets.MaterialItemSheet.First;
                    break;
                default:
                    throw new Exception();
            }

            var inventory = new Inventory();
            ITradableItem tradableItem;
            if (itemType == ItemType.Material)
            {
                tradableItem = (ITradableItem)ItemFactory.CreateTradableMaterial((MaterialItemSheet.Row)row);
            }
            else
            {
                tradableItem = (ITradableItem)ItemFactory.CreateItem(row, new TestRandom());
            }

            tradableItem.RequiredBlockIndex = blockIndex;
            inventory.AddItem((ItemBase)tradableItem, 1);
            Assert.Single(inventory.Items);
            Assert.Equal(
                expected,
                inventory.TryGetTradableItems(tradableItem.TradableId, 0, 1, out List<Inventory.Item> items)
            );
            if (expected)
            {
                Assert.Single(items);
                Assert.Equal((ITradableItem)items.First().item, tradableItem);
            }
            else
            {
                Assert.Empty(items);
            }
        }
    }
}
