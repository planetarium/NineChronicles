namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class Sell9Test
    {
        private const long ProductPrice = 100;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Currency _currency;
        private readonly AvatarState _avatarState;
        private readonly TableSheets _tableSheets;
        private IAccountStateDelta _initialState;

        public Sell9Test(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new State();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(_currency);

            var shopState = new ShopState();

            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);
            _avatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            agentState.avatarAddresses[0] = _avatarAddress;

            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, goldCurrencyState.Serialize())
                .SetState(Addresses.Shop, shopState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize());
        }

        [Theory]
        [InlineData(ItemType.Consumable, 1, true)]
        [InlineData(ItemType.Costume, 1, false)]
        [InlineData(ItemType.Equipment, 1, true)]
        [InlineData(ItemType.Material, 1, false)]
        public void Execute(
            ItemType itemType,
            int itemCount,
            bool backward
        )
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);

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

            Assert.Equal(0, tradableItem.RequiredBlockIndex);
            avatarState.inventory.AddItem2((ItemBase)tradableItem, itemCount);

            var previousStates = _initialState;
            if (backward)
            {
                previousStates = previousStates.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                previousStates = previousStates
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(_avatarAddress, avatarState.SerializeV2());
            }

            var currencyState = previousStates.GetGoldCurrency();
            var price = new FungibleAssetValue(currencyState, ProductPrice, 0);
            var orderId = new Guid("6f460c1a755d48e4ad6765d5f519dbc8");
            var orderAddress = Order.DeriveAddress(orderId);
            var shardedShopAddress = ShardedShopStateV2.DeriveAddress(
                tradableItem.ItemSubType,
                orderId);
            long blockIndex = 1;
            Assert.Null(previousStates.GetState(shardedShopAddress));

            var sellAction = new Sell9
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = tradableItem.TradableId,
                count = itemCount,
                price = price,
                itemSubType = tradableItem.ItemSubType,
                orderId = orderId,
            };
            var nextState = sellAction.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = previousStates,
                Rehearsal = false,
                Signer = _agentAddress,
                Random = new TestRandom(),
            });

            long expiredBlockIndex = Order.ExpirationInterval + blockIndex;

            // Check AvatarState and Inventory
            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            Assert.Single(nextAvatarState.inventory.Items);
            Assert.True(nextAvatarState.inventory.TryGetLockedItem(new OrderLock(orderId), out var inventoryItem));
            Assert.False(nextAvatarState.inventory.TryGetTradableItems(tradableItem.TradableId, blockIndex, itemCount, out _));
            Assert.False(nextAvatarState.inventory.TryGetTradableItems(tradableItem.TradableId, expiredBlockIndex, itemCount, out _));
            ITradableItem nextTradableItem = (ITradableItem)inventoryItem.item;
            Assert.Equal(expiredBlockIndex, nextTradableItem.RequiredBlockIndex);

            // Check ShardedShopState
            var nextSerializedShardedShopState = nextState.GetState(shardedShopAddress);
            Assert.NotNull(nextSerializedShardedShopState);
            var nextShardedShopState =
                new ShardedShopStateV2((Dictionary)nextSerializedShardedShopState);
            Assert.Single(nextShardedShopState.OrderDigestList);
            var orderDigest = nextShardedShopState.OrderDigestList.First(o => o.OrderId.Equals(orderId));
            Assert.Equal(price, orderDigest.Price);
            Assert.Equal(blockIndex, orderDigest.StartedBlockIndex);
            Assert.Equal(expiredBlockIndex, orderDigest.ExpiredBlockIndex);
            Assert.Equal(((ItemBase)tradableItem).Id, orderDigest.ItemId);
            Assert.Equal(tradableItem.TradableId, orderDigest.TradableId);

            var serializedOrder = nextState.GetState(orderAddress);
            Assert.NotNull(serializedOrder);
            var serializedItem = nextState.GetState(Addresses.GetItemAddress(tradableItem.TradableId));
            Assert.NotNull(serializedItem);

            var order = OrderFactory.Deserialize((Dictionary)serializedOrder);
            ITradableItem orderItem = (ITradableItem)ItemFactory.Deserialize((Dictionary)serializedItem);

            Assert.Equal(price, order.Price);
            Assert.Equal(orderId, order.OrderId);
            Assert.Equal(tradableItem.TradableId, order.TradableId);
            Assert.Equal(blockIndex, order.StartedBlockIndex);
            Assert.Equal(expiredBlockIndex, order.ExpiredBlockIndex);
            Assert.Equal(_agentAddress, order.SellerAgentAddress);
            Assert.Equal(_avatarAddress, order.SellerAvatarAddress);
            Assert.Equal(expiredBlockIndex, orderItem.RequiredBlockIndex);

            var receiptDict = nextState.GetState(OrderDigestListState.DeriveAddress(_avatarAddress));
            Assert.NotNull(receiptDict);
            var orderDigestList = new OrderDigestListState((Dictionary)receiptDict);
            Assert.Single(orderDigestList.OrderDigestList);
            OrderDigest orderDigest2 = orderDigestList.OrderDigestList.First();
            Assert.Equal(orderDigest, orderDigest2);
        }

        [Fact]
        public void Execute_Throw_InvalidPriceException()
        {
            var action = new Sell9
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = default,
                count = 1,
                price = -1 * _currency,
                itemSubType = default,
                orderId = default,
            };

            Assert.Throws<InvalidPriceException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Signer = _agentAddress,
            }));
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException()
        {
            var action = new Sell9
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = default,
                count = 1,
                price = 0 * _currency,
                itemSubType = ItemSubType.Food,
                orderId = default,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = new State(),
                Signer = _agentAddress,
            }));
        }

        [Fact]
        public void Execute_Throw_NotEnoughClearedStageLevelException()
        {
            var avatarState = new AvatarState(_avatarState)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    0
                ),
            };

            _initialState = _initialState.SetState(_avatarAddress, avatarState.Serialize());

            var action = new Sell9
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = default,
                count = 1,
                price = 0 * _currency,
                itemSubType = ItemSubType.Food,
                orderId = default,
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Signer = _agentAddress,
            }));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_Throw_ItemDoesNotExistException(bool isLock)
        {
            var tradableId = Guid.NewGuid();
            if (isLock)
            {
                var tradableItem = ItemFactory.CreateItemUsable(
                    _tableSheets.EquipmentItemSheet.First,
                    tradableId,
                    0);
                var orderLock = new OrderLock(Guid.NewGuid());
                _avatarState.inventory.AddItem2(tradableItem, 1, orderLock);
                Assert.True(_avatarState.inventory.TryGetLockedItem(orderLock, out _));
                _initialState = _initialState.SetState(
                    _avatarAddress.Derive(LegacyInventoryKey),
                    _avatarState.inventory.Serialize()
                );
            }

            var action = new Sell9
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = tradableId,
                count = 1,
                price = 0 * _currency,
                itemSubType = ItemSubType.Weapon,
                orderId = default,
            };

            Assert.Throws<ItemDoesNotExistException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_Throw_InvalidItemTypeException()
        {
            var equipmentId = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                equipmentId,
                10);
            _avatarState.inventory.AddItem2(equipment);

            _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());

            var action = new Sell9
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = equipmentId,
                count = 1,
                price = 0 * _currency,
                itemSubType = ItemSubType.Food,
                orderId = default,
            };

            Assert.Throws<InvalidItemTypeException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 11,
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_Throw_DuplicateOrderIdException()
        {
            ITradableItem tradableItem = (ITradableItem)ItemFactory.CreateItem(
                _tableSheets.EquipmentItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Weapon),
                new TestRandom());
            AvatarState avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.inventory.AddItem2((ItemBase)tradableItem);

            Guid tradableId = tradableItem.TradableId;
            Guid orderId = Guid.NewGuid();
            var shardedShopAddress = ShardedShopStateV2.DeriveAddress(
                ItemSubType.Weapon,
                orderId);
            var shardedShopState = new ShardedShopStateV2(shardedShopAddress);
            var order = OrderFactory.Create(
                _agentAddress,
                _avatarAddress,
                orderId,
                _currency * 0,
                tradableId,
                0,
                ItemSubType.Weapon,
                1
            );
            shardedShopState.Add(order.Digest2(avatarState, _tableSheets.CostumeStatSheet), 1);
            Assert.Single(shardedShopState.OrderDigestList);

            IAccountStateDelta previousStates = _initialState
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(shardedShopAddress, shardedShopState.Serialize());

            var action = new Sell9
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = tradableId,
                count = 1,
                price = 0 * _currency,
                itemSubType = ItemSubType.Weapon,
                orderId = orderId,
            };

            Assert.Throws<DuplicateOrderIdException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 1,
                PreviousStates = previousStates,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Rehearsal()
        {
            Guid tradableId = Guid.NewGuid();
            Guid orderId = Guid.NewGuid();
            var action = new Sell9
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = tradableId,
                count = 1,
                price = _currency * ProductPrice,
                itemSubType = ItemSubType.Weapon,
                orderId = orderId,
            };

            var updatedAddresses = new List<Address>()
            {
                _agentAddress,
                _avatarAddress,
                _avatarAddress.Derive(LegacyInventoryKey),
                _avatarAddress.Derive(LegacyWorldInformationKey),
                _avatarAddress.Derive(LegacyQuestListKey),
                Addresses.GetItemAddress(tradableId),
                Order.DeriveAddress(orderId),
                ShardedShopStateV2.DeriveAddress(ItemSubType.Weapon, orderId),
                OrderDigestListState.DeriveAddress(_avatarAddress),
            };

            var state = new State();

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                BlockIndex = 0,
                Rehearsal = true,
            });

            Assert.Equal(updatedAddresses.ToImmutableHashSet(), nextState.UpdatedAddresses);
        }
    }
}
