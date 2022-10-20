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
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
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
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            Currency currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
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
            bool isLock,
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
                OrderLock? orderLock = null;
                if (isLock)
                {
                    orderLock = new OrderLock(Guid.NewGuid());
                }

                _avatarState.inventory.AddItem(item, count, orderLock);
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
            foreach (var (blockIndex, count, _) in orderData.InventoryData)
            {
                ItemBase item = ItemFactory.CreateTradableMaterial(row);
                ITradableItem tradableItem = (ITradableItem)item;
                tradableItem.RequiredBlockIndex = blockIndex;
                _avatarState.inventory.AddItem(item, count);
            }

            Assert.Equal(orderData.InventoryData.Count, _avatarState.inventory.Items.Count);

            if (orderData.Exception is null)
            {
                ITradableItem result = order.Sell2(_avatarState);

                Assert.Equal(order.ExpiredBlockIndex, result.RequiredBlockIndex);
                Assert.Equal(order.TradableId, result.TradableId);
                Assert.Equal(orderData.TotalCount, _avatarState.inventory.Items.Sum(i => i.count));
                Assert.True(_avatarState.inventory.TryGetTradableItem(tradableId, order.ExpiredBlockIndex, order.ItemCount, out _));
            }
            else
            {
                Assert.Throws(orderData.Exception, () => order.Sell2(_avatarState));
            }
        }

        [Theory]
        [MemberData(nameof(SellMemberData2))]
        public void Sell2(OrderData orderData)
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
            foreach (var (blockIndex, count, isLock) in orderData.InventoryData)
            {
                ItemBase item = ItemFactory.CreateTradableMaterial(row);
                ITradableItem tradableItem = (ITradableItem)item;
                tradableItem.RequiredBlockIndex = blockIndex;
                OrderLock? orderLock = null;
                if (isLock)
                {
                    orderLock = new OrderLock(orderId);
                }

                _avatarState.inventory.AddItem(item, count, orderLock);
                Assert.Equal(isLock, _avatarState.inventory.TryGetLockedItem(new OrderLock(orderId), out _));
            }

            Assert.Equal(orderData.InventoryData.Count, _avatarState.inventory.Items.Count);

            if (orderData.Exception is null)
            {
                ITradableItem result = order.Sell(_avatarState);

                Assert.Equal(order.ExpiredBlockIndex, result.RequiredBlockIndex);
                Assert.Equal(order.TradableId, result.TradableId);
                Assert.Equal(orderData.TotalCount, _avatarState.inventory.Items.Sum(i => i.count));
                Assert.True(_avatarState.inventory.TryGetTradableItem(tradableId, order.ExpiredBlockIndex, order.ItemCount, out var item));
                Assert.True(_avatarState.inventory.TryGetLockedItem(new OrderLock(orderId), out var lockedItem));
                Assert.Equal(item, lockedItem);
            }
            else
            {
                Assert.Throws(orderData.Exception, () => order.Sell2(_avatarState));
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
                order.Sell2(_avatarState);
            }

            if (exc is null)
            {
                Assert.True(_avatarState.inventory.TryGetTradableItem(order.TradableId, order.ExpiredBlockIndex, 1, out _));
                Assert.False(_avatarState.inventory.TryGetTradableItem(order.TradableId, 1, 1, out _));

                ITradableItem result = order.Cancel2(_avatarState, 1);

                Assert.False(_avatarState.inventory.TryGetTradableItem(order.TradableId, order.ExpiredBlockIndex, 1, out _));
                Assert.True(_avatarState.inventory.TryGetTradableItem(order.TradableId, 1, 1, out _));
            }
            else
            {
                Assert.Throws(exc, () => order.Digest2(_avatarState, _tableSheets.CostumeStatSheet));
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
                orderItemCount,
                orderItemSubType
            );

            if (add)
            {
                _avatarState.inventory.AddItem(item, itemCount);
            }

            if (exc is null)
            {
                order.ValidateCancelOrder2(_avatarState, tradableId);
            }
            else
            {
                Assert.Throws(exc, () => order.ValidateCancelOrder2(_avatarState, tradableId));
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
        public void ValidateCancelOrder2(
            bool useAgentAddress,
            bool useAvatarAddress,
            bool useTradableId,
            bool isLock,
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
                orderItemCount,
                orderItemSubType
            );
            OrderLock? orderLock = null;
            if (isLock)
            {
                orderLock = new OrderLock(orderId);
            }

            _avatarState.inventory.AddItem(item, itemCount, orderLock);

            if (exc is null)
            {
                order.ValidateCancelOrder(_avatarState, tradableId);
            }
            else
            {
                Assert.Throws(exc, () => order.ValidateCancelOrder(_avatarState, tradableId));
            }
        }

        [Theory]
        [InlineData(ItemSubType.ApStone, true, true, 1, 1, null)]
        [InlineData(ItemSubType.Hourglass, true, false, 2, 1, null)]
        [InlineData(ItemSubType.ApStone, false, false, 1, 1, typeof(ItemDoesNotExistException))]
        public void Cancel(ItemSubType itemSubType, bool add, bool exist, long blockIndex, int itemCount, Type exc)
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
                blockIndex,
                itemCount,
                itemSubType
            );

            if (add)
            {
                _avatarState.inventory.AddItem(item, itemCount);
                order.Sell2(_avatarState);

                if (exist)
                {
                    _avatarState.inventory.AddItem(item);
                }
            }

            if (exc is null)
            {
                Assert.True(_avatarState.inventory.TryGetTradableItem(item.TradableId, order.ExpiredBlockIndex, itemCount, out _));
                Assert.Equal(exist, _avatarState.inventory.TryGetTradableItem(item.TradableId, blockIndex, 1, out _));

                ITradableItem result = order.Cancel2(_avatarState, blockIndex);

                Assert.Equal(item.TradableId, result.TradableId);
                Assert.Equal(blockIndex, result.RequiredBlockIndex);
                int expectedCount = exist ? itemCount + 1 : 1;
                Assert.False(_avatarState.inventory.TryGetTradableItem(item.TradableId, order.ExpiredBlockIndex, 1, out _));
                Assert.True(_avatarState.inventory.TryGetTradableItem(item.TradableId, blockIndex, expectedCount, out _));
            }
            else
            {
                Assert.Throws(exc, () => order.Cancel2(_avatarState, blockIndex));
            }
        }

        [Theory]
        [InlineData(ItemSubType.ApStone, true, true, 1, 1, null)]
        [InlineData(ItemSubType.Hourglass, true, false, 2, 1, null)]
        [InlineData(ItemSubType.ApStone, false, false, 1, 1, typeof(ItemDoesNotExistException))]
        public void Cancel2(ItemSubType itemSubType, bool isLock, bool exist, long blockIndex, int itemCount, Type exc)
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
                blockIndex,
                itemCount,
                itemSubType
            );

            if (isLock)
            {
                _avatarState.inventory.AddItem(item, itemCount);
                order.Sell(_avatarState);

                if (exist)
                {
                    _avatarState.inventory.AddItem(item);
                }
            }

            if (exc is null)
            {
                var orderLock = new OrderLock(orderId);
                Assert.True(_avatarState.inventory.TryGetLockedItem(orderLock, out _));
                Assert.Equal(exist, _avatarState.inventory.TryGetTradableItem(item.TradableId, blockIndex, 1, out _));

                ITradableItem result = order.Cancel(_avatarState, blockIndex);

                Assert.Equal(item.TradableId, result.TradableId);
                Assert.Equal(blockIndex, result.RequiredBlockIndex);
                int expectedCount = exist ? itemCount + 1 : 1;
                Assert.False(_avatarState.inventory.TryGetLockedItem(orderLock, out _));
                Assert.True(_avatarState.inventory.TryGetTradableItems(item.TradableId, blockIndex, expectedCount, out _));
            }
            else
            {
                Assert.Throws(exc, () => order.Cancel(_avatarState, blockIndex));
            }
        }

        [Theory]
        [InlineData(true, false, false, true, true, true, false, true, Buy.ErrorCodeInvalidAddress)]
        [InlineData(true, true, false, true, true, true, false, true, Buy.ErrorCodeInvalidAddress)]
        [InlineData(true, false, true, true, true, true, false, true, Buy.ErrorCodeInvalidAddress)]
        [InlineData(true, true, true, false, true, true, false, true, Buy.ErrorCodeInvalidTradableId)]
        [InlineData(true, true, true, true, false, true, false, true, Buy.ErrorCodeInvalidPrice)]
        [InlineData(true, true, true, true, true, true, true, true, Buy.ErrorCodeShopItemExpired)]
        [InlineData(true, true, true, true, true, false, false, true, Buy.ErrorCodeItemDoesNotExist)]
        [InlineData(true, true, true, true, true, true, false, false, Buy.ErrorCodeItemDoesNotExist)]
        [InlineData(false, true, true, true, true, true, false, true, Buy.ErrorCodeInvalidItemType)]
        [InlineData(true, true, true, true, true, true, false, true, 0)]
        public void ValidateTransfer(
            bool equalItemType,
            bool equalAgentAddress,
            bool equalAvatarAddress,
            bool equalTradableId,
            bool equalPrice,
            bool add,
            bool expire,
            bool equalCount,
            int expected
        )
        {
            var row = _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Hourglass);
            TradableMaterial item = ItemFactory.CreateTradableMaterial(row);
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            var agentAddress = equalAgentAddress ? _avatarState.agentAddress : default;
            var avatarAddress = equalAvatarAddress ? _avatarState.address : default;
            FungibleOrder order = OrderFactory.CreateFungibleOrder(
                agentAddress,
                avatarAddress,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                item.TradableId,
                1,
                2,
                equalItemType ? ItemSubType.Hourglass : ItemSubType.ApStone
            );
            FungibleAssetValue price = equalPrice ? order.Price : _currency * 0;
            Guid tradableId = equalTradableId ? item.TradableId : default;
            int itemCount = equalCount ? order.ItemCount : order.ItemCount - 1;
            long blockIndex = expire ? order.ExpiredBlockIndex + 1 : order.ExpiredBlockIndex;
            item.RequiredBlockIndex = blockIndex;

            if (add)
            {
                _avatarState.inventory.AddItem(item, itemCount);
            }

            Assert.Equal(expected, order.ValidateTransfer2(_avatarState, tradableId, price, blockIndex));
        }

        [Theory]
        [InlineData(true, false, false, true, true, true, false, true, Buy.ErrorCodeInvalidAddress)]
        [InlineData(true, true, false, true, true, true, false, true, Buy.ErrorCodeInvalidAddress)]
        [InlineData(true, false, true, true, true, true, false, true, Buy.ErrorCodeInvalidAddress)]
        [InlineData(true, true, true, false, true, true, false, true, Buy.ErrorCodeInvalidTradableId)]
        [InlineData(true, true, true, true, false, true, false, true, Buy.ErrorCodeInvalidPrice)]
        [InlineData(true, true, true, true, true, true, true, true, Buy.ErrorCodeShopItemExpired)]
        [InlineData(true, true, true, true, true, false, false, true, Buy.ErrorCodeItemDoesNotExist)]
        [InlineData(true, true, true, true, true, true, false, false, Buy.ErrorCodeItemDoesNotExist)]
        [InlineData(false, true, true, true, true, true, false, true, Buy.ErrorCodeInvalidItemType)]
        [InlineData(true, true, true, true, true, true, false, true, 0)]
        public void ValidateTransfer2(
            bool equalItemType,
            bool equalAgentAddress,
            bool equalAvatarAddress,
            bool equalTradableId,
            bool equalPrice,
            bool add,
            bool expire,
            bool equalCount,
            int expected
        )
        {
            var row = _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Hourglass);
            TradableMaterial item = ItemFactory.CreateTradableMaterial(row);
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            var agentAddress = equalAgentAddress ? _avatarState.agentAddress : default;
            var avatarAddress = equalAvatarAddress ? _avatarState.address : default;
            FungibleOrder order = OrderFactory.CreateFungibleOrder(
                agentAddress,
                avatarAddress,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                item.TradableId,
                1,
                2,
                equalItemType ? ItemSubType.Hourglass : ItemSubType.ApStone
            );
            FungibleAssetValue price = equalPrice ? order.Price : _currency * 0;
            Guid tradableId = equalTradableId ? item.TradableId : default;
            int itemCount = equalCount ? order.ItemCount : order.ItemCount - 1;
            long blockIndex = expire ? order.ExpiredBlockIndex + 1 : order.ExpiredBlockIndex;
            item.RequiredBlockIndex = blockIndex;

            if (add)
            {
                _avatarState.inventory.AddItem(item, itemCount, new OrderLock(orderId));
            }

            Assert.Equal(expected, order.ValidateTransfer(_avatarState, tradableId, price, blockIndex));
        }

        [Theory]
        [InlineData(false, typeof(ItemDoesNotExistException))]
        [InlineData(true, null)]
        public void Transfer(bool add, Type exc)
        {
            var row = _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Hourglass);
            TradableMaterial item = ItemFactory.CreateTradableMaterial(row);
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            FungibleOrder order = OrderFactory.CreateFungibleOrder(
                _avatarState.agentAddress,
                _avatarState.address,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                item.TradableId,
                1,
                1,
                ItemSubType.Hourglass
            );

            if (add)
            {
                _avatarState.inventory.AddItem(item, 1);
                order.Sell2(_avatarState);
            }

            var buyer = new AvatarState(
                Addresses.Blacksmith,
                Addresses.Admin,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default,
                "buyer"
            );

            if (exc is null)
            {
                order.Transfer2(_avatarState, buyer, 100);
                Assert.False(_avatarState.inventory.TryGetTradableItem(order.TradableId, 100, 1, out _));
                Assert.True(buyer.inventory.TryGetTradableItem(order.TradableId, 100, 1, out Inventory.Item inventoryItem));
                ITradableFungibleItem result = (ITradableFungibleItem)inventoryItem.item;
                Assert.Equal(100, result.RequiredBlockIndex);
            }
            else
            {
                Assert.Throws(exc, () => order.Transfer2(_avatarState, buyer, 0));
            }
        }

        [Theory]
        [InlineData(false, typeof(ItemDoesNotExistException))]
        [InlineData(true, null)]
        public void Transfer2(bool add, Type exc)
        {
            var row = _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Hourglass);
            TradableMaterial item = ItemFactory.CreateTradableMaterial(row);
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            FungibleOrder order = OrderFactory.CreateFungibleOrder(
                _avatarState.agentAddress,
                _avatarState.address,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                item.TradableId,
                1,
                1,
                ItemSubType.Hourglass
            );

            if (add)
            {
                _avatarState.inventory.AddItem(item, 1);
                order.Sell(_avatarState);
            }

            var buyer = new AvatarState(
                Addresses.Blacksmith,
                Addresses.Admin,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default,
                "buyer"
            );

            if (exc is null)
            {
                order.Transfer(_avatarState, buyer, 100);
                Assert.False(_avatarState.inventory.TryGetTradableItem(order.TradableId, 100, 1, out _));
                Assert.True(buyer.inventory.TryGetTradableItem(order.TradableId, 100, 1, out Inventory.Item inventoryItem));
                ITradableFungibleItem result = (ITradableFungibleItem)inventoryItem.item;
                Assert.Equal(100, result.RequiredBlockIndex);
            }
            else
            {
                Assert.Throws(exc, () => order.Transfer(_avatarState, buyer, 0));
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
                false,
                null,
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
                ItemSubType.Hourglass,
                true,
                typeof(ItemDoesNotExistException),
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
                false,
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
                false,
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
                false,
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
                false,
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
                false,
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
                false,
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
                false,
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
                false,
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
                    InventoryData = new List<(long, int, bool)>
                    {
                        (1, 1, false),
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
                    InventoryData = new List<(long, int, bool)>
                    {
                        (1, 1, false),
                        (5, 2, false),
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
                    InventoryData = new List<(long, int, bool)>
                    {
                        (10, 20, false),
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
                    InventoryData = new List<(long, int, bool)>
                    {
                        (3, 2, false),
                        (5, 3, false),
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
                    InventoryData = new List<(long, int, bool)>
                    {
                        (1, 1, false),
                        (2, 100, false),
                    },
                    Exception = typeof(ItemDoesNotExistException),
                },
            },
        };

        public static IEnumerable<object[]> SellMemberData2() => new List<object[]>
        {
            new object[]
            {
                new OrderData
                {
                    SellCount = 1,
                    BlockIndex = 1,
                    InventoryData = new List<(long, int, bool)>
                    {
                        (1, 1, false),
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
                    InventoryData = new List<(long, int, bool)>
                    {
                        (1, 1, false),
                        (5, 2, false),
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
                    InventoryData = new List<(long, int, bool)>
                    {
                        (10, 20, false),
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
                    InventoryData = new List<(long, int, bool)>
                    {
                        (3, 2, false),
                        (5, 3, false),
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
                    InventoryData = new List<(long, int, bool)>
                    {
                        (1, 1, false),
                        (2, 100, false),
                    },
                    Exception = typeof(ItemDoesNotExistException),
                },
            },
            new object[]
            {
                new OrderData
                {
                    SellCount = 1,
                    BlockIndex = 1,
                    InventoryData = new List<(long, int, bool)>
                    {
                        (1, 1, true),
                    },
                    Exception = typeof(ItemDoesNotExistException),
                },
            },
            new object[]
            {
                new OrderData
                {
                    SellCount = 3,
                    BlockIndex = 10,
                    InventoryData = new List<(long, int, bool)>
                    {
                        (1, 1, false),
                        (5, 2, true),
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

            public List<(long, int, bool)> InventoryData { get; set; }

            public Type Exception { get; set; }

            public int TotalCount => InventoryData.Sum(i => i.Item2);
        }
    }
}
