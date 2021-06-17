namespace Lib9c.Tests.Model.Order
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Libplanet;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Xunit;

    public class FungibleOrderTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Currency _currency;
        private readonly AvatarState _avatarState;

        public FungibleOrderTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _currency = new Currency("NCG", 2, minter: null);
            _avatarState = new AvatarState(
                Addresses.Blacksmith,
                Addresses.Admin,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default,
                "name"
            );
        }

        [Fact]
        public void Serialize()
        {
            Guid orderId = new Guid("6d460c1a-755d-48e4-ad67-65d5f519dbc8");
            Guid itemId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            FungibleOrder order = OrderFactory.CreateFungibleOrder(
                Addresses.Admin,
                Addresses.Blacksmith,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                itemId,
                1,
                2,
                ItemSubType.Hourglass
            );

            Assert.Equal(1, order.StartedBlockIndex);
            Assert.Equal(_currency * 10, order.Price);
            Assert.Equal(Order.OrderType.Fungible, order.Type);
            Assert.Equal(Addresses.Admin, order.SellerAgentAddress);
            Assert.Equal(Addresses.Blacksmith, order.SellerAvatarAddress);
            Assert.Equal(orderId, order.OrderId);
            Assert.Equal(itemId, order.TradableId);
            Assert.Equal(2, order.ItemCount);
            Assert.Equal(ItemSubType.Hourglass, order.ItemSubType);

            Dictionary serialized = (Dictionary)order.Serialize();

            Assert.Equal(order, new FungibleOrder(serialized));
        }

        [Fact]
        public void Serialize_DotNet_Api()
        {
            Guid orderId = new Guid("6d460c1a-755d-48e4-ad67-65d5f519dbc8");
            Guid itemId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            Currency currency = new Currency("NCG", 2, minter: null);
            FungibleOrder order = OrderFactory.CreateFungibleOrder(
                Addresses.Admin,
                Addresses.Blacksmith,
                orderId,
                new FungibleAssetValue(currency, 10, 0),
                itemId,
                1,
                1,
                ItemSubType.Hourglass
            );

            Assert.Equal(1, order.StartedBlockIndex);
            Assert.Equal(currency * 10, order.Price);
            Assert.Equal(Order.OrderType.Fungible, order.Type);
            Assert.Equal(Addresses.Admin, order.SellerAgentAddress);
            Assert.Equal(Addresses.Blacksmith, order.SellerAvatarAddress);
            Assert.Equal(orderId, order.OrderId);
            Assert.Equal(itemId, order.TradableId);
            Assert.Equal(1, order.ItemCount);
            Assert.Equal(ItemSubType.Hourglass, order.ItemSubType);

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, order);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (FungibleOrder)formatter.Deserialize(ms);

            Assert.Equal(order, deserialized);
            Assert.Equal(order.Serialize(), deserialized.Serialize());
        }

        [Theory]
        [MemberData(nameof(ValidateMemberData))]
        public void Validate(
            int count,
            int orderCount,
            int requiredBlockIndex,
            Address agentAddress,
            Address avatarAddress,
            bool add,
            ItemSubType itemSubType,
            ItemSubType orderItemSubType,
            Type exc
        )
        {
            var row = _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == itemSubType);
            ItemBase item = ItemFactory.CreateTradableMaterial(row);
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            ITradableItem tradableItem = (ITradableItem)item;
            tradableItem.RequiredBlockIndex = requiredBlockIndex;
            FungibleOrder order = OrderFactory.CreateFungibleOrder(
                agentAddress,
                avatarAddress,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                tradableItem.TradableId,
                1,
                orderCount,
                orderItemSubType
            );
            if (add)
            {
                _avatarState.inventory.AddItem(item, count);
            }

            if (exc is null)
            {
                order.Validate(_avatarState, count);
            }
            else
            {
                Assert.Throws(exc, () => order.Validate(_avatarState, count));
            }
        }

        [Theory]
        [MemberData(nameof(SellMemberData))]
        public void Sell(OrderData orderData)
        {
            var row = _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Hourglass);
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            Guid tradableId = TradableMaterial.DeriveTradableId(row.ItemId);
            FungibleOrder order = OrderFactory.CreateFungibleOrder(
                _avatarState.agentAddress,
                _avatarState.address,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                tradableId,
                orderData.BlockIndex,
                orderData.SellCount,
                ItemSubType.Hourglass
            );
            foreach (var (blockIndex, count) in orderData.InventoryData)
            {
                ItemBase item = ItemFactory.CreateTradableMaterial(row);
                ITradableItem tradableItem = (ITradableItem)item;
                tradableItem.RequiredBlockIndex = blockIndex;
                _avatarState.inventory.AddItem(item, count);
            }

            Assert.Equal(orderData.InventoryData.Count, _avatarState.inventory.Items.Count);

            if (orderData.Exception is null)
            {
                ITradableItem result = order.Sell(_avatarState);

                Assert.Equal(order.ExpiredBlockIndex, result.RequiredBlockIndex);
                Assert.Equal(order.TradableId, result.TradableId);
                Assert.Equal(orderData.TotalCount, _avatarState.inventory.Items.Sum(i => i.count));
                Assert.True(_avatarState.inventory.TryGetTradableItem(tradableId, order.ExpiredBlockIndex, order.ItemCount, out _));
            }
            else
            {
                Assert.Throws(orderData.Exception, () => order.Sell(_avatarState));
            }
        }

        [Theory]
        [InlineData(ItemSubType.ApStone, true, null)]
        [InlineData(ItemSubType.Hourglass, false, typeof(ItemDoesNotExistException))]
        public void Digest(ItemSubType itemSubType, bool add, Type exc)
        {
            var row = _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == itemSubType);
            TradableMaterial item = ItemFactory.CreateTradableMaterial(row);
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            item.RequiredBlockIndex = 1;
            FungibleOrder order = OrderFactory.CreateFungibleOrder(
                _avatarState.agentAddress,
                _avatarState.address,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                item.TradableId,
                1,
                1,
                itemSubType
            );

            if (add)
            {
                _avatarState.inventory.AddNonFungibleItem(item);
                order.Sell(_avatarState);
            }

            if (exc is null)
            {
                Assert.True(_avatarState.inventory.TryGetTradableItem(order.TradableId, order.ExpiredBlockIndex, 1, out _));
                Assert.False(_avatarState.inventory.TryGetTradableItem(order.TradableId, 1, 1, out _));

                ITradableItem result = order.Cancel(_avatarState, 1);

                Assert.False(_avatarState.inventory.TryGetTradableItem(order.TradableId, order.ExpiredBlockIndex, 1, out _));
                Assert.True(_avatarState.inventory.TryGetTradableItem(order.TradableId, 1, 1, out _));
            }
            else
            {
                Assert.Throws(exc, () => order.Digest(_avatarState, _tableSheets.CostumeStatSheet));
            }
        }

        [Theory]
        [InlineData(false, false, false, false, ItemSubType.Hourglass, ItemSubType.Hourglass, 1, 1, typeof(InvalidAddressException))]
        [InlineData(true, false, false, false, ItemSubType.Hourglass, ItemSubType.Hourglass, 1, 1, typeof(InvalidAddressException))]
        [InlineData(true, true, false, false, ItemSubType.Hourglass, ItemSubType.Hourglass, 1, 1, typeof(InvalidTradableIdException))]
        [InlineData(true, true, true, false, ItemSubType.Hourglass, ItemSubType.Hourglass, 1, 1, typeof(ItemDoesNotExistException))]
        [InlineData(true, true, true, true, ItemSubType.ApStone, ItemSubType.ApStone, 1, 2, typeof(ItemDoesNotExistException))]
        [InlineData(true, true, true, true, ItemSubType.Hourglass, ItemSubType.ApStone, 1, 1, typeof(InvalidItemTypeException))]
        [InlineData(true, true, true, true, ItemSubType.ApStone, ItemSubType.ApStone, 1, 1, null)]
        public void ValidateCancelOrder(
            bool useAgentAddress,
            bool useAvatarAddress,
            bool useTradableId,
            bool add,
            ItemSubType itemSubType,
            ItemSubType orderItemSubType,
            int itemCount,
            int orderItemCount,
            Type exc
        )
        {
            var row = _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == itemSubType);
            TradableMaterial item = ItemFactory.CreateTradableMaterial(row);
            item.RequiredBlockIndex = 1;
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            var agentAddress = useAgentAddress ? _avatarState.agentAddress : default;
            var avatarAddress = useAvatarAddress ? _avatarState.address : default;
            var tradableId = useTradableId ? item.TradableId : default;
            FungibleOrder order = OrderFactory.CreateFungibleOrder(
                agentAddress,
                avatarAddress,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                item.TradableId,
                1,
                1,
                orderItemSubType
            );

            if (add)
            {
                _avatarState.inventory.AddItem(item, 1);
            }

            if (exc is null)
            {
                order.ValidateCancelOrder(_avatarState, tradableId);
            }
            else
            {
                Assert.Throws(exc, () => order.ValidateCancelOrder(_avatarState, tradableId));
            }
        }

#pragma warning disable SA1204
        public static IEnumerable<object[]> ValidateMemberData() => new List<object[]>
        {
            new object[]
            {
                1,
                1,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                true,
                ItemSubType.Hourglass,
                ItemSubType.Hourglass,
                null,
            },
            new object[]
            {
                1,
                1,
                0,
                default,
                default,
                false,
                ItemSubType.Hourglass,
                ItemSubType.Hourglass,
                typeof(InvalidAddressException),
            },
            new object[]
            {
                0,
                1,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                false,
                ItemSubType.Hourglass,
                ItemSubType.Hourglass,
                typeof(InvalidItemCountException),
            },
            new object[]
            {
                1,
                2,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                false,
                ItemSubType.Hourglass,
                ItemSubType.Hourglass,
                typeof(InvalidItemCountException),
            },
            new object[]
            {
                2,
                1,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                false,
                ItemSubType.Hourglass,
                ItemSubType.Hourglass,
                typeof(InvalidItemCountException),
            },
            new object[]
            {
                -1,
                2,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                false,
                ItemSubType.Hourglass,
                ItemSubType.Hourglass,
                typeof(InvalidItemCountException),
            },
            new object[]
            {
                1,
                1,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                false,
                ItemSubType.Hourglass,
                ItemSubType.Hourglass,
                typeof(ItemDoesNotExistException),
            },
            new object[]
            {
                1,
                1,
                2,
                Addresses.Admin,
                Addresses.Blacksmith,
                true,
                ItemSubType.Hourglass,
                ItemSubType.Hourglass,
                typeof(ItemDoesNotExistException),
            },
            new object[]
            {
                1,
                1,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                true,
                ItemSubType.Hourglass,
                ItemSubType.Food,
                typeof(InvalidItemTypeException),
            },
        };

        public static IEnumerable<object[]> SellMemberData() => new List<object[]>
        {
            new object[]
            {
                new OrderData
                {
                    SellCount = 1,
                    BlockIndex = 1,
                    InventoryData = new List<(long, int)>
                    {
                        (1, 1),
                    },
                    Exception = null,
                },
            },
            new object[]
            {
                new OrderData
                {
                    SellCount = 3,
                    BlockIndex = 10,
                    InventoryData = new List<(long, int)>
                    {
                        (1, 1),
                        (5, 2),
                    },
                    Exception = null,
                },
            },
            new object[]
            {
                new OrderData
                {
                    SellCount = 3,
                    BlockIndex = 100,
                    InventoryData = new List<(long, int)>
                    {
                        (10, 20),
                    },
                    Exception = null,
                },
            },
            new object[]
            {
                new OrderData
                {
                    SellCount = 1,
                    BlockIndex = 1,
                    InventoryData = new List<(long, int)>
                    {
                        (3, 2),
                        (5, 3),
                    },
                    Exception = typeof(ItemDoesNotExistException),
                },
            },
            new object[]
            {
                new OrderData
                {
                    SellCount = 2,
                    BlockIndex = 1,
                    InventoryData = new List<(long, int)>
                    {
                        (1, 1),
                        (2, 100),
                    },
                    Exception = typeof(ItemDoesNotExistException),
                },
            },
        };
#pragma warning restore SA1204

        public class OrderData
        {
            public int SellCount { get; set; }

            public long BlockIndex { get; set; }

            public List<(long, int)> InventoryData { get; set; }

            public Type Exception { get; set; }

            public int TotalCount => InventoryData.Sum(i => i.Item2);
        }
    }
}
