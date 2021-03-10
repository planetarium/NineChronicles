namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
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

    public class BuyMultipleTest
    {
        private readonly Address _sellerAgentAddress;
        private readonly Address _sellerAvatarAddress;
        private readonly Address _buyerAgentAddress;
        private readonly Address _buyerAvatarAddress;
        private readonly AvatarState _buyerAvatarState;
        private readonly TableSheets _tableSheets;
        private readonly GoldCurrencyState _goldCurrencyState;
        private IAccountStateDelta _initialState;

        public BuyMultipleTest(ITestOutputHelper outputHelper)
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

            var currency = new Currency("NCG", 2, minters: null);
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

            var shopState = new ShopState();

            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .SetState(Addresses.Shop, shopState.Serialize())
                .SetState(_sellerAgentAddress, sellerAgentState.Serialize())
                .SetState(_sellerAvatarAddress, sellerAvatarState.Serialize())
                .SetState(_buyerAgentAddress, buyerAgentState.Serialize())
                .SetState(_buyerAvatarAddress, _buyerAvatarState.Serialize())
                .MintAsset(_buyerAgentAddress, _goldCurrencyState.Currency * 100);
        }

        public static IEnumerable<object[]> GetExecuteMemberData()
        {
            yield return new object[]
            {
                new ShopItemData()
                {
                    ItemType = ItemType.Equipment,
                    ItemId = Guid.NewGuid(),
                    RequiredBlockIndex = Sell.ExpiredBlockIndex,
                    Buy = true,
                    Price = 10,
                    ContainsInInventory = true,
                },
                new ShopItemData()
                {
                    ItemType = ItemType.Costume,
                    ItemId = Guid.NewGuid(),
                    RequiredBlockIndex = Sell.ExpiredBlockIndex,
                    Buy = false,
                    Price = 20,
                    ContainsInInventory = true,
                },
                new ShopItemData()
                {
                    ItemType = ItemType.Costume,
                    ItemId = Guid.NewGuid(),
                    RequiredBlockIndex = 0,
                    Buy = true,
                    Price = 30,
                    ContainsInInventory = false,
                },
            };
        }

        [Theory]
        [MemberData(nameof(GetExecuteMemberData))]
        public void Execute(params ShopItemData[] productDatas)
        {
            var sellerAvatarState = _initialState.GetAvatarState(_sellerAvatarAddress);
            var buyerAvatarState = _initialState.GetAvatarState(_buyerAvatarAddress);

            var goldCurrency = _initialState.GetGoldCurrency();
            var shopState = _initialState.GetShopState();
            var buyCount = 0;
            var itemIdsToBuy = new List<Guid>();

            foreach (var product in productDatas)
            {
                INonFungibleItem nonFungibleItem;
                if (product.ItemType == ItemType.Equipment)
                {
                    var itemUsable = ItemFactory.CreateItemUsable(
                        _tableSheets.EquipmentItemSheet.First,
                        product.ItemId,
                        product.RequiredBlockIndex);
                    nonFungibleItem = itemUsable;
                }
                else
                {
                    var costume = ItemFactory.CreateCostume(_tableSheets.CostumeItemSheet.First, product.ItemId);
                    costume.Update(product.RequiredBlockIndex);
                    nonFungibleItem = costume;
                }

                // Case for backward compatibility of `Buy`
                if (product.ContainsInInventory)
                {
                    sellerAvatarState.inventory.AddItem((ItemBase)nonFungibleItem);
                }

                var shopItemId = Guid.NewGuid();

                var shopItem = new ShopItem(
                    _sellerAgentAddress,
                    _sellerAvatarAddress,
                    shopItemId,
                    new FungibleAssetValue(_goldCurrencyState.Currency, product.Price, 0),
                    product.RequiredBlockIndex,
                    nonFungibleItem);
                shopState.Register(shopItem);

                if (product.Buy)
                {
                    ++buyCount;
                    itemIdsToBuy.Add(shopItemId);
                }
            }

            Assert.NotNull(shopState.Products);
            Assert.Equal(3, shopState.Products.Count);

            IAccountStateDelta previousStates = _initialState
                .SetState(_sellerAvatarAddress, sellerAvatarState.Serialize())
                .SetState(_buyerAvatarAddress, buyerAvatarState.Serialize())
                .SetState(Addresses.Shop, shopState.Serialize());

            var buyerGold = previousStates.GetBalance(_buyerAgentAddress, goldCurrency);
            var priceData = new PriceData(goldCurrency);
            var priceSumData = productDatas
                .Where(i => i.Buy)
                .Aggregate(priceData, (priceSum, next) =>
                {
                    var price = new FungibleAssetValue(goldCurrency, next.Price, 0);
                    var tax = price.DivRem(100, out _) * Buy.TaxRate;
                    var taxedPrice = price - tax;
                    priceData.TaxSum += tax;
                    priceData.TaxedPriceSum += taxedPrice;
                    priceData.PriceSum += price;
                    return priceData;
                });

            var buyMultipleAction = new BuyMultiple
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productIds = itemIdsToBuy,
                sellerAgentAddress = _sellerAgentAddress,
                sellerAvatarAddress = _sellerAvatarAddress,
            };
            var nextState = buyMultipleAction.Execute(new ActionContext()
            {
                BlockIndex = 1,
                PreviousStates = previousStates,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _buyerAgentAddress,
            });

            var nextShopState = nextState.GetShopState();
            Assert.Equal(productDatas.Length - buyCount, nextShopState.Products.Count);

            var nextBuyerAvatarState = nextState.GetAvatarState(_buyerAvatarAddress);
            foreach (var product in productDatas.Where(i => i.Buy))
            {
                Assert.True(
                    nextBuyerAvatarState.inventory.TryGetNonFungibleItem(
                        product.ItemId,
                        out INonFungibleItem outNonFungibleItem)
                );
                Assert.Equal(1, outNonFungibleItem.RequiredBlockIndex);
            }

            Assert.Equal(buyCount, nextBuyerAvatarState.mailBox.Count);

            goldCurrency = nextState.GetGoldCurrency();
            var nextGoldCurrencyGold = nextState.GetBalance(Addresses.GoldCurrency, goldCurrency);
            Assert.Equal(priceSumData.TaxSum, nextGoldCurrencyGold);
            var nextSellerGold = nextState.GetBalance(_sellerAgentAddress, goldCurrency);
            Assert.Equal(priceSumData.TaxedPriceSum, nextSellerGold);
            var nextBuyerGold = nextState.GetBalance(_buyerAgentAddress, goldCurrency);
            Assert.Equal(buyerGold - priceSumData.PriceSum, nextBuyerGold);

            previousStates = nextState;
        }

        [Fact]
        public void ExecuteThrowInvalidAddressException()
        {
            var action = new BuyMultiple
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productIds = new List<Guid>(),
                sellerAgentAddress = _buyerAgentAddress,
                sellerAvatarAddress = _buyerAvatarAddress,
            };

            Assert.Throws<InvalidAddressException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = new State(),
                    Random = new TestRandom(),
                    Signer = _buyerAgentAddress,
                })
            );
        }

        [Fact]
        public void ExecuteThrowFailedLoadStateException()
        {
            var action = new BuyMultiple
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productIds = new List<Guid>(),
                sellerAgentAddress = _sellerAgentAddress,
                sellerAvatarAddress = _sellerAvatarAddress,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = new State(),
                    Random = new TestRandom(),
                    Signer = _buyerAgentAddress,
                })
            );
        }

        [Fact]
        public void ExecuteThrowNotEnoughClearedStageLevelException()
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

            var action = new BuyMultiple
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productIds = new List<Guid>(),
                sellerAgentAddress = _sellerAgentAddress,
                sellerAvatarAddress = _sellerAvatarAddress,
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = _initialState,
                    Random = new TestRandom(),
                    Signer = _buyerAgentAddress,
                })
            );
        }

        [Fact]
        public void ExecuteThrowItemDoesNotExistException()
        {
            var shopState = _initialState.GetShopState();
            Assert.Empty(shopState.Products);

            var costume = ItemFactory.CreateCostume(
                _tableSheets.CostumeItemSheet.First,
                Guid.NewGuid());
            shopState.Register(new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                100,
                costume));

            _initialState = _initialState
                .SetState(Addresses.Shop, shopState.Serialize());

            shopState = _initialState.GetShopState();
            Assert.NotEmpty(shopState.Products);

            var productIds = shopState.Products.Keys.ToList();
            Assert.NotEmpty(productIds);
            productIds.Add(default);

            var action = new BuyMultiple
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productIds = productIds,
                sellerAgentAddress = _sellerAgentAddress,
                sellerAvatarAddress = _sellerAvatarAddress,
            };

            Assert.Throws<ItemDoesNotExistException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = _initialState,
                    Random = new TestRandom(),
                    Signer = _buyerAgentAddress,
                })
            );
        }

        [Fact]
        public void ExecuteThrowItemDoesNotExistExceptionBySellerAvatar()
        {
            IAccountStateDelta previousStates = _initialState;
            var shopState = previousStates.GetShopState();

            var equipment = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                100);
            shopState.Register(new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                100,
                equipment));

            previousStates = previousStates
                .SetState(Addresses.Shop, shopState.Serialize());

            shopState = previousStates.GetShopState();
            Assert.NotEmpty(shopState.Products);

            var (productId, shopItem) = shopState.Products.First();
            Assert.True(shopItem.ExpiredBlockIndex > 0);
            Assert.True(shopItem.ItemUsable.RequiredBlockIndex > 0);

            var action = new BuyMultiple
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productIds = shopState.Products.Keys,
                sellerAgentAddress = _sellerAgentAddress,
                sellerAvatarAddress = _sellerAvatarAddress,
            };

            Assert.Throws<ItemDoesNotExistException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = _initialState,
                    Random = new TestRandom(),
                    Signer = _buyerAgentAddress,
                })
            );
        }

        [Fact]
        public void ExecuteThrowItemDoesNotExistExceptionByEmptyCollection()
        {
            var action = new BuyMultiple
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productIds = new List<Guid>(),
                sellerAgentAddress = _sellerAgentAddress,
                sellerAvatarAddress = _sellerAvatarAddress,
            };

            Assert.Throws<ItemDoesNotExistException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = _initialState,
                    Random = new TestRandom(),
                    Signer = _buyerAgentAddress,
                })
            );
        }

        [Fact]
        public void ExecuteThrowInsufficientBalanceException()
        {
            var shopState = _initialState.GetShopState();

            var equipment = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                1);
            shopState.Register(new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(_goldCurrencyState.Currency, 1, 0),
                100,
                equipment));

            var costume = ItemFactory.CreateCostume(
                _tableSheets.CostumeItemSheet.First,
                Guid.NewGuid());
            shopState.Register(new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                100,
                costume));

            _initialState = _initialState
                .SetState(Addresses.Shop, shopState.Serialize());
            shopState = _initialState.GetShopState();
            Assert.NotEmpty(shopState.Products);

            var productIds = shopState.Products.Keys;
            Assert.NotEmpty(productIds);

            var balance = _initialState.GetBalance(_buyerAgentAddress, _goldCurrencyState.Currency);
            _initialState = _initialState.BurnAsset(_buyerAgentAddress, balance);

            var action = new BuyMultiple
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productIds = productIds,
                sellerAgentAddress = _sellerAgentAddress,
                sellerAvatarAddress = _sellerAvatarAddress,
            };

            Assert.Throws<InsufficientBalanceException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = _initialState,
                    Random = new TestRandom(),
                    Signer = _buyerAgentAddress,
                })
            );
        }

        [Fact]
        public void ExecuteThrowShopItemExpiredException()
        {
            IAccountStateDelta previousStates = _initialState;
            var shopState = previousStates.GetShopState();

            var productId = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                10);
            shopState.Register(new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                10,
                equipment));

            previousStates = previousStates
                .SetState(Addresses.Shop, shopState.Serialize());
            shopState = previousStates.GetShopState();

            Assert.True(shopState.Products.ContainsKey(productId));

            var action = new BuyMultiple
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productIds = shopState.Products.Keys,
                sellerAgentAddress = _sellerAgentAddress,
                sellerAvatarAddress = _sellerAvatarAddress,
            };

            Assert.Throws<ShopItemExpiredException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 11,
                    PreviousStates = previousStates,
                    Random = new TestRandom(),
                    Signer = _buyerAgentAddress,
                })
            );
        }

        private struct PriceData
        {
            public FungibleAssetValue TaxSum;
            public FungibleAssetValue TaxedPriceSum;
            public FungibleAssetValue PriceSum;

            public PriceData(Currency currency)
            {
                TaxSum = new FungibleAssetValue(currency, 0, 0);
                TaxedPriceSum = new FungibleAssetValue(currency, 0, 0);
                PriceSum = new FungibleAssetValue(currency, 0, 0);
            }
        }

        public class ShopItemData
        {
            public ItemType ItemType { get; set; }

            public Guid ItemId { get; set; }

            public BigInteger Price { get; set; }

            public long RequiredBlockIndex { get; set; }

            public bool Buy { get; set; }

            public bool ContainsInInventory { get; set; }
        }
    }
}
