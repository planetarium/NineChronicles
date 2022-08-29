namespace Lib9c.Tests.Model.Order
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Lib9c.Tests.Action;
    using Libplanet;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Xunit;

    public class NonFungibleOrderTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Currency _currency;
        private readonly AvatarState _avatarState;

        public NonFungibleOrderTest()
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
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                Addresses.Admin,
                Addresses.Blacksmith,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                itemId,
                1,
                ItemSubType.Weapon
            );

            Assert.Equal(1, order.StartedBlockIndex);
            Assert.Equal(_currency * 10, order.Price);
            Assert.Equal(Order.OrderType.NonFungible, order.Type);
            Assert.Equal(Addresses.Admin, order.SellerAgentAddress);
            Assert.Equal(Addresses.Blacksmith, order.SellerAvatarAddress);
            Assert.Equal(orderId, order.OrderId);
            Assert.Equal(itemId, order.TradableId);
            Assert.Equal(ItemSubType.Weapon, order.ItemSubType);

            Dictionary serialized = (Dictionary)order.Serialize();

            Assert.Equal(order, new NonFungibleOrder(serialized));
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
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                Addresses.Admin,
                Addresses.Blacksmith,
                orderId,
                new FungibleAssetValue(currency, 10, 0),
                itemId,
                1,
                ItemSubType.Weapon
            );

            Assert.Equal(1, order.StartedBlockIndex);
            Assert.Equal(currency * 10, order.Price);
            Assert.Equal(Order.OrderType.NonFungible, order.Type);
            Assert.Equal(Addresses.Admin, order.SellerAgentAddress);
            Assert.Equal(Addresses.Blacksmith, order.SellerAvatarAddress);
            Assert.Equal(orderId, order.OrderId);
            Assert.Equal(itemId, order.TradableId);
            Assert.Equal(ItemSubType.Weapon, order.ItemSubType);

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, order);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (NonFungibleOrder)formatter.Deserialize(ms);

            Assert.Equal(order, deserialized);
            Assert.Equal(order.Serialize(), deserialized.Serialize());
        }

        [Theory]
        [MemberData(nameof(ValidateMemberData))]
        public void Validate(
            int count,
            int requiredBlockIndex,
            Address agentAddress,
            Address avatarAddress,
            bool add,
            ItemSubType itemSubType,
            ItemSubType orderItemSubType,
            Type exc
        )
        {
            var row = _tableSheets.ItemSheet.OrderedList.First(r => r.ItemSubType == itemSubType);
            ItemBase item = ItemFactory.CreateItem(row, new TestRandom());
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            ITradableItem tradableItem = (ITradableItem)item;
            tradableItem.RequiredBlockIndex = requiredBlockIndex;
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                agentAddress,
                avatarAddress,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                tradableItem.TradableId,
                1,
                orderItemSubType
            );
            if (add)
            {
                _avatarState.inventory.AddNonFungibleItem(item);
            }

            if (exc is null)
            {
                order.Validate(_avatarState, 1);
            }
            else
            {
                Assert.Throws(exc, () => order.Validate(_avatarState, count));
            }
        }

        [Theory]
        [InlineData(ItemSubType.Weapon, true, null)]
        [InlineData(ItemSubType.Weapon, false, typeof(ItemDoesNotExistException))]
        [InlineData(ItemSubType.FullCostume, true, null)]
        [InlineData(ItemSubType.Food, false, typeof(ItemDoesNotExistException))]
        public void Sell(ItemSubType itemSubType, bool add, Type exc)
        {
            var row = _tableSheets.ItemSheet.OrderedList.First(r => r.ItemSubType == itemSubType);
            ItemBase item = ItemFactory.CreateItem(row, new TestRandom());
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            ITradableItem tradableItem = (ITradableItem)item;
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                _avatarState.agentAddress,
                _avatarState.address,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                tradableItem.TradableId,
                1,
                itemSubType
            );
            if (add)
            {
                _avatarState.inventory.AddNonFungibleItem(item);
            }

            if (item is IEquippableItem equippableItem)
            {
                equippableItem.Equip();
            }

            Assert.Equal(add, _avatarState.inventory.TryGetTradableItems(tradableItem.TradableId, order.StartedBlockIndex, 1, out _));

            if (exc is null)
            {
                ITradableItem result = order.Sell2(_avatarState);
                Assert.Equal(order.ExpiredBlockIndex, result.RequiredBlockIndex);
                if (result is IEquippableItem equippableItem1)
                {
                    Assert.False(equippableItem1.Equipped);
                }
            }
            else
            {
                Assert.Throws(exc, () => order.Sell2(_avatarState));
            }

            Assert.False(_avatarState.inventory.TryGetTradableItems(tradableItem.TradableId, order.StartedBlockIndex, 1, out _));
            Assert.Equal(add, _avatarState.inventory.TryGetTradableItems(tradableItem.TradableId, order.ExpiredBlockIndex, 1, out _));
        }

        [Theory]
        [InlineData(ItemSubType.Weapon, true, false, null)]
        [InlineData(ItemSubType.Weapon, true, true, typeof(ItemDoesNotExistException))]
        [InlineData(ItemSubType.Weapon, false, false, typeof(ItemDoesNotExistException))]
        [InlineData(ItemSubType.FullCostume, true, false, null)]
        [InlineData(ItemSubType.Food, false, false, typeof(ItemDoesNotExistException))]
        public void Sell2(ItemSubType itemSubType, bool add, bool isLock, Type exc)
        {
            var row = _tableSheets.ItemSheet.OrderedList.First(r => r.ItemSubType == itemSubType);
            ItemBase item = ItemFactory.CreateItem(row, new TestRandom());
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            ITradableItem tradableItem = (ITradableItem)item;
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                _avatarState.agentAddress,
                _avatarState.address,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                tradableItem.TradableId,
                1,
                itemSubType
            );

            var orderLock = new OrderLock(orderId);
            if (add)
            {
                if (isLock)
                {
                    _avatarState.inventory.AddNonFungibleItem(item, orderLock);
                }
                else
                {
                    _avatarState.inventory.AddNonFungibleItem(item);
                }
            }

            if (item is IEquippableItem equippableItem)
            {
                equippableItem.Equip();
            }

            Assert.Equal(add && !isLock, _avatarState.inventory.TryGetNonFungibleItem(tradableItem.TradableId, out _));

            if (exc is null)
            {
                ITradableItem result = order.Sell(_avatarState);
                Assert.Equal(order.ExpiredBlockIndex, result.RequiredBlockIndex);
                if (result is IEquippableItem equippableItem1)
                {
                    Assert.False(equippableItem1.Equipped);
                }

                Assert.True(_avatarState.inventory.TryGetLockedItem(new OrderLock(orderId), out Inventory.Item inventoryItem));
                Assert.Equal(result, (ITradableItem)inventoryItem.item);
            }
            else
            {
                Assert.Throws(exc, () => order.Sell(_avatarState));
            }
        }

        [Theory]
        [InlineData(ItemSubType.Weapon, true, null)]
        [InlineData(ItemSubType.Weapon, false, typeof(ItemDoesNotExistException))]
        [InlineData(ItemSubType.FullCostume, true, null)]
        [InlineData(ItemSubType.Food, false, typeof(ItemDoesNotExistException))]
        public void Digest(ItemSubType itemSubType, bool add, Type exc)
        {
            var row = _tableSheets.ItemSheet.OrderedList.First(r => r.ItemSubType == itemSubType);
            ItemBase item = ItemFactory.CreateItem(row, new TestRandom());
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            ITradableItem tradableItem = (ITradableItem)item;
            tradableItem.RequiredBlockIndex = 1;
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                _avatarState.agentAddress,
                _avatarState.address,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                tradableItem.TradableId,
                1,
                itemSubType
            );

            if (add)
            {
                _avatarState.inventory.AddNonFungibleItem(item);
            }

            if (exc is null)
            {
                int cp = CPHelper.GetCP(tradableItem, _tableSheets.CostumeStatSheet);
                Assert.True(cp > 0);
                OrderDigest digest = order.Digest2(_avatarState, _tableSheets.CostumeStatSheet);

                Assert.Equal(orderId, digest.OrderId);
                Assert.Equal(order.StartedBlockIndex, digest.StartedBlockIndex);
                Assert.Equal(order.ExpiredBlockIndex, digest.ExpiredBlockIndex);
                Assert.Equal(order.Price, digest.Price);
                Assert.Equal(item.Id, digest.ItemId);
                Assert.Equal(cp, digest.CombatPoint);
                Assert.Equal(0, digest.Level);
            }
            else
            {
                Assert.Throws(exc, () => order.Digest2(_avatarState, _tableSheets.CostumeStatSheet));
            }
        }

        [Theory]
        [InlineData(false, false, false, false, ItemSubType.Weapon, ItemSubType.Weapon, typeof(InvalidAddressException))]
        [InlineData(true, false, false, false, ItemSubType.Weapon, ItemSubType.Weapon, typeof(InvalidAddressException))]
        [InlineData(true, true, false, false, ItemSubType.Weapon, ItemSubType.Weapon, typeof(InvalidTradableIdException))]
        [InlineData(true, true, true, false, ItemSubType.Weapon, ItemSubType.Weapon, typeof(ItemDoesNotExistException))]
        [InlineData(true, true, true, true, ItemSubType.Weapon, ItemSubType.Armor, typeof(InvalidItemTypeException))]
        [InlineData(true, true, true, true, ItemSubType.Armor, ItemSubType.Armor, null)]
        public void ValidateCancelOrder(
            bool useAgentAddress,
            bool useAvatarAddress,
            bool useTradableId,
            bool add,
            ItemSubType itemSubType,
            ItemSubType orderItemSubType,
            Type exc
        )
        {
            var row = _tableSheets.ItemSheet.OrderedList.First(r => r.ItemSubType == itemSubType);
            ItemBase item = ItemFactory.CreateItem(row, new TestRandom());
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            ITradableItem tradableItem = (ITradableItem)item;
            tradableItem.RequiredBlockIndex = 1;
            var agentAddress = useAgentAddress ? _avatarState.agentAddress : default;
            var avatarAddress = useAvatarAddress ? _avatarState.address : default;
            var tradableId = useTradableId ? tradableItem.TradableId : default;
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                agentAddress,
                avatarAddress,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                tradableItem.TradableId,
                1,
                orderItemSubType
            );
            if (add)
            {
                _avatarState.inventory.AddNonFungibleItem(item);
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
        [InlineData(false, false, false, false, ItemSubType.Weapon, ItemSubType.Weapon, typeof(InvalidAddressException))]
        [InlineData(true, false, false, false, ItemSubType.Weapon, ItemSubType.Weapon, typeof(InvalidAddressException))]
        [InlineData(true, true, false, false, ItemSubType.Weapon, ItemSubType.Weapon, typeof(InvalidTradableIdException))]
        [InlineData(true, true, true, false, ItemSubType.Weapon, ItemSubType.Weapon, typeof(ItemDoesNotExistException))]
        [InlineData(true, true, true, true, ItemSubType.Weapon, ItemSubType.Armor, typeof(InvalidItemTypeException))]
        [InlineData(true, true, true, true, ItemSubType.Armor, ItemSubType.Armor, null)]
        public void ValidateCancelOrder2(
            bool useAgentAddress,
            bool useAvatarAddress,
            bool useTradableId,
            bool isLock,
            ItemSubType itemSubType,
            ItemSubType orderItemSubType,
            Type exc
        )
        {
            var row = _tableSheets.ItemSheet.OrderedList.First(r => r.ItemSubType == itemSubType);
            ItemBase item = ItemFactory.CreateItem(row, new TestRandom());
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            ITradableItem tradableItem = (ITradableItem)item;
            tradableItem.RequiredBlockIndex = 1;
            var agentAddress = useAgentAddress ? _avatarState.agentAddress : default;
            var avatarAddress = useAvatarAddress ? _avatarState.address : default;
            var tradableId = useTradableId ? tradableItem.TradableId : default;
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                agentAddress,
                avatarAddress,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                tradableItem.TradableId,
                1,
                orderItemSubType
            );
            OrderLock? orderLock = null;
            if (isLock)
            {
                orderLock = new OrderLock(orderId);
            }

            _avatarState.inventory.AddNonFungibleItem(item, orderLock);

            Assert.Equal(isLock, _avatarState.inventory.TryGetLockedItem(new OrderLock(orderId), out _));

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
        [InlineData(ItemSubType.Weapon, 1, true, null)]
        [InlineData(ItemSubType.Weapon, 0, false, typeof(ItemDoesNotExistException))]
        [InlineData(ItemSubType.FullCostume, 2, true, null)]
        public void Cancel(ItemSubType itemSubType, long blockIndex, bool add, Type exc)
        {
            var row = _tableSheets.ItemSheet.OrderedList.First(r => r.ItemSubType == itemSubType);
            ItemBase item = ItemFactory.CreateItem(row, new TestRandom());
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            ITradableItem tradableItem = (ITradableItem)item;
            tradableItem.RequiredBlockIndex = 1;
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                _avatarState.agentAddress,
                _avatarState.address,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                tradableItem.TradableId,
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
                Assert.True(
                    _avatarState.inventory.TryGetNonFungibleItem(
                        tradableItem.TradableId,
                        out INonFungibleItem nonFungibleItem
                    )
                );
                Assert.Equal(order.ExpiredBlockIndex, nonFungibleItem.RequiredBlockIndex);

                ITradableItem result = order.Cancel2(_avatarState, blockIndex);

                Assert.Equal(blockIndex, result.RequiredBlockIndex);
                Assert.Equal(itemSubType, result.ItemSubType);
                Assert.Equal(tradableItem.TradableId, result.TradableId);
                Assert.True(
                    _avatarState.inventory.TryGetNonFungibleItem(
                        tradableItem.TradableId,
                        out INonFungibleItem nextNonFungibleItem
                    )
                );
                Assert.Equal(blockIndex, nextNonFungibleItem.RequiredBlockIndex);
            }
            else
            {
                Assert.Throws(exc, () => order.Cancel2(_avatarState, blockIndex));
            }
        }

        [Theory]
        [InlineData(ItemSubType.Weapon, 1, true, null)]
        [InlineData(ItemSubType.Weapon, 0, false, typeof(ItemDoesNotExistException))]
        [InlineData(ItemSubType.FullCostume, 2, true, null)]
        public void Cancel2(ItemSubType itemSubType, long blockIndex, bool isLock, Type exc)
        {
            var row = _tableSheets.ItemSheet.OrderedList.First(r => r.ItemSubType == itemSubType);
            ItemBase item = ItemFactory.CreateItem(row, new TestRandom());
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            ITradableItem tradableItem = (ITradableItem)item;
            tradableItem.RequiredBlockIndex = 1;
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                _avatarState.agentAddress,
                _avatarState.address,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                tradableItem.TradableId,
                1,
                itemSubType
            );

            _avatarState.inventory.AddNonFungibleItem(item);

            if (isLock)
            {
                order.Sell(_avatarState);
            }

            if (exc is null)
            {
                var orderLock = new OrderLock(orderId);
                Assert.True(
                    _avatarState.inventory.TryGetLockedItem(orderLock, out Inventory.Item inventoryItem)
                );
                var nonFungibleItem = (INonFungibleItem)inventoryItem.item;
                Assert.Equal(order.ExpiredBlockIndex, nonFungibleItem.RequiredBlockIndex);

                ITradableItem result = order.Cancel(_avatarState, blockIndex);

                Assert.Equal(blockIndex, result.RequiredBlockIndex);
                Assert.Equal(itemSubType, result.ItemSubType);
                Assert.Equal(tradableItem.TradableId, result.TradableId);
                Assert.True(
                    _avatarState.inventory.TryGetNonFungibleItem(
                        tradableItem.TradableId,
                        out INonFungibleItem nextNonFungibleItem
                    )
                );
                Assert.Equal(blockIndex, nextNonFungibleItem.RequiredBlockIndex);
                Assert.False(_avatarState.inventory.TryGetLockedItem(orderLock, out _));
            }
            else
            {
                Assert.Throws(exc, () => order.Cancel(_avatarState, blockIndex));
            }
        }

        [Theory]
        [InlineData(true, false, false, true, true, true, false, Buy.ErrorCodeInvalidAddress)]
        [InlineData(true, true, false, true, true, true, false, Buy.ErrorCodeInvalidAddress)]
        [InlineData(true, false, true, true, true, true, false, Buy.ErrorCodeInvalidAddress)]
        [InlineData(true, true, true, false, true, true, false, Buy.ErrorCodeInvalidTradableId)]
        [InlineData(true, true, true, true, false, true, false, Buy.ErrorCodeInvalidPrice)]
        [InlineData(true, true, true, true, true, true, true, Buy.ErrorCodeShopItemExpired)]
        [InlineData(true, true, true, true, true, false, false, Buy.ErrorCodeItemDoesNotExist)]
        [InlineData(false, true, true, true, true, true, false, Buy.ErrorCodeInvalidItemType)]
        [InlineData(true, true, true, true, true, true, false, 0)]
        public void ValidateTransfer(
            bool equalItemSubtype,
            bool useAgentAddress,
            bool useAvatarAddress,
            bool useTradableId,
            bool usePrice,
            bool add,
            bool expire,
            int expected
        )
        {
            var row = _tableSheets.ItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Weapon);
            ItemBase item = ItemFactory.CreateItem(row, new TestRandom());
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            ITradableItem tradableItem = (ITradableItem)item;
            tradableItem.RequiredBlockIndex = 1;
            var agentAddress = useAgentAddress ? _avatarState.agentAddress : default;
            var avatarAddress = useAvatarAddress ? _avatarState.address : default;
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                agentAddress,
                avatarAddress,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                tradableItem.TradableId,
                1,
                equalItemSubtype ? ItemSubType.Weapon : ItemSubType.Armor
            );
            FungibleAssetValue price = usePrice ? order.Price : _currency * 0;
            Guid tradableId = useTradableId ? tradableItem.TradableId : default;
            if (add)
            {
                _avatarState.inventory.AddNonFungibleItem(item);
                order.Sell2(_avatarState);
            }

            long blockIndex = expire ? order.ExpiredBlockIndex + 1 : order.ExpiredBlockIndex;

            Assert.Equal(expected, order.ValidateTransfer2(_avatarState, tradableId, price, blockIndex));
        }

        [Theory]
        [InlineData(true, false, false, true, true, true, false, Buy.ErrorCodeInvalidAddress)]
        [InlineData(true, true, false, true, true, true, false, Buy.ErrorCodeInvalidAddress)]
        [InlineData(true, false, true, true, true, true, false, Buy.ErrorCodeInvalidAddress)]
        [InlineData(true, true, true, false, true, true, false, Buy.ErrorCodeInvalidTradableId)]
        [InlineData(true, true, true, true, false, true, false, Buy.ErrorCodeInvalidPrice)]
        [InlineData(true, true, true, true, true, true, true, Buy.ErrorCodeShopItemExpired)]
        [InlineData(true, true, true, true, true, false, false, Buy.ErrorCodeItemDoesNotExist)]
        [InlineData(false, true, true, true, true, true, false, Buy.ErrorCodeInvalidItemType)]
        [InlineData(true, true, true, true, true, true, false, 0)]
        public void ValidateTransfer2(
            bool equalItemSubtype,
            bool useAgentAddress,
            bool useAvatarAddress,
            bool useTradableId,
            bool usePrice,
            bool add,
            bool expire,
            int expected
        )
        {
            var row = _tableSheets.ItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Weapon);
            ItemBase item = ItemFactory.CreateItem(row, new TestRandom());
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            ITradableItem tradableItem = (ITradableItem)item;
            tradableItem.RequiredBlockIndex = 1;
            var agentAddress = useAgentAddress ? _avatarState.agentAddress : default;
            var avatarAddress = useAvatarAddress ? _avatarState.address : default;
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                agentAddress,
                avatarAddress,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                tradableItem.TradableId,
                1,
                equalItemSubtype ? ItemSubType.Weapon : ItemSubType.Armor
            );
            FungibleAssetValue price = usePrice ? order.Price : _currency * 0;
            Guid tradableId = useTradableId ? tradableItem.TradableId : default;
            if (add)
            {
                _avatarState.inventory.AddNonFungibleItem(item);
                order.Sell(_avatarState);
            }

            long blockIndex = expire ? order.ExpiredBlockIndex + 1 : order.ExpiredBlockIndex;

            Assert.Equal(expected, order.ValidateTransfer(_avatarState, tradableId, price, blockIndex));
        }

        [Theory]
        [InlineData(false, typeof(ItemDoesNotExistException))]
        [InlineData(true, null)]
        public void Transfer(bool add, Type exc)
        {
            var row = _tableSheets.ItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Weapon);
            ItemBase item = ItemFactory.CreateItem(row, new TestRandom());
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            ITradableItem tradableItem = (ITradableItem)item;
            tradableItem.RequiredBlockIndex = 1;
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                _avatarState.agentAddress,
                _avatarState.address,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                tradableItem.TradableId,
                1,
                ItemSubType.Weapon
            );

            if (add)
            {
                _avatarState.inventory.AddNonFungibleItem(item);
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
                Assert.False(_avatarState.inventory.TryGetNonFungibleItem(order.TradableId, out _));
                Assert.True(buyer.inventory.TryGetNonFungibleItem(order.TradableId, out INonFungibleItem result));
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
            var row = _tableSheets.ItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Weapon);
            ItemBase item = ItemFactory.CreateItem(row, new TestRandom());
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            ITradableItem tradableItem = (ITradableItem)item;
            tradableItem.RequiredBlockIndex = 1;
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                _avatarState.agentAddress,
                _avatarState.address,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                tradableItem.TradableId,
                1,
                ItemSubType.Weapon
            );

            if (add)
            {
                _avatarState.inventory.AddNonFungibleItem(item);
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
                Assert.False(_avatarState.inventory.TryGetNonFungibleItem(order.TradableId, out _));
                Assert.True(buyer.inventory.TryGetNonFungibleItem(order.TradableId, out INonFungibleItem result));
                Assert.Equal(100, result.RequiredBlockIndex);
            }
            else
            {
                Assert.Throws(exc, () => order.Transfer2(_avatarState, buyer, 0));
            }
        }

#pragma warning disable SA1204
        public static IEnumerable<object[]> ValidateMemberData() => new List<object[]>
        {
            new object[]
            {
                1,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                true,
                ItemSubType.Weapon,
                ItemSubType.Weapon,
                null,
            },
            new object[]
            {
                1,
                0,
                default,
                default,
                false,
                ItemSubType.Weapon,
                ItemSubType.Weapon,
                typeof(InvalidAddressException),
            },
            new object[]
            {
                0,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                false,
                ItemSubType.Food,
                ItemSubType.Food,
                typeof(InvalidItemCountException),
            },
            new object[]
            {
                -1,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                false,
                ItemSubType.Food,
                ItemSubType.Food,
                typeof(InvalidItemCountException),
            },
            new object[]
            {
                2,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                false,
                ItemSubType.FullCostume,
                ItemSubType.FullCostume,
                typeof(InvalidItemCountException),
            },
            new object[]
            {
                1,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                false,
                ItemSubType.Weapon,
                ItemSubType.Weapon,
                typeof(ItemDoesNotExistException),
            },
            new object[]
            {
                1,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                true,
                ItemSubType.Weapon,
                ItemSubType.Food,
                typeof(InvalidItemTypeException),
            },
            new object[]
            {
                1,
                100,
                Addresses.Admin,
                Addresses.Blacksmith,
                true,
                ItemSubType.Weapon,
                ItemSubType.Weapon,
                typeof(RequiredBlockIndexException),
            },
        };
#pragma warning restore SA1204
    }
}
