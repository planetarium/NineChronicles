namespace Lib9c.Tests.Model.Order
{
    using System;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Xunit;

    public class OrderFactoryTest
    {
        private readonly TableSheets _tableSheets;

        public OrderFactoryTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Theory]
        [InlineData(ItemType.Consumable, 1, Order.OrderType.NonFungible)]
        [InlineData(ItemType.Costume, 2, Order.OrderType.NonFungible)]
        [InlineData(ItemType.Equipment, 3, Order.OrderType.NonFungible)]
        [InlineData(ItemType.Material, 4, Order.OrderType.Fungible)]
        public void Create(ItemType itemType, long blockIndex, Order.OrderType orderType)
        {
            ITradableItem tradableItem;
            Guid itemId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            switch (itemType)
            {
                case ItemType.Consumable:
                    tradableItem = ItemFactory.CreateItemUsable(
                        _tableSheets.ConsumableItemSheet.First,
                        itemId,
                        0);
                    break;
                case ItemType.Costume:
                    tradableItem = ItemFactory.CreateCostume(
                        _tableSheets.CostumeItemSheet.First,
                        itemId);
                    break;
                case ItemType.Equipment:
                    tradableItem = ItemFactory.CreateItemUsable(
                        _tableSheets.EquipmentItemSheet.First,
                        itemId,
                        0);
                    break;
                case ItemType.Material:
                    var tradableMaterialRow = _tableSheets.MaterialItemSheet.OrderedList
                        .First(row => row.ItemSubType == ItemSubType.Hourglass);
                    tradableItem = ItemFactory.CreateTradableMaterial(tradableMaterialRow);
                    itemId = tradableItem.TradableId;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null);
            }

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            Guid orderId = new Guid("6d460c1a-755d-48e4-ad67-65d5f519dbc8");

            Order order = OrderFactory.Create(
                Addresses.Admin,
                Addresses.Blacksmith,
                orderId,
                new FungibleAssetValue(currency, 1, 0),
                tradableItem.TradableId,
                blockIndex,
                tradableItem.ItemSubType,
                1
            );

            Assert.Equal(orderType, order.Type);
            Assert.Equal(blockIndex, order.StartedBlockIndex);
            Assert.Equal(currency * 1, order.Price);
            Assert.Equal(Addresses.Admin, order.SellerAgentAddress);
            Assert.Equal(Addresses.Blacksmith, order.SellerAvatarAddress);
            Assert.Equal(orderId, order.OrderId);
            Assert.Equal(itemId, order.TradableId);
            if (orderType == Order.OrderType.Fungible)
            {
                Assert.Equal(1, ((FungibleOrder)order).ItemCount);
            }
        }

        [Theory]
        [InlineData(ItemSubType.EquipmentMaterial)]
        [InlineData(ItemSubType.FoodMaterial)]
        [InlineData(ItemSubType.MonsterPart)]
        [InlineData(ItemSubType.NormalMaterial)]
        public void Create_Throw_InvalidItemTypeException(ItemSubType itemSubType)
        {
            Assert.Throws<InvalidItemTypeException>(() =>
                OrderFactory.Create(default, default, default, default, default, default, itemSubType, 1));
        }

        [Theory]
        [InlineData(ItemType.Consumable, 1, Order.OrderType.NonFungible)]
        [InlineData(ItemType.Costume, 2, Order.OrderType.NonFungible)]
        [InlineData(ItemType.Equipment, 3, Order.OrderType.NonFungible)]
        [InlineData(ItemType.Material, 4, Order.OrderType.Fungible)]
        public void Deserialize(ItemType itemType, long blockIndex, Order.OrderType orderType)
        {
            ITradableItem tradableItem;
            switch (itemType)
            {
                case ItemType.Consumable:
                    tradableItem = ItemFactory.CreateItemUsable(
                        _tableSheets.ConsumableItemSheet.First,
                        Guid.NewGuid(),
                        0);
                    break;
                case ItemType.Costume:
                    tradableItem = ItemFactory.CreateCostume(
                        _tableSheets.CostumeItemSheet.First,
                        Guid.NewGuid());
                    break;
                case ItemType.Equipment:
                    tradableItem = ItemFactory.CreateItemUsable(
                        _tableSheets.EquipmentItemSheet.First,
                        Guid.NewGuid(),
                        0);
                    break;
                case ItemType.Material:
                    var tradableMaterialRow = _tableSheets.MaterialItemSheet.OrderedList
                        .First(row => row.ItemSubType == ItemSubType.Hourglass);
                    tradableItem = ItemFactory.CreateTradableMaterial(tradableMaterialRow);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null);
            }

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            Order order = OrderFactory.Create(
                Addresses.Admin,
                Addresses.Blacksmith,
                default,
                new FungibleAssetValue(currency, 1, 0),
                tradableItem.TradableId,
                blockIndex,
                tradableItem.ItemSubType,
                1
            );

            Dictionary serialized = (Dictionary)order.Serialize();
            Order deserialized = OrderFactory.Deserialize(serialized);
            Assert.Equal(order, deserialized);
            Assert.Equal(orderType, deserialized.Type);
        }
    }
}
