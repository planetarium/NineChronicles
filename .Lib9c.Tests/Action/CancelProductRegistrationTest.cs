namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.Market;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static SerializeKeys;

    public class CancelProductRegistrationTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly TableSheets _tableSheets;

        public CancelProductRegistrationTest(ITestOutputHelper outputHelper)
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
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void Execute_Throw_InvalidAddressException(
            bool invalidAvatarAddress,
            bool invalidAgentAddress
        )
        {
            var action = new CancelProductRegistration
            {
                AvatarAddress = _avatarAddress,
                ProductInfos = new List<ProductInfo>
                {
                    new ProductInfo
                    {
                        AvatarAddress = _avatarAddress,
                        AgentAddress = _agentAddress,
                        Legacy = false,
                        Price = 1 * _goldCurrencyState.Currency,
                        ProductId = Guid.NewGuid(),
                        Type = ProductType.NonFungible,
                    },
                    new ProductInfo
                    {
                        AvatarAddress = invalidAvatarAddress
                            ? new PrivateKey().ToAddress()
                            : _avatarAddress,
                        AgentAddress = invalidAgentAddress
                            ? new PrivateKey().ToAddress()
                            : _agentAddress,
                        Legacy = false,
                        Price = 1 * _goldCurrencyState.Currency,
                        ProductId = Guid.NewGuid(),
                        Type = ProductType.Fungible,
                    },
                },
            };

            var actionContext = new ActionContext
            {
                Signer = _agentAddress,
                BlockIndex = 1L,
                PreviousStates = _initialState,
                Random = new TestRandom(),
            };
            Assert.Throws<InvalidAddressException>(() => action.Execute(actionContext));
        }

        [Fact]
        public void Execute_Throw_ProductNotFoundException()
        {
            var prevState = _initialState.MintAsset(_avatarAddress, 1 * RuneHelper.StakeRune);
            var registerProduct = new RegisterProduct
            {
                RegisterInfos = new List<IRegisterInfo>
                {
                    new AssetInfo
                    {
                        AvatarAddress = _avatarAddress,
                        Price = 1 * _goldCurrencyState.Currency,
                        Type = ProductType.FungibleAssetValue,
                        Asset = 1 * RuneHelper.StakeRune,
                    },
                },
            };
            var nexState = registerProduct.Execute(new ActionContext
            {
                PreviousStates = prevState,
                BlockIndex = 1L,
                Signer = _agentAddress,
                Random = new TestRandom(),
            });
            Assert.Equal(
                0 * RuneHelper.StakeRune,
                nexState.GetBalance(_avatarAddress, RuneHelper.StakeRune)
            );
            var productsState =
                new ProductsState(
                    (List)nexState.GetState(ProductsState.DeriveAddress(_avatarAddress)));
            var productId = Assert.Single(productsState.ProductIds);

            var action = new CancelProductRegistration
            {
                AvatarAddress = _avatarAddress,
                ProductInfos = new List<ProductInfo>
                {
                    new ProductInfo
                    {
                        AgentAddress = _agentAddress,
                        AvatarAddress = _avatarAddress,
                        Legacy = false,
                        Price = 1 * _goldCurrencyState.Currency,
                        ProductId = productId,
                        Type = ProductType.FungibleAssetValue,
                    },
                    new ProductInfo
                    {
                        AgentAddress = _agentAddress,
                        AvatarAddress = _avatarAddress,
                        Legacy = false,
                        Price = 1 * _goldCurrencyState.Currency,
                        ProductId = productId,
                        Type = ProductType.FungibleAssetValue,
                    },
                },
            };

            Assert.Throws<ProductNotFoundException>(() => action.Execute(new ActionContext
            {
                PreviousStates = nexState,
                BlockIndex = 2L,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
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
        public void Execute_BackwardCompatibility(
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
                var costume =
                    ItemFactory.CreateCostume(_tableSheets.CostumeItemSheet.First, itemId);
                costume.Update(requiredBlockIndex);
                tradableItem = costume;
                itemSubType = costume.ItemSubType;
            }
            else
            {
                var material = ItemFactory.CreateTradableMaterial(
                    _tableSheets.MaterialItemSheet.OrderedList.First(r =>
                        r.ItemSubType == ItemSubType.Hourglass));
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

            var orderDigestList =
                new OrderDigestListState(OrderDigestListState.DeriveAddress(_avatarAddress));
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
            Assert.True(
                avatarState.inventory.TryGetLockedItem(new OrderLock(orderId), out var outItem));
            Assert.Equal(itemCount, outItem.count);

            if (fromPreviousAction)
            {
                prevState = prevState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                prevState = prevState
                    .SetState(
                        _avatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize())
                    .SetState(_avatarAddress, avatarState.SerializeV2());
            }

            prevState = prevState
                .SetState(Addresses.GetItemAddress(itemId), sellItem.Serialize())
                .SetState(Order.DeriveAddress(order.OrderId), order.Serialize())
                .SetState(orderDigestList.Address, orderDigestList.Serialize())
                .SetState(shardedShopAddress, shopState.Serialize());

            var sellCancellationAction = new SellCancellation
            {
                orderId = orderId,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = itemSubType,
                tradableId = itemId,
            };
            var expectedState = sellCancellationAction.Execute(new ActionContext
            {
                BlockIndex = 101,
                PreviousStates = prevState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _agentAddress,
            });

            var cancelProductRegistration = new CancelProductRegistration
            {
                AvatarAddress = _avatarAddress,
                ProductInfos = new List<ProductInfo>
                {
                    new ProductInfo
                    {
                        ProductId = orderId,
                        AgentAddress = _agentAddress,
                        AvatarAddress = _avatarAddress,
                        Legacy = true,
                        Price = order.Price,
                        Type = tradableItem is TradableMaterial
                            ? ProductType.Fungible
                            : ProductType.NonFungible,
                    },
                },
            };

            var actualState = cancelProductRegistration.Execute(new ActionContext
            {
                BlockIndex = 101,
                PreviousStates = prevState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _agentAddress,
            });

            foreach (var nextState in new[] { expectedState, actualState })
            {
                ShardedShopStateV2 nextShopState =
                    new ShardedShopStateV2((Dictionary)nextState.GetState(shardedShopAddress));
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
                var nextReceiptList =
                    new OrderDigestListState((Dictionary)nextState.GetState(orderDigestList.Address));
                Assert.Empty(nextReceiptList.OrderDigestList);

                var sellCancelItem =
                    (ITradableItem)ItemFactory.Deserialize(
                        (Dictionary)nextState.GetState(Addresses.GetItemAddress(itemId)));
                Assert.Equal(101, sellCancelItem.RequiredBlockIndex);
            }
        }
    }
}
