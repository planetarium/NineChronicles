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
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class SellCancellation8Test
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly TableSheets _tableSheets;

        public SellCancellation8Test(ITestOutputHelper outputHelper)
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
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            _goldCurrencyState = new GoldCurrencyState(currency);

            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);
            _avatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
            var avatarState = new AvatarState(
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
                .SetState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(Addresses.Shop, new ShopState().Serialize())
                .SetState(_avatarAddress, avatarState.Serialize());
        }

        [Theory]
        [InlineData(ItemType.Equipment, "F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4", 1, 1, 1, true)]
        [InlineData(ItemType.Costume, "936DA01F-9ABD-4d9d-80C7-02AF85C822A8", 1, 1, 1, true)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 1, 1, 1, true)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 2, 1, 2, true)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 2, 2, 3, true)]
        [InlineData(ItemType.Equipment, "F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4", 1, 1, 1, false)]
        [InlineData(ItemType.Costume, "936DA01F-9ABD-4d9d-80C7-02AF85C822A8", 1, 1, 1, false)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 1, 1, 1, false)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 2, 1, 2, false)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 2, 2, 3, false)]
        public void Execute(
            ItemType itemType,
            string guid,
            int itemCount,
            int inventoryCount,
            int expectedCount,
            bool fromPreviousAction
        )
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            ITradableItem tradableItem;
            Guid itemId = new Guid(guid);
            Guid orderId = Guid.NewGuid();
            ItemSubType itemSubType;
            const long requiredBlockIndex = Order.ExpirationInterval;
            if (itemType == ItemType.Equipment)
            {
                var itemUsable = ItemFactory.CreateItemUsable(
                    _tableSheets.EquipmentItemSheet.First,
                    itemId,
                    requiredBlockIndex);
                tradableItem = itemUsable;
                itemSubType = itemUsable.ItemSubType;
            }
            else if (itemType == ItemType.Costume)
            {
                var costume = ItemFactory.CreateCostume(_tableSheets.CostumeItemSheet.First, itemId);
                costume.Update(requiredBlockIndex);
                tradableItem = costume;
                itemSubType = costume.ItemSubType;
            }
            else
            {
                var material = ItemFactory.CreateTradableMaterial(
                    _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Hourglass));
                itemSubType = material.ItemSubType;
                material.RequiredBlockIndex = requiredBlockIndex;
                tradableItem = material;
            }

            var result = new DailyReward2.DailyRewardResult()
            {
                id = default,
                materials = new Dictionary<Material, int>(),
            };

            for (var i = 0; i < 100; i++)
            {
                var mail = new DailyRewardMail(result, i, default, 0);
                avatarState.Update2(mail);
            }

            Address shardedShopAddress = ShardedShopStateV2.DeriveAddress(itemSubType, orderId);
            ShardedShopStateV2 shopState = new ShardedShopStateV2(shardedShopAddress);
            Order order = OrderFactory.Create(
                _agentAddress,
                _avatarAddress,
                orderId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                tradableItem.TradableId,
                requiredBlockIndex,
                itemSubType,
                itemCount
            );

            var expirationMail = new OrderExpirationMail(
                101,
                orderId,
                order.ExpiredBlockIndex,
                orderId
            );
            avatarState.mailBox.Add(expirationMail);

            var orderDigestList = new OrderDigestListState(OrderDigestListState.DeriveAddress(_avatarAddress));
            IAccountStateDelta prevState = _initialState;

            if (inventoryCount > 1)
            {
                for (int i = 0; i < inventoryCount; i++)
                {
                    // Different RequiredBlockIndex for divide inventory slot.
                    if (tradableItem is ITradableFungibleItem tradableFungibleItem)
                    {
                        var tradable = (TradableMaterial)tradableFungibleItem.Clone();
                        tradable.RequiredBlockIndex = tradableItem.RequiredBlockIndex - i;
                        avatarState.inventory.AddItem(tradable, 2 - i);
                    }
                }
            }
            else
            {
                avatarState.inventory.AddItem((ItemBase)tradableItem, itemCount);
            }

            ITradableItem sellItem;
            sellItem = order.Sell(avatarState);
            OrderDigest orderDigest = order.Digest(avatarState, _tableSheets.CostumeStatSheet);
            shopState.Add(orderDigest, requiredBlockIndex);
            orderDigestList.Add(orderDigest);

            Assert.Equal(inventoryCount, avatarState.inventory.Items.Count);
            Assert.Equal(expectedCount, avatarState.inventory.Items.Sum(i => i.count));

            Assert.Single(shopState.OrderDigestList);
            Assert.Single(orderDigestList.OrderDigestList);

            Assert.Equal(requiredBlockIndex * 2, sellItem.RequiredBlockIndex);
            Assert.True(avatarState.inventory.TryGetLockedItem(new OrderLock(orderId), out var outItem));
            Assert.Equal(itemCount, outItem.count);

            if (fromPreviousAction)
            {
                prevState = prevState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                prevState = prevState
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(_avatarAddress, avatarState.SerializeV2());
            }

            prevState = prevState
                .SetState(Addresses.GetItemAddress(itemId), sellItem.Serialize())
                .SetState(Order.DeriveAddress(order.OrderId), order.Serialize())
                .SetState(orderDigestList.Address, orderDigestList.Serialize())
                .SetState(shardedShopAddress, shopState.Serialize());

            var sellCancellationAction = new SellCancellation8
            {
                orderId = orderId,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = itemSubType,
                tradableId = itemId,
            };
            var nextState = sellCancellationAction.Execute(new ActionContext
            {
                BlockIndex = 101,
                PreviousStates = prevState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _agentAddress,
            });

            ShardedShopStateV2 nextShopState = new ShardedShopStateV2((Dictionary)nextState.GetState(shardedShopAddress));
            Assert.Empty(nextShopState.OrderDigestList);

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            Assert.Equal(expectedCount, nextAvatarState.inventory.Items.Sum(i => i.count));
            Assert.False(nextAvatarState.inventory.TryGetTradableItems(
                itemId,
                0,
                itemCount,
                out List<Inventory.Item> _
            ));
            Assert.True(nextAvatarState.inventory.TryGetTradableItems(
                itemId,
                requiredBlockIndex,
                itemCount,
                out List<Inventory.Item> inventoryItems
            ));
            Assert.False(nextAvatarState.inventory.TryGetLockedItem(new OrderLock(orderId), out _));
            Assert.Equal(inventoryCount, inventoryItems.Count);
            Inventory.Item inventoryItem = inventoryItems.First();
            Assert.Equal(itemCount, inventoryItem.count);
            Assert.Equal(inventoryCount, nextAvatarState.inventory.Items.Count);
            ITradableItem nextTradableItem = (ITradableItem)inventoryItem.item;
            Assert.Equal(101, nextTradableItem.RequiredBlockIndex);
            Assert.Equal(30, nextAvatarState.mailBox.Count);
            Assert.Empty(nextAvatarState.mailBox.OfType<OrderExpirationMail>());
            var cancelMail = nextAvatarState.mailBox.OfType<CancelOrderMail>().First();
            Assert.Equal(orderId, cancelMail.OrderId);
            var nextReceiptList = new OrderDigestListState((Dictionary)nextState.GetState(orderDigestList.Address));
            Assert.Empty(nextReceiptList.OrderDigestList);

            var sellCancelItem = (ITradableItem)ItemFactory.Deserialize((Dictionary)nextState.GetState(Addresses.GetItemAddress(itemId)));
            Assert.Equal(101, sellCancelItem.RequiredBlockIndex);
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException()
        {
            var action = new SellCancellation8
            {
                orderId = default,
                sellerAvatarAddress = default,
                itemSubType = ItemSubType.Weapon,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = _initialState,
                    Random = new TestRandom(),
                    Signer = default,
                })
            );
        }

        [Fact]
        public void Execute_Throw_NotEnoughClearedStageLevelException()
        {
            var avatarState = new AvatarState(_initialState.GetAvatarState(_avatarAddress))
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    0
                ),
            };

            IAccountStateDelta prevState = _initialState.SetState(_avatarAddress, avatarState.Serialize());

            var action = new SellCancellation8
            {
                orderId = default,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = ItemSubType.Weapon,
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = prevState,
                Signer = _agentAddress,
            }));
        }

        [Theory]
        [InlineData(ItemType.Equipment)]
        [InlineData(ItemType.Consumable)]
        [InlineData(ItemType.Costume)]
        [InlineData(ItemType.Material)]
        public void Execute_Throw_ItemDoesNotExistException(ItemType itemType)
        {
            Guid orderId = Guid.NewGuid();
            ITradableItem tradableItem;
            ItemSheet.Row row;
            switch (itemType)
            {
                case ItemType.Consumable:
                    row = _tableSheets.ConsumableItemSheet.First;
                    break;
                case ItemType.Costume:
                    row = _tableSheets.CostumeItemSheet.First;
                    break;
                case ItemType.Equipment:
                    row = _tableSheets.EquipmentItemSheet.First;
                    break;
                case ItemType.Material:
                    row = _tableSheets.MaterialItemSheet.OrderedList
                        .First(r => r.ItemSubType == ItemSubType.Hourglass);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null);
            }

            if (itemType == ItemType.Material)
            {
                tradableItem = ItemFactory.CreateTradableMaterial((MaterialItemSheet.Row)row);
            }
            else
            {
                tradableItem = (ITradableItem)ItemFactory.CreateItem(row, new TestRandom());
            }

            tradableItem.RequiredBlockIndex = Sell6.ExpiredBlockIndex;

            Address shardedShopAddress = ShardedShopStateV2.DeriveAddress(tradableItem.ItemSubType, orderId);
            var shopState = new ShardedShopStateV2(shardedShopAddress);

            Order order = OrderFactory.Create(
                _agentAddress,
                _avatarAddress,
                orderId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                tradableItem.TradableId,
                0,
                tradableItem.ItemSubType,
                1
            );
            var orderDigest = new OrderDigest(
                _agentAddress,
                order.StartedBlockIndex,
                order.ExpiredBlockIndex,
                orderId,
                tradableItem.TradableId,
                order.Price,
                0,
                0,
                0,
                1
            );
            var orderDigestList = new OrderDigestListState(OrderDigestListState.DeriveAddress(_avatarAddress));
            orderDigestList.Add(orderDigest);

            IAccountStateDelta prevState = _initialState
                .SetState(Order.DeriveAddress(orderId), order.Serialize())
                .SetState(orderDigestList.Address, orderDigestList.Serialize())
                .SetState(shardedShopAddress, shopState.Serialize());

            var action = new SellCancellation8
            {
                orderId = orderId,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = tradableItem.ItemSubType,
                tradableId = tradableItem.TradableId,
            };

            ItemDoesNotExistException exc = Assert.Throws<ItemDoesNotExistException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = prevState,
                    Random = new TestRandom(),
                    Signer = _agentAddress,
                })
            );
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void Execute_Throw_InvalidAddressException(bool useAgentAddress, bool useAvatarAddress)
        {
            Guid orderId = Guid.NewGuid();
            ItemUsable itemUsable = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                Sell6.ExpiredBlockIndex);
            Address shardedShopAddress = ShardedShopStateV2.DeriveAddress(itemUsable.ItemSubType, orderId);
            var shopState = new ShardedShopStateV2(shardedShopAddress);

            Address agentAddress = useAgentAddress ? _agentAddress : default;
            Address avatarAddress = useAvatarAddress ? _avatarAddress : default;
            Order order = OrderFactory.Create(
                agentAddress,
                avatarAddress,
                orderId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                itemUsable.TradableId,
                0,
                itemUsable.ItemSubType,
                1
            );
            var orderDigest = new OrderDigest(
                _agentAddress,
                order.StartedBlockIndex,
                order.ExpiredBlockIndex,
                orderId,
                itemUsable.TradableId,
                order.Price,
                0,
                0,
                itemUsable.Id,
                1
            );
            shopState.Add(orderDigest, 0);
            var orderDigestList = new OrderDigestListState(OrderDigestListState.DeriveAddress(_avatarAddress));
            orderDigestList.Add(orderDigest);

            IAccountStateDelta prevState = _initialState
                .SetState(Order.DeriveAddress(orderId), order.Serialize())
                .SetState(orderDigestList.Address, orderDigestList.Serialize())
                .SetState(shardedShopAddress, shopState.Serialize());

            var action = new SellCancellation8
            {
                orderId = orderId,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = itemUsable.ItemSubType,
                tradableId = itemUsable.TradableId,
            };

            Assert.Throws<InvalidAddressException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = prevState,
                    Random = new TestRandom(),
                    Signer = _agentAddress,
                })
            );
        }

        [Fact]
        public void Rehearsal()
        {
            var action = new SellCancellation8()
            {
                sellerAvatarAddress = _avatarAddress,
                orderId = default,
                itemSubType = ItemSubType.Weapon,
                tradableId = default,
            };

            var updatedAddresses = new List<Address>()
            {
                _avatarAddress,
                _avatarAddress.Derive(LegacyInventoryKey),
                _avatarAddress.Derive(LegacyWorldInformationKey),
                _avatarAddress.Derive(LegacyQuestListKey),
                ShardedShopStateV2.DeriveAddress(ItemSubType.Weapon, default(Guid)),
                OrderDigestListState.DeriveAddress(_avatarAddress),
                Addresses.GetItemAddress(default),
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

        [Fact]
        public void PlainValue()
        {
            var action = new SellCancellation8
            {
                sellerAvatarAddress = _avatarAddress,
                orderId = Guid.NewGuid(),
                itemSubType = ItemSubType.Weapon,
                tradableId = Guid.NewGuid(),
            };

            var plainValue = action.PlainValue;

            var action2 = new SellCancellation8();
            action2.LoadPlainValue(plainValue);

            Assert.Equal(plainValue, action2.PlainValue);
        }

        [Theory]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 1, 1, 1, true)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 2, 1, 2, true)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 2, 2, 3, true)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 1, 1, 1, false)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 2, 1, 2, false)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 2, 2, 3, false)]
        public void Execute_ReconfigureFungibleItem(
            ItemType itemType,
            string guid,
            int itemCount,
            int inventoryCount,
            int expectedCount,
            bool fromPreviousAction
        )
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            ITradableItem tradableItem;
            Guid itemId = new Guid(guid);
            Guid orderId = Guid.NewGuid();
            ItemSubType itemSubType;
            const long requiredBlockIndex = Order.ExpirationInterval;
            if (itemType == ItemType.Equipment)
            {
                var itemUsable = ItemFactory.CreateItemUsable(
                    _tableSheets.EquipmentItemSheet.First,
                    itemId,
                    requiredBlockIndex);
                tradableItem = itemUsable;
                itemSubType = itemUsable.ItemSubType;
            }
            else if (itemType == ItemType.Costume)
            {
                var costume = ItemFactory.CreateCostume(_tableSheets.CostumeItemSheet.First, itemId);
                costume.Update(requiredBlockIndex);
                tradableItem = costume;
                itemSubType = costume.ItemSubType;
            }
            else
            {
                var material = ItemFactory.CreateTradableMaterial(
                    _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Hourglass));
                itemSubType = material.ItemSubType;
                material.RequiredBlockIndex = requiredBlockIndex;
                tradableItem = material;
            }

            var result = new DailyReward2.DailyRewardResult()
            {
                id = default,
                materials = new Dictionary<Material, int>(),
            };

            for (var i = 0; i < 100; i++)
            {
                var mail = new DailyRewardMail(result, i, default, 0);
                avatarState.Update2(mail);
            }

            Address shardedShopAddress = ShardedShopStateV2.DeriveAddress(itemSubType, orderId);
            ShardedShopStateV2 shopState = new ShardedShopStateV2(shardedShopAddress);
            Order order = OrderFactory.Create(
                _agentAddress,
                _avatarAddress,
                orderId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                tradableItem.TradableId,
                requiredBlockIndex,
                itemSubType,
                itemCount
            );

            var expirationMail = new OrderExpirationMail(
                101,
                orderId,
                order.ExpiredBlockIndex,
                orderId
            );
            avatarState.mailBox.Add(expirationMail);

            var orderDigestList = new OrderDigestListState(OrderDigestListState.DeriveAddress(_avatarAddress));
            IAccountStateDelta prevState = _initialState;

            if (inventoryCount > 1)
            {
                for (int i = 0; i < inventoryCount; i++)
                {
                    // Different RequiredBlockIndex for divide inventory slot.
                    if (tradableItem is ITradableFungibleItem tradableFungibleItem)
                    {
                        var tradable = (TradableMaterial)tradableFungibleItem.Clone();
                        tradable.RequiredBlockIndex = tradableItem.RequiredBlockIndex - i;
                        avatarState.inventory.AddItem(tradable, 2 - i);
                    }
                }
            }
            else
            {
                avatarState.inventory.AddItem((ItemBase)tradableItem, itemCount);
            }

            ITradableItem sellItem;
            sellItem = order.Sell(avatarState);
            OrderDigest orderDigest = order.Digest(avatarState, _tableSheets.CostumeStatSheet);
            shopState.Add(orderDigest, requiredBlockIndex);
            orderDigestList.Add(orderDigest);

            Assert.Equal(inventoryCount, avatarState.inventory.Items.Count);
            Assert.Equal(expectedCount, avatarState.inventory.Items.Sum(i => i.count));

            Assert.Single(shopState.OrderDigestList);
            Assert.Single(orderDigestList.OrderDigestList);

            Assert.Equal(requiredBlockIndex * 2, sellItem.RequiredBlockIndex);
            Assert.True(avatarState.inventory.TryGetLockedItem(new OrderLock(orderId), out var outItem));
            Assert.Equal(itemCount, outItem.count);

            if (fromPreviousAction)
            {
                prevState = prevState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                prevState = prevState
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(_avatarAddress, avatarState.SerializeV2());
            }

            prevState = prevState
                .SetState(Addresses.GetItemAddress(itemId), sellItem.Serialize())
                .SetState(Order.DeriveAddress(order.OrderId), order.Serialize())
                .SetState(orderDigestList.Address, orderDigestList.Serialize())
                .SetState(shardedShopAddress, shopState.Serialize());

            var sellCancellationAction = new SellCancellation8
            {
                orderId = orderId,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = itemSubType,
                tradableId = itemId,
            };
            var nextState = sellCancellationAction.Execute(new ActionContext
            {
                BlockIndex = 101,
                PreviousStates = prevState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _agentAddress,
            });

            ShardedShopStateV2 nextShopState = new ShardedShopStateV2((Dictionary)nextState.GetState(shardedShopAddress));
            Assert.Empty(nextShopState.OrderDigestList);

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            Assert.Equal(expectedCount, nextAvatarState.inventory.Items.Sum(i => i.count));
            Assert.False(nextAvatarState.inventory.TryGetTradableItems(
                itemId,
                0,
                itemCount,
                out List<Inventory.Item> _
            ));
            Assert.True(nextAvatarState.inventory.TryGetTradableItems(
                itemId,
                requiredBlockIndex,
                itemCount,
                out List<Inventory.Item> inventoryItems
            ));
            Assert.False(nextAvatarState.inventory.TryGetLockedItem(new OrderLock(orderId), out _));
            Assert.Equal(inventoryCount, inventoryItems.Count);
            Inventory.Item inventoryItem = inventoryItems.First();
            Assert.Equal(itemCount, inventoryItem.count);
            Assert.Equal(inventoryCount, nextAvatarState.inventory.Items.Count);
            ITradableItem nextTradableItem = (ITradableItem)inventoryItem.item;
            Assert.Equal(101, nextTradableItem.RequiredBlockIndex);
            Assert.Equal(30, nextAvatarState.mailBox.Count);
            Assert.Empty(nextAvatarState.mailBox.OfType<OrderExpirationMail>());
            var cancelMail = nextAvatarState.mailBox.OfType<CancelOrderMail>().First();
            Assert.Equal(orderId, cancelMail.OrderId);
            var nextReceiptList = new OrderDigestListState((Dictionary)nextState.GetState(orderDigestList.Address));
            Assert.Empty(nextReceiptList.OrderDigestList);

            var sellCancelItem = (ITradableItem)ItemFactory.Deserialize((Dictionary)nextState.GetState(Addresses.GetItemAddress(itemId)));
            Assert.Equal(101, sellCancelItem.RequiredBlockIndex);
        }
    }
}
