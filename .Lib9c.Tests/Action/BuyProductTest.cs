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
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.Market;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using OrderData = BuyTest.OrderData;

    public class BuyProductTest
    {
        private readonly Address _sellerAgentAddress;
        private readonly Address _sellerAvatarAddress;
        private readonly Address _sellerAgentAddress2;
        private readonly Address _sellerAvatarAddress2;
        private readonly Address _buyerAgentAddress;
        private readonly Address _buyerAvatarAddress;
        private readonly AvatarState _buyerAvatarState;
        private readonly TableSheets _tableSheets;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly Guid _orderId;
        private IAccountStateDelta _initialState;

        public BuyProductTest(ITestOutputHelper outputHelper)
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

            _sellerAgentAddress = new PrivateKey().ToAddress();
            var sellerAgentState = new AgentState(_sellerAgentAddress);
            _sellerAvatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
            var sellerAvatarState = new AvatarState(
                _sellerAvatarAddress,
                _sellerAgentAddress,
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
            sellerAgentState.avatarAddresses[0] = _sellerAvatarAddress;

            _sellerAgentAddress2 = new PrivateKey().ToAddress();
            var agentState2 = new AgentState(_sellerAgentAddress2);
            _sellerAvatarAddress2 = new PrivateKey().ToAddress();
            var sellerAvatarState2 = new AvatarState(
                _sellerAvatarAddress2,
                _sellerAgentAddress2,
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
            agentState2.avatarAddresses[0] = _sellerAvatarAddress2;

            _buyerAgentAddress = new PrivateKey().ToAddress();
            var buyerAgentState = new AgentState(_buyerAgentAddress);
            _buyerAvatarAddress = new PrivateKey().ToAddress();
            _buyerAvatarState = new AvatarState(
                _buyerAvatarAddress,
                _buyerAgentAddress,
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
            buyerAgentState.avatarAddresses[0] = _buyerAvatarAddress;

            _orderId = new Guid("6d460c1a-755d-48e4-ad67-65d5f519dbc8");
            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .SetState(_sellerAgentAddress, sellerAgentState.Serialize())
                .SetState(_sellerAvatarAddress, sellerAvatarState.Serialize())
                .SetState(_sellerAgentAddress2, agentState2.Serialize())
                .SetState(_sellerAvatarAddress2, sellerAvatarState2.Serialize())
                .SetState(_buyerAgentAddress, buyerAgentState.Serialize())
                .SetState(_buyerAvatarAddress, _buyerAvatarState.Serialize())
                .MintAsset(_buyerAgentAddress, _goldCurrencyState.Currency * 100);
        }

        public static IEnumerable<object[]> GetExecuteMemberData()
        {
            yield return new object[]
            {
                new OrderData()
                {
                    ItemType = ItemType.Equipment,
                    TradableId = Guid.NewGuid(),
                    OrderId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell6.ExpiredBlockIndex,
                    Price = 10,
                    ItemCount = 1,
                },
                new OrderData()
                {
                    ItemType = ItemType.Costume,
                    TradableId = Guid.NewGuid(),
                    OrderId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = 0,
                    Price = 20,
                    ItemCount = 1,
                },
            };
            yield return new object[]
            {
                new OrderData()
                {
                    ItemType = ItemType.Costume,
                    TradableId = Guid.NewGuid(),
                    OrderId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = 0,
                    Price = 10,
                    ItemCount = 1,
                },
                new OrderData()
                {
                    ItemType = ItemType.Equipment,
                    TradableId = Guid.NewGuid(),
                    OrderId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell6.ExpiredBlockIndex,
                    Price = 50,
                    ItemCount = 1,
                },
            };
            yield return new object[]
            {
                new OrderData()
                {
                    ItemType = ItemType.Material,
                    TradableId = new Guid("15396359-04db-68d5-f24a-d89c18665900"),
                    OrderId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell6.ExpiredBlockIndex,
                    Price = 50,
                    ItemCount = 1,
                },
                new OrderData()
                {
                    ItemType = ItemType.Material,
                    TradableId = new Guid("15396359-04db-68d5-f24a-d89c18665900"),
                    OrderId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = 0,
                    Price = 10,
                    ItemCount = 2,
                },
            };
        }

        [Fact]
        public void Execute_InvalidAddress()
        {
            var productId = Guid.NewGuid();
            var action = new BuyProduct
            {
                AvatarAddress = _buyerAvatarAddress,
                ProductInfos = new List<ProductInfo>
                {
                    new ProductInfo
                    {
                        AvatarAddress = _buyerAvatarAddress,
                        AgentAddress = _sellerAgentAddress,
                        Legacy = false,
                        Price = 1 * _goldCurrencyState.Currency,
                        ProductId = productId,
                        Type = ProductType.NonFungible,
                    },
                },
            };

            var actionContext = new ActionContext
            {
                Signer = _buyerAgentAddress,
                BlockIndex = 1L,
                PreviousStates = _initialState,
                Random = new TestRandom(),
            };
            Assert.Throws<InvalidAddressException>(() => action.Execute(actionContext));
        }

        [Fact]
        public void Execute_ProductNotFoundException()
        {
            var productId = Guid.NewGuid();
            var action = new BuyProduct
            {
                AvatarAddress = _buyerAvatarAddress,
                ProductInfos = new List<ProductInfo>
                {
                    new ProductInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        AgentAddress = _sellerAgentAddress,
                        Legacy = false,
                        Price = 1 * _goldCurrencyState.Currency,
                        ProductId = productId,
                        Type = ProductType.NonFungible,
                    },
                },
            };

            _initialState = _initialState.SetState(
                ProductsState.DeriveAddress(_sellerAvatarAddress), new ProductsState().Serialize());
            var actionContext = new ActionContext
            {
                Signer = _buyerAgentAddress,
                BlockIndex = 1L,
                PreviousStates = _initialState,
                Random = new TestRandom(),
            };
            Assert.Throws<ProductNotFoundException>(() => action.Execute(actionContext));
        }

        [Theory]
        [MemberData(nameof(GetExecuteMemberData))]
        public void Execute_BackwardCompatibility(params OrderData[] orderDataList)
        {
            AvatarState buyerAvatarState = _initialState.GetAvatarState(_buyerAvatarAddress);
            List<ProductInfo> productInfos = new List<ProductInfo>();
            List<PurchaseInfo> purchaseInfos = new List<PurchaseInfo>();
            foreach (var orderData in orderDataList)
            {
                (AvatarState sellerAvatarState, AgentState sellerAgentState) = CreateAvatarState(
                    orderData.SellerAgentAddress,
                    orderData.SellerAvatarAddress
                );
                ITradableItem tradableItem;
                Guid orderId = orderData.OrderId;
                Guid itemId = orderData.TradableId;
                ItemSubType itemSubType;
                if (orderData.ItemType == ItemType.Equipment)
                {
                    var itemUsable = ItemFactory.CreateItemUsable(
                        _tableSheets.EquipmentItemSheet.First,
                        itemId,
                        0);
                    tradableItem = itemUsable;
                    itemSubType = itemUsable.ItemSubType;
                }
                else if (orderData.ItemType == ItemType.Costume)
                {
                    var costume = ItemFactory.CreateCostume(_tableSheets.CostumeItemSheet.First, itemId);
                    tradableItem = costume;
                    itemSubType = costume.ItemSubType;
                }
                else
                {
                    var material = ItemFactory.CreateTradableMaterial(
                        _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Hourglass));
                    tradableItem = material;
                    itemSubType = ItemSubType.Hourglass;
                }

                var result = new DailyReward2.DailyRewardResult()
                {
                    id = default,
                    materials = new Dictionary<Material, int>(),
                };

                for (var i = 0; i < 100; i++)
                {
                    var mail = new DailyRewardMail(result, i, default, 0);
                    sellerAvatarState.Update(mail);
                    buyerAvatarState.Update(mail);
                }

                Address shardedShopAddress = ShardedShopStateV2.DeriveAddress(itemSubType, orderId);
                var shopState = _initialState.GetState(shardedShopAddress) is null
                    ? new ShardedShopStateV2(shardedShopAddress)
                    : new ShardedShopStateV2((Dictionary)_initialState.GetState(shardedShopAddress));
                var order = OrderFactory.Create(
                    sellerAgentState.address,
                    sellerAvatarState.address,
                    orderId,
                    new FungibleAssetValue(_goldCurrencyState.Currency, orderData.Price, 0),
                    tradableItem.TradableId,
                    0,
                    itemSubType,
                    orderData.ItemCount
                );
                sellerAvatarState.inventory.AddItem((ItemBase)tradableItem, orderData.ItemCount);

                var sellItem = order.Sell(sellerAvatarState);
                var orderDigest = order.Digest(sellerAvatarState, _tableSheets.CostumeStatSheet);
                Assert.True(sellerAvatarState.inventory.TryGetLockedItem(new OrderLock(orderId), out _));

                var orderDigestListState = new OrderDigestListState(OrderDigestListState.DeriveAddress(orderData.SellerAvatarAddress));
                orderDigestListState.Add(orderDigest);
                shopState.Add(orderDigest, 0);

                Assert.Equal(order.ExpiredBlockIndex, sellItem.RequiredBlockIndex);
                Assert.DoesNotContain(((ItemBase)tradableItem).Id, buyerAvatarState.itemMap.Keys);

                var expirationMail = new OrderExpirationMail(
                    101,
                    orderId,
                    order.ExpiredBlockIndex,
                    orderId
                );
                sellerAvatarState.mailBox.Add(expirationMail);

                var purchaseInfo = new PurchaseInfo(
                    orderId,
                    tradableItem.TradableId,
                    order.SellerAgentAddress,
                    order.SellerAvatarAddress,
                    itemSubType,
                    order.Price
                );
                var productInfo = new ProductInfo
                {
                    ProductId = order.OrderId,
                    AgentAddress = order.SellerAgentAddress,
                    AvatarAddress = order.SellerAvatarAddress,
                    Legacy = true,
                    Price = order.Price,
                    Type = tradableItem is TradableMaterial
                        ? ProductType.Fungible
                        : ProductType.NonFungible,
                };
                productInfos.Add(productInfo);
                purchaseInfos.Add(purchaseInfo);

                _initialState = _initialState
                    .SetState(Order.DeriveAddress(orderId), order.Serialize())
                    .SetState(_buyerAvatarAddress, buyerAvatarState.Serialize())
                    .SetState(sellerAvatarState.address, sellerAvatarState.Serialize())
                    .SetState(shardedShopAddress, shopState.Serialize())
                    .SetState(orderDigestListState.Address, orderDigestListState.Serialize());
            }

            var buyAction = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = purchaseInfos,
            };
            var buyProductAction = new BuyProduct
            {
                AvatarAddress = _buyerAvatarAddress,
                ProductInfos = productInfos,
            };

            var expectedState = buyAction.Execute(new ActionContext()
            {
                BlockIndex = 100,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _buyerAgentAddress,
            });

            var actualState = buyProductAction.Execute(new ActionContext
            {
                BlockIndex = 100,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _buyerAgentAddress,
            });

            Assert.Empty(buyAction.errors);

            foreach (var nextState in new[] { expectedState, actualState })
            {
                FungibleAssetValue totalTax = 0 * _goldCurrencyState.Currency;
                FungibleAssetValue totalPrice = 0 * _goldCurrencyState.Currency;
                Currency goldCurrencyState = nextState.GetGoldCurrency();
                AvatarState nextBuyerAvatarState = nextState.GetAvatarStateV2(_buyerAvatarAddress);

                foreach (var purchaseInfo in purchaseInfos)
                {
                    Address shardedShopAddress =
                        ShardedShopStateV2.DeriveAddress(purchaseInfo.ItemSubType, purchaseInfo.OrderId);
                    var nextShopState = new ShardedShopStateV2((Dictionary)nextState.GetState(shardedShopAddress));
                    Assert.DoesNotContain(nextShopState.OrderDigestList, o => o.OrderId.Equals(purchaseInfo.OrderId));
                    Order order =
                        OrderFactory.Deserialize(
                            (Dictionary)nextState.GetState(Order.DeriveAddress(purchaseInfo.OrderId)));
                    FungibleAssetValue tax = order.GetTax();
                    FungibleAssetValue taxedPrice = order.Price - tax;
                    totalTax += tax;
                    totalPrice += order.Price;

                    int itemCount = order is FungibleOrder fungibleOrder ? fungibleOrder.ItemCount : 1;
                    Assert.True(
                        nextBuyerAvatarState.inventory.TryGetTradableItems(
                            purchaseInfo.TradableId,
                            100,
                            itemCount,
                            out List<Inventory.Item> inventoryItems)
                    );
                    Assert.Single(inventoryItems);
                    Inventory.Item inventoryItem = inventoryItems.First();
                    ITradableItem tradableItem = (ITradableItem)inventoryItem.item;
                    Assert.Equal(100, tradableItem.RequiredBlockIndex);
                    int expectedCount = tradableItem is TradableMaterial
                        ? orderDataList.Sum(i => i.ItemCount)
                        : itemCount;
                    Assert.Equal(expectedCount, inventoryItem.count);
                    Assert.Equal(expectedCount, nextBuyerAvatarState.itemMap[((ItemBase)tradableItem).Id]);

                    var nextSellerAvatarState = nextState.GetAvatarStateV2(purchaseInfo.SellerAvatarAddress);
                    Assert.False(
                        nextSellerAvatarState.inventory.TryGetTradableItems(
                            purchaseInfo.TradableId,
                            100,
                            itemCount,
                            out _)
                    );
                    Assert.Equal(30, nextSellerAvatarState.mailBox.Count);
                    Assert.Empty(nextSellerAvatarState.mailBox.OfType<OrderExpirationMail>());
                    Assert.Single(nextSellerAvatarState.mailBox.OfType<OrderSellerMail>());
                    var sellerMail = nextSellerAvatarState.mailBox.OfType<OrderSellerMail>().First();
                    Assert.Equal(order.OrderId, sellerMail.OrderId);

                    var buyerMail = nextBuyerAvatarState.mailBox
                        .OfType<OrderBuyerMail>()
                        .Single(i => i.OrderId.Equals(order.OrderId));
                    Assert.Equal(order.OrderId, buyerMail.OrderId);

                    FungibleAssetValue sellerGold =
                        nextState.GetBalance(purchaseInfo.SellerAgentAddress, goldCurrencyState);
                    Assert.Equal(taxedPrice, sellerGold);

                    var orderReceipt = new OrderReceipt((Dictionary)nextState.GetState(OrderReceipt.DeriveAddress(order.OrderId)));
                    Assert.Equal(order.OrderId, orderReceipt.OrderId);
                    Assert.Equal(_buyerAgentAddress, orderReceipt.BuyerAgentAddress);
                    Assert.Equal(_buyerAvatarAddress, orderReceipt.BuyerAvatarAddress);
                    Assert.Equal(100, orderReceipt.TransferredBlockIndex);

                    var nextOrderDigestListState = new OrderDigestListState(
                        (Dictionary)nextState.GetState(OrderDigestListState.DeriveAddress(purchaseInfo.SellerAvatarAddress))
                    );
                    Assert.Empty(nextOrderDigestListState.OrderDigestList);
                }

                Assert.Equal(30, nextBuyerAvatarState.mailBox.Count);

                var arenaSheet = _tableSheets.ArenaSheet;
                var arenaData = arenaSheet.GetRoundByBlockIndex(100);
                var feeStoreAddress = Addresses.GetShopFeeAddress(arenaData.ChampionshipId, arenaData.Round);
                var goldCurrencyGold = nextState.GetBalance(feeStoreAddress, goldCurrencyState);
                Assert.Equal(totalTax, goldCurrencyGold);
                var buyerGold = nextState.GetBalance(_buyerAgentAddress, goldCurrencyState);
                var prevBuyerGold = _initialState.GetBalance(_buyerAgentAddress, goldCurrencyState);
                Assert.Equal(prevBuyerGold - totalPrice, buyerGold);
            }
        }

        private (AvatarState AvatarState, AgentState AgentState) CreateAvatarState(
            Address agentAddress, Address avatarAddress)
        {
            var agentState = new AgentState(agentAddress);
            var rankingMapAddress = new PrivateKey().ToAddress();

            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
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
            agentState.avatarAddresses[0] = avatarAddress;

            _initialState = _initialState
                .SetState(agentAddress, agentState.Serialize())
                .SetState(avatarAddress, avatarState.Serialize());
            return (avatarState, agentState);
        }
    }
}
