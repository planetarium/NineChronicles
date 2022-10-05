namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Reflection;
    using Bencodex.Types;
    using Lib9c.DevExtensions;
    using Lib9c.DevExtensions.Action;
    using Lib9c.DevExtensions.Model;
    using Lib9c.Model.Order;
    using Lib9c.Tests.TestHelper;
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

    public class Buy11Test
    {
        private readonly Address _sellerAgentAddress;
        private readonly Address _sellerAvatarAddress;
        private readonly Address _buyerAgentAddress;
        private readonly Address _buyerAvatarAddress;
        private readonly AvatarState _buyerAvatarState;
        private readonly TableSheets _tableSheets;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly Guid _orderId;
        private IAccountStateDelta _initialState;
        private IValue _arenaSheetState;

        public Buy11Test(ITestOutputHelper outputHelper)
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
                .SetState(_buyerAgentAddress, buyerAgentState.Serialize())
                .SetState(_buyerAvatarAddress, _buyerAvatarState.Serialize())
                .SetState(Addresses.Shop, new ShopState().Serialize())
                .MintAsset(_buyerAgentAddress, _goldCurrencyState.Currency * 100);

            var arenaSheetAddress = Addresses.GetSheetAddress<ArenaSheet>();
            _arenaSheetState = _initialState.GetState(arenaSheetAddress);
            _initialState = _initialState.SetState(arenaSheetAddress, null);
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

        public static IEnumerable<object[]> GetReconfigureFungibleItemMemberData()
        {
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
                    ItemCount = 50,
                },
                new OrderData()
                {
                    ItemType = ItemType.Material,
                    TradableId = new Guid("15396359-04db-68d5-f24a-d89c18665900"),
                    OrderId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell6.ExpiredBlockIndex + 1,
                    Price = 10,
                    ItemCount = 60,
                },
            };
        }

        [Theory]
        [MemberData(nameof(GetExecuteMemberData))]
        public void Execute(params OrderData[] orderDataList)
        {
            AvatarState buyerAvatarState = _initialState.GetAvatarState(_buyerAvatarAddress);
            List<PurchaseInfo> purchaseInfos = new List<PurchaseInfo>();
            ShopState legacyShopState = _initialState.GetShopState();
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
                purchaseInfos.Add(purchaseInfo);

                _initialState = _initialState
                    .SetState(Order.DeriveAddress(orderId), order.Serialize())
                    .SetState(_buyerAvatarAddress, buyerAvatarState.Serialize())
                    .SetState(sellerAvatarState.address, sellerAvatarState.Serialize())
                    .SetState(shardedShopAddress, shopState.Serialize())
                    .SetState(orderDigestListState.Address, orderDigestListState.Serialize());
            }

            var buyAction = new Buy11
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = purchaseInfos,
            };
            var nextState = buyAction.Execute(new ActionContext()
            {
                BlockIndex = 100,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _buyerAgentAddress,
            });

            FungibleAssetValue totalTax = 0 * _goldCurrencyState.Currency;
            FungibleAssetValue totalPrice = 0 * _goldCurrencyState.Currency;
            Currency goldCurrencyState = nextState.GetGoldCurrency();
            AvatarState nextBuyerAvatarState = nextState.GetAvatarState(_buyerAvatarAddress);

            Assert.Empty(buyAction.errors);

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

            var goldCurrencyGold = nextState.GetBalance(Buy11.GetFeeStoreAddress(), goldCurrencyState);
            Assert.Equal(totalTax, goldCurrencyGold);
            var buyerGold = nextState.GetBalance(_buyerAgentAddress, goldCurrencyState);
            var prevBuyerGold = _initialState.GetBalance(_buyerAgentAddress, goldCurrencyState);
            Assert.Equal(prevBuyerGold - totalPrice, buyerGold);
        }

        [Theory]
        [InlineData(false, false, typeof(FailedLoadStateException))]
        [InlineData(true, false, typeof(NotEnoughClearedStageLevelException))]
        public void Execute_Throw_Exception(bool equalAvatarAddress, bool clearStage, Type exc)
        {
            PurchaseInfo purchaseInfo = new PurchaseInfo(
                default,
                default,
                _buyerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Food,
                _goldCurrencyState.Currency * 0
            );

            if (!clearStage)
            {
                var avatarState = new AvatarState(_buyerAvatarState)
                {
                    worldInformation = new WorldInformation(
                        0,
                        _tableSheets.WorldSheet,
                        0
                    ),
                };
                _initialState = _initialState.SetState(_buyerAvatarAddress, avatarState.Serialize());
            }

            var avatarAddress = equalAvatarAddress ? _buyerAvatarAddress : default;
            var action = new Buy11
            {
                buyerAvatarAddress = avatarAddress,
                purchaseInfos = new[] { purchaseInfo },
            };

            Assert.Throws(exc, () => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = _initialState,
                    Random = new TestRandom(),
                    Signer = _buyerAgentAddress,
                })
            );
        }

        [Theory]
        [MemberData(nameof(ErrorCodeMemberData))]
        public void Execute_ErrorCode(ErrorCodeMember errorCodeMember)
        {
            var agentAddress = errorCodeMember.BuyerExist ? _buyerAgentAddress : default;
            var orderPrice = new FungibleAssetValue(_goldCurrencyState.Currency, 10, 0);
            var sellerAvatarAddress = errorCodeMember.EqualSellerAvatar ? _sellerAvatarAddress : default;
            Address sellerAgentAddress = default;
            if (errorCodeMember.EqualSigner)
            {
                sellerAgentAddress = _buyerAgentAddress;
            }
            else if (errorCodeMember.EqualSellerAgent)
            {
                sellerAgentAddress = _sellerAgentAddress;
            }

            var item = ItemFactory.CreateItem(
                _tableSheets.ConsumableItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Food), new TestRandom());
            var orderTradableId = ((ITradableItem)item).TradableId;
            var tradableId = errorCodeMember.EqualTradableId ? orderTradableId : Guid.NewGuid();
            var price = errorCodeMember.EqualPrice ? orderPrice : default;

            var blockIndex = errorCodeMember.Expire ? Order.ExpirationInterval + 1 : 10;

            if (errorCodeMember.ShopStateExist)
            {
                var shopAddress = ShardedShopStateV2.DeriveAddress(ItemSubType.Food, _orderId);
                var shopState = new ShardedShopStateV2(shopAddress);
                if (errorCodeMember.OrderExist)
                {
                    var sellerAvatarState = _initialState.GetAvatarState(_sellerAvatarAddress);
                    if (!errorCodeMember.NotContains)
                    {
                        var orderLock = new OrderLock(_orderId);
                        sellerAvatarState.inventory.AddItem(item, iLock: orderLock);
                    }

                    var order = OrderFactory.Create(
                        sellerAgentAddress,
                        sellerAvatarAddress,
                        _orderId,
                        orderPrice,
                        orderTradableId,
                        0,
                        ItemSubType.Food,
                        1
                    );
                    if (errorCodeMember.Duplicate)
                    {
                        _initialState = _initialState.SetState(
                            OrderReceipt.DeriveAddress(_orderId),
                            new OrderReceipt(_orderId, _buyerAgentAddress, _buyerAvatarAddress, 0)
                                .Serialize()
                        );
                    }

                    if (errorCodeMember.DigestExist)
                    {
                        var orderDigest = new OrderDigest(
                            sellerAvatarAddress,
                            order.StartedBlockIndex,
                            order.ExpiredBlockIndex,
                            order.OrderId,
                            order.TradableId,
                            orderPrice,
                            0,
                            0,
                            item.Id,
                            1
                        );
                        var orderDigestList = new OrderDigestListState(OrderDigestListState.DeriveAddress(sellerAvatarAddress));
                        orderDigestList.Add(orderDigest);
                        _initialState = _initialState.SetState(orderDigestList.Address, orderDigestList.Serialize());

                        var digest = order.Digest(sellerAvatarState, _tableSheets.CostumeStatSheet);
                        shopState.Add(digest, 0);
                        _initialState = _initialState.SetState(sellerAvatarAddress, sellerAvatarState.Serialize());
                    }

                    _initialState = _initialState.SetState(Order.DeriveAddress(_orderId), order.Serialize());
                }

                _initialState = _initialState.SetState(shopAddress, shopState.Serialize());
            }

            if (errorCodeMember.NotEnoughBalance)
            {
                var balance = _initialState.GetBalance(_buyerAgentAddress, _goldCurrencyState.Currency);
                _initialState = _initialState.BurnAsset(_buyerAgentAddress, balance);
            }

            PurchaseInfo purchaseInfo = new PurchaseInfo(
                _orderId,
                tradableId,
                sellerAgentAddress,
                sellerAvatarAddress,
                ItemSubType.Food,
                price
            );

            var action = new Buy11
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo },
            };

            IAccountStateDelta nextState = action.Execute(new ActionContext()
            {
                BlockIndex = blockIndex,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Signer = _buyerAgentAddress,
            });

            Assert.Contains(
                errorCodeMember.ErrorCode,
                action.errors.Select(r => r.errorCode)
            );

            foreach (var address in new[] { agentAddress, sellerAgentAddress, GoldCurrencyState.Address })
            {
                Assert.Equal(
                    _initialState.GetBalance(address, _goldCurrencyState.Currency),
                    nextState.GetBalance(address, _goldCurrencyState.Currency)
                );
            }
        }

        [Theory]
        [MemberData(nameof(GetReconfigureFungibleItemMemberData))]
        public void Execute_ReconfigureFungibleItem(params OrderData[] orderDataList)
        {
            var buyerAvatarState = _initialState.GetAvatarState(_buyerAvatarAddress);
            var purchaseInfos = new List<PurchaseInfo>();
            var firstData = orderDataList.First();
            var (sellerAvatarState, sellerAgentState) = CreateAvatarState(firstData.SellerAgentAddress, firstData.SellerAvatarAddress);

            var dummyItem = ItemFactory.CreateTradableMaterial(
                _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Hourglass));
            sellerAvatarState.inventory.AddItem2((ItemBase)dummyItem, orderDataList.Sum(x => x.ItemCount));

            foreach (var orderData in orderDataList)
            {
                var orderId = orderData.OrderId;
                var material = ItemFactory.CreateTradableMaterial(
                    _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Hourglass));
                ITradableItem tradableItem = material;
                var itemSubType = ItemSubType.Hourglass;

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
                var inventoryAddress = orderData.SellerAvatarAddress.Derive(LegacyInventoryKey);
                _initialState.SetState(inventoryAddress, sellerAvatarState.inventory.Serialize());

                var sellItem = order.Sell3(sellerAvatarState);
                var orderDigest = order.Digest(sellerAvatarState, _tableSheets.CostumeStatSheet);

                Address digestListAddress = OrderDigestListState.DeriveAddress(firstData.SellerAvatarAddress);
                var digestListState = new OrderDigestListState(OrderDigestListState.DeriveAddress(firstData.SellerAvatarAddress));
                if (_initialState.TryGetState(digestListAddress, out Dictionary rawDigestList))
                {
                    digestListState = new OrderDigestListState(rawDigestList);
                }

                var orderDigestListState = digestListState;
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
                    firstData.SellerAgentAddress,
                    firstData.SellerAvatarAddress,
                    itemSubType,
                    order.Price
                );
                purchaseInfos.Add(purchaseInfo);

                _initialState = _initialState
                    .SetState(Order.DeriveAddress(orderId), order.Serialize())
                    .SetState(_buyerAvatarAddress, buyerAvatarState.Serialize())
                    .SetState(sellerAvatarState.address, sellerAvatarState.Serialize())
                    .SetState(shardedShopAddress, shopState.Serialize())
                    .SetState(orderDigestListState.Address, orderDigestListState.Serialize());
            }

            var sumCount = orderDataList.Sum(x => x.ItemCount);
            Assert.Equal(1, sellerAvatarState.inventory.Items.Count);
            Assert.Equal(sumCount, sellerAvatarState.inventory.Items.First().count);

            var buyAction = new Buy11
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = purchaseInfos,
            };
            var nextState = buyAction.Execute(new ActionContext()
            {
                BlockIndex = 100,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _buyerAgentAddress,
            });

            AvatarState nextBuyerAvatarState = nextState.GetAvatarState(_buyerAvatarAddress);

            Assert.Empty(buyAction.errors);

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
                Assert.Equal(2, nextSellerAvatarState.mailBox.OfType<OrderSellerMail>().Count());

                var buyerMail = nextBuyerAvatarState.mailBox
                    .OfType<OrderBuyerMail>()
                    .Single(i => i.OrderId.Equals(order.OrderId));
                Assert.Equal(order.OrderId, buyerMail.OrderId);

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
        }

        [Fact]
        public void Rehearsal()
        {
            PurchaseInfo purchaseInfo = new PurchaseInfo(
                _orderId,
                default,
                _sellerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Weapon,
                new FungibleAssetValue(_goldCurrencyState.Currency, 10, 0)
            );

            var action = new Buy11
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo },
            };

            var updatedAddresses = new List<Address>()
            {
                _sellerAgentAddress,
                _sellerAvatarAddress,
                _sellerAvatarAddress.Derive(LegacyInventoryKey),
                _sellerAvatarAddress.Derive(LegacyWorldInformationKey),
                _sellerAvatarAddress.Derive(LegacyQuestListKey),
                OrderDigestListState.DeriveAddress(_sellerAvatarAddress),
                _buyerAgentAddress,
                _buyerAvatarAddress,
                _buyerAvatarAddress.Derive(LegacyInventoryKey),
                _buyerAvatarAddress.Derive(LegacyWorldInformationKey),
                _buyerAvatarAddress.Derive(LegacyQuestListKey),
                Buy11.GetFeeStoreAddress(),
                ShardedShopStateV2.DeriveAddress(ItemSubType.Weapon, _orderId),
                OrderReceipt.DeriveAddress(_orderId),
            };

            var state = new State();

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _buyerAgentAddress,
                BlockIndex = 0,
                Rehearsal = true,
            });

            Assert.Equal(updatedAddresses.ToImmutableHashSet(), nextState.UpdatedAddresses);
        }

        [Fact]
        public void Execute_With_Testbed()
        {
            var result = BlockChainHelper.MakeInitialState();
            var testbed = result.GetTestbed();
            var nextState = result.GetState();
            var data = TestbedHelper.LoadData<TestbedSell>("TestbedSell");

            Assert.Equal(testbed.Orders.Count(), testbed.result.ItemInfos.Count);
            for (var i = 0; i < testbed.Orders.Count; i++)
            {
                Assert.Equal(data.Items[i].ItemSubType, testbed.Orders[i].ItemSubType);
            }

            var purchaseInfos = new List<PurchaseInfo>();
            foreach (var order in testbed.Orders)
            {
                var purchaseInfo = new PurchaseInfo(
                    order.OrderId,
                    order.TradableId,
                    order.SellerAgentAddress,
                    order.SellerAvatarAddress,
                    order.ItemSubType,
                    order.Price
                );
                purchaseInfos.Add(purchaseInfo);
            }

            var prevBuyerGold = nextState.GetBalance(result.GetAgentState().address, nextState.GetGoldCurrency());

            var buyAction = new Buy11
            {
                buyerAvatarAddress = result.GetAvatarState().address,
                purchaseInfos = purchaseInfos,
            };

            var arenaSheetAddress = Addresses.GetSheetAddress<ArenaSheet>();
            nextState = nextState.SetState(arenaSheetAddress, null);

            nextState = buyAction.Execute(new ActionContext()
            {
                BlockIndex = 100,
                PreviousStates = nextState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = result.GetAgentState().address,
            });

            var totalTax = 0 * _goldCurrencyState.Currency;
            var totalPrice = 0 * _goldCurrencyState.Currency;
            var goldCurrencyState = nextState.GetGoldCurrency();
            var nextBuyerAvatarState = nextState.GetAvatarState(result.GetAvatarState().address);

            Assert.Empty(buyAction.errors);

            var agentRevenue = new Dictionary<Address, FungibleAssetValue>();
            foreach (var purchaseInfo in purchaseInfos)
            {
                var shardedShopAddress =
                    ShardedShopStateV2.DeriveAddress(purchaseInfo.ItemSubType, purchaseInfo.OrderId);
                var nextShopState = new ShardedShopStateV2((Dictionary)nextState.GetState(shardedShopAddress));
                Assert.DoesNotContain(nextShopState.OrderDigestList, o => o.OrderId.Equals(purchaseInfo.OrderId));

                var order = OrderFactory.Deserialize(
                        (Dictionary)nextState.GetState(Order.DeriveAddress(purchaseInfo.OrderId)));
                var tradableId = purchaseInfo.TradableId;
                var itemCount = order is FungibleOrder fungibleOrder ? fungibleOrder.ItemCount : 1;
                var nextSellerAvatarState =
                    nextState.GetAvatarStateV2(purchaseInfo.SellerAvatarAddress);

                Assert.True(nextBuyerAvatarState.inventory.TryGetTradableItem(
                    tradableId, 100, itemCount, out var _));
                Assert.False(nextSellerAvatarState.inventory.TryGetTradableItem(
                    tradableId, 100, itemCount, out var _));

                Assert.Empty(nextSellerAvatarState.mailBox.OfType<OrderExpirationMail>());
                var orderReceipt = new OrderReceipt((Dictionary)nextState.GetState(OrderReceipt.DeriveAddress(order.OrderId)));
                Assert.Equal(order.OrderId, orderReceipt.OrderId);
                Assert.Equal(result.GetAgentState().address, orderReceipt.BuyerAgentAddress);
                Assert.Equal(result.GetAvatarState().address, orderReceipt.BuyerAvatarAddress);
                Assert.Equal(100, orderReceipt.TransferredBlockIndex);

                totalTax += order.GetTax();
                totalPrice += order.Price;

                var revenue = order.Price - order.GetTax();
                if (agentRevenue.ContainsKey(order.SellerAgentAddress))
                {
                    agentRevenue[order.SellerAgentAddress] += revenue;
                }
                else
                {
                    agentRevenue.Add(order.SellerAgentAddress, revenue);
                }

                var mailCount = purchaseInfos.Count(x =>
                    x.SellerAvatarAddress.Equals(purchaseInfo.SellerAvatarAddress));
                Assert.Equal(mailCount, nextSellerAvatarState.mailBox.OfType<OrderSellerMail>().Count());
                Assert.Empty(nextSellerAvatarState.mailBox.OfType<OrderExpirationMail>());
            }

            var buyerMails = nextBuyerAvatarState.mailBox.OfType<OrderBuyerMail>().ToList();
            Assert.Equal(testbed.Orders.Count(), buyerMails.Count());
            foreach (var mail in buyerMails)
            {
                Assert.True(purchaseInfos.Exists(x => x.OrderId.Equals(mail.OrderId)));
            }

            var buyerGold = nextState.GetBalance(result.GetAgentState().address, goldCurrencyState);
            Assert.Equal(prevBuyerGold - totalPrice, buyerGold);
            var goldCurrencyGold = nextState.GetBalance(Buy11.GetFeeStoreAddress(), goldCurrencyState);
            Assert.Equal(totalTax, goldCurrencyGold);

            foreach (var (agentAddress, expectedGold) in agentRevenue)
            {
                var gold = nextState.GetBalance(agentAddress, goldCurrencyState);
                Assert.Equal(expectedGold, gold);
            }
        }

        [Fact]
        public void Execute_ActionObsoletedException()
        {
            var result = BlockChainHelper.MakeInitialState();
            var testbed = result.GetTestbed();
            var nextState = result.GetState();
            var data = TestbedHelper.LoadData<TestbedSell>("TestbedSell");

            Assert.Equal(testbed.Orders.Count(), testbed.result.ItemInfos.Count);
            for (var i = 0; i < testbed.Orders.Count; i++)
            {
                Assert.Equal(data.Items[i].ItemSubType, testbed.Orders[i].ItemSubType);
            }

            var purchaseInfos = new List<PurchaseInfo>();
            foreach (var order in testbed.Orders)
            {
                var purchaseInfo = new PurchaseInfo(
                    order.OrderId,
                    order.TradableId,
                    order.SellerAgentAddress,
                    order.SellerAvatarAddress,
                    order.ItemSubType,
                    order.Price
                );
                purchaseInfos.Add(purchaseInfo);
            }

            var buyAction = new Buy11
            {
                buyerAvatarAddress = result.GetAvatarState().address,
                purchaseInfos = purchaseInfos,
            };

            var arenaSheetAddress = Addresses.GetSheetAddress<ArenaSheet>();
            nextState = nextState.SetState(arenaSheetAddress, _arenaSheetState);
            Assert.Throws<ActionObsoletedException>(() =>
            {
                buyAction.Execute(new ActionContext()
                {
                    BlockIndex = 100,
                    PreviousStates = nextState,
                    Random = new TestRandom(),
                    Rehearsal = false,
                    Signer = result.GetAgentState().address,
                });
            });
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

        public class OrderData
        {
            public ItemType ItemType { get; set; }

            public Guid TradableId { get; set; }

            public Guid OrderId { get; set; }

            public Address SellerAgentAddress { get; set; }

            public Address SellerAvatarAddress { get; set; }

            public BigInteger Price { get; set; }

            public long RequiredBlockIndex { get; set; }

            public int ItemCount { get; set; }
        }

        public class ErrorCodeMember
        {
            public bool EqualSigner { get; set; }

            public bool BuyerExist { get; set; }

            public bool ShopStateExist { get; set; }

            public bool OrderExist { get; set; }

            public bool DigestExist { get; set; }

            public int ErrorCode { get; set; }

            public bool EqualSellerAgent { get; set; }

            public bool EqualSellerAvatar { get; set; }

            public bool EqualTradableId { get; set; }

            public bool EqualPrice { get; set; }

            public bool Expire { get; set; }

            public bool NotContains { get; set; }

            public bool NotEnoughBalance { get; set; }

            public bool Duplicate { get; set; }
        }

#pragma warning disable SA1201
        public static IEnumerable<object[]> ErrorCodeMemberData() => new List<object[]>
        {
            new object[]
            {
                new ErrorCodeMember()
                {
                    EqualSigner = true,
                    ErrorCode = Buy.ErrorCodeInvalidAddress,
                },
            },
            new object[]
            {
                new ErrorCodeMember()
                {
                    BuyerExist = true,
                    ErrorCode = Buy.ErrorCodeFailedLoadingState,
                },
            },
            new object[]
            {
                new ErrorCodeMember()
                {
                    BuyerExist = true,
                    ShopStateExist = true,
                    ErrorCode = Buy.ErrorCodeInvalidOrderId,
                },
            },
            new object[]
            {
                new ErrorCodeMember()
                {
                    BuyerExist = true,
                    ShopStateExist = true,
                    OrderExist = true,
                    ErrorCode = Buy.ErrorCodeInvalidOrderId,
                },
            },
            new object[]
            {
                new ErrorCodeMember()
                {
                    BuyerExist = true,
                    ShopStateExist = true,
                    OrderExist = true,
                    DigestExist = true,
                    ErrorCode = Buy.ErrorCodeFailedLoadingState,
                },
            },
            new object[]
            {
                new ErrorCodeMember()
                {
                    BuyerExist = true,
                    ShopStateExist = true,
                    OrderExist = true,
                    DigestExist = true,
                    EqualSellerAgent = true,
                    EqualSellerAvatar = true,
                    ErrorCode = Buy.ErrorCodeInvalidTradableId,
                },
            },
            new object[]
            {
                new ErrorCodeMember()
                {
                    BuyerExist = true,
                    ShopStateExist = true,
                    OrderExist = true,
                    DigestExist = true,
                    EqualSellerAgent = true,
                    EqualSellerAvatar = true,
                    EqualTradableId = true,
                    ErrorCode = Buy.ErrorCodeInvalidPrice,
                },
            },
            new object[]
            {
                new ErrorCodeMember()
                {
                    BuyerExist = true,
                    ShopStateExist = true,
                    OrderExist = true,
                    DigestExist = true,
                    EqualSellerAgent = true,
                    EqualSellerAvatar = true,
                    EqualTradableId = true,
                    EqualPrice = true,
                    Expire = true,
                    ErrorCode = Buy.ErrorCodeShopItemExpired,
                },
            },
            new object[]
            {
                new ErrorCodeMember()
                {
                    BuyerExist = true,
                    ShopStateExist = true,
                    OrderExist = true,
                    DigestExist = true,
                    EqualSellerAgent = true,
                    EqualSellerAvatar = true,
                    EqualTradableId = true,
                    EqualPrice = true,
                    NotEnoughBalance = true,
                    ErrorCode = Buy.ErrorCodeInsufficientBalance,
                },
            },
            new object[]
            {
                new ErrorCodeMember()
                {
                    BuyerExist = true,
                    ShopStateExist = true,
                    OrderExist = true,
                    DigestExist = true,
                    EqualSellerAgent = true,
                    EqualSellerAvatar = true,
                    EqualTradableId = true,
                    EqualPrice = true,
                    Duplicate = true,
                    ErrorCode = Buy.ErrorCodeDuplicateSell,
                },
            },
        };
#pragma warning restore SA1201
    }
}
