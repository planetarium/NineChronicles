namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.Serialization.Formatters.Binary;
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
        private readonly Dictionary<AvatarState, AgentState> _sellerAgentStateMap;
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

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            _goldCurrencyState = new GoldCurrencyState(currency);

            _sellerAgentStateMap = new Dictionary<AvatarState, AgentState>();

            _buyerAgentAddress = new PrivateKey().ToAddress();
            var buyerAgentState = new AgentState(_buyerAgentAddress);
            _buyerAvatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
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
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell6.ExpiredBlockIndex,
                    Buy = true,
                    Price = 10,
                    ContainsInInventory = true,
                },
                new ShopItemData()
                {
                    ItemType = ItemType.Costume,
                    ItemId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell6.ExpiredBlockIndex,
                    Buy = false,
                    Price = 20,
                    ContainsInInventory = true,
                },
                new ShopItemData()
                {
                    ItemType = ItemType.Costume,
                    ItemId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = 0,
                    Buy = true,
                    Price = 30,
                    ContainsInInventory = false,
                },
            };
            yield return new object[]
            {
                new ShopItemData()
                {
                    ItemType = ItemType.Costume,
                    ItemId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell6.ExpiredBlockIndex,
                    Buy = false,
                    Price = 10,
                    ContainsInInventory = true,
                },
                new ShopItemData()
                {
                    ItemType = ItemType.Costume,
                    ItemId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell6.ExpiredBlockIndex,
                    Buy = false,
                    Price = 50,
                    ContainsInInventory = true,
                },
                new ShopItemData()
                {
                    ItemType = ItemType.Equipment,
                    ItemId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell6.ExpiredBlockIndex,
                    Buy = false,
                    Price = 30,
                    ContainsInInventory = true,
                },
            };
            yield return new object[]
            {
                new ShopItemData()
                {
                    ItemType = ItemType.Equipment,
                    ItemId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = 0,
                    Buy = true,
                    Price = 20,
                    ContainsInInventory = false,
                },
                new ShopItemData()
                {
                    ItemType = ItemType.Equipment,
                    ItemId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = 0,
                    Buy = true,
                    Price = 50,
                    ContainsInInventory = false,
                },
                new ShopItemData()
                {
                    ItemType = ItemType.Equipment,
                    ItemId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = 0,
                    Buy = true,
                    Price = 30,
                    ContainsInInventory = false,
                },
            };
            yield return new object[]
            {
                new ShopItemData()
                {
                    ItemType = ItemType.Costume,
                    ItemId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell6.ExpiredBlockIndex,
                    Buy = true,
                    Price = 30,
                    ContainsInInventory = true,
                },
                new ShopItemData()
                {
                    ItemType = ItemType.Costume,
                    ItemId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell6.ExpiredBlockIndex,
                    Buy = true,
                    Price = 30,
                    ContainsInInventory = true,
                },
                new ShopItemData()
                {
                    ItemType = ItemType.Equipment,
                    ItemId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell6.ExpiredBlockIndex,
                    Buy = true,
                    Price = 30,
                    ContainsInInventory = true,
                },
            };
        }

        [Theory]
        [MemberData(nameof(GetExecuteMemberData))]
        public void Execute(params ShopItemData[] productDatas)
        {
            var buyerAvatarState = _initialState.GetAvatarState(_buyerAvatarAddress);

            var goldCurrency = _initialState.GetGoldCurrency();
            var shopState = _initialState.GetShopState();
            var buyCount = 0;
            var itemsToBuy = new List<BuyMultiple.PurchaseInfo>();

            foreach (var product in productDatas)
            {
                var (sellerAvatarState, sellerAgentState) =
                    CreateAvatarState(product.SellerAgentAddress, product.SellerAvatarAddress);

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
                    sellerAvatarState.inventory.AddItem2((ItemBase)nonFungibleItem);
                }

                var shopItemId = Guid.NewGuid();

                var shopItem = new ShopItem(
                    sellerAgentState.address,
                    sellerAvatarState.address,
                    shopItemId,
                    new FungibleAssetValue(_goldCurrencyState.Currency, product.Price, 0),
                    product.RequiredBlockIndex,
                    nonFungibleItem);
                shopState.Register(shopItem);

                if (product.Buy)
                {
                    ++buyCount;
                    var purchaseInfo = new BuyMultiple.PurchaseInfo(
                            shopItem.ProductId,
                            shopItem.SellerAgentAddress,
                            shopItem.SellerAvatarAddress);
                    itemsToBuy.Add(purchaseInfo);
                }
            }

            Assert.NotNull(shopState.Products);
            Assert.Equal(3, shopState.Products.Count);

            _initialState = _initialState
                .SetState(_buyerAvatarAddress, buyerAvatarState.Serialize())
                .SetState(Addresses.Shop, shopState.Serialize());

            var priceData = new PriceData(goldCurrency);

            foreach (var avatarState in _sellerAgentStateMap.Keys)
            {
                var agentState = _sellerAgentStateMap[avatarState];
                priceData.TaxedPriceSum[agentState.address] = new FungibleAssetValue(goldCurrency, 0, 0);

                _initialState = _initialState
                    .SetState(avatarState.address, avatarState.Serialize());
            }

            IAccountStateDelta previousStates = _initialState;

            var buyerGold = previousStates.GetBalance(_buyerAgentAddress, goldCurrency);
            var priceSumData = productDatas
                .Where(i => i.Buy)
                .Aggregate(priceData, (priceSum, next) =>
                {
                    var price = new FungibleAssetValue(goldCurrency, next.Price, 0);
                    var tax = price.DivRem(100, out _) * Buy.TaxRate;
                    var taxedPrice = price - tax;
                    priceData.TaxSum += tax;

                    var prevSum = priceData.TaxedPriceSum[next.SellerAgentAddress];
                    priceData.TaxedPriceSum[next.SellerAgentAddress] = prevSum + taxedPrice;
                    priceData.PriceSum += price;
                    return priceData;
                });

            var buyMultipleAction = new BuyMultiple
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = itemsToBuy,
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
                Assert.Equal(product.RequiredBlockIndex, outNonFungibleItem.RequiredBlockIndex);
            }

            Assert.Equal(buyCount, nextBuyerAvatarState.mailBox.Count);

            goldCurrency = nextState.GetGoldCurrency();
            var nextGoldCurrencyGold = nextState.GetBalance(Addresses.GoldCurrency, goldCurrency);
            Assert.Equal(priceSumData.TaxSum, nextGoldCurrencyGold);

            foreach (var product in productDatas)
            {
                var nextSellerGold = nextState.GetBalance(product.SellerAgentAddress, goldCurrency);
                Assert.Equal(priceSumData.TaxedPriceSum[product.SellerAgentAddress], nextSellerGold);
            }

            var nextBuyerGold = nextState.GetBalance(_buyerAgentAddress, goldCurrency);
            Assert.Equal(buyerGold - priceSumData.PriceSum, nextBuyerGold);

            previousStates = nextState;
        }

        [Fact]
        public void ExecuteThrowInvalidAddressException()
        {
            var shopState = _initialState.GetShopState();
            var costume = ItemFactory.CreateCostume(
                   _tableSheets.CostumeItemSheet.First,
                   Guid.NewGuid());
            shopState.Register(new ShopItem(
                _buyerAgentAddress,
                _buyerAvatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                100,
                costume));

            _initialState = _initialState
                .SetState(Addresses.Shop, shopState.Serialize());

            shopState = _initialState.GetShopState();
            var products = shopState.Products.Values
                .Select(p => new BuyMultiple.PurchaseInfo(
                    p.ProductId,
                    p.SellerAgentAddress,
                    p.SellerAvatarAddress))
                .ToList();
            Assert.NotEmpty(products);

            var action = new BuyMultiple
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = products,
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
                purchaseInfos = new List<BuyMultiple.PurchaseInfo>(),
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
                purchaseInfos = new List<BuyMultiple.PurchaseInfo>(),
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
        public void ExecuteThrowItemDoesNotExistErrorByEmptyCollection()
        {
            var action = new BuyMultiple
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new List<BuyMultiple.PurchaseInfo>(),
            };
            action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Signer = _buyerAgentAddress,
            });

            var nextBuyerAvatarState = _initialState.GetAvatarState(_buyerAvatarAddress);
            foreach (var result in action.buyerResult.purchaseResults)
            {
                Assert.Equal(BuyMultiple.ERROR_CODE_ITEM_DOES_NOT_EXIST, result.errorCode);
            }
        }

        [Fact]
        public void ExecuteInsufficientBalanceError()
        {
            var shopState = _initialState.GetShopState();

            var sellerAvatarAddress = new PrivateKey().ToAddress();
            var sellerAgentAddress = new PrivateKey().ToAddress();
            var (avatarState, agentState) = CreateAvatarState(sellerAgentAddress, sellerAvatarAddress);

            var equipment = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                1);
            shopState.Register(new ShopItem(
                sellerAgentAddress,
                sellerAvatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(_goldCurrencyState.Currency, 1, 0),
                100,
                equipment));

            var costume = ItemFactory.CreateCostume(
                _tableSheets.CostumeItemSheet.First,
                Guid.NewGuid());
            shopState.Register(new ShopItem(
                sellerAgentAddress,
                sellerAvatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                100,
                costume));

            _initialState = _initialState
                .SetState(Addresses.Shop, shopState.Serialize());
            shopState = _initialState.GetShopState();
            Assert.NotEmpty(shopState.Products);

            var products = shopState.Products.Values
                .Select(p => new BuyMultiple.PurchaseInfo(
                    p.ProductId,
                    p.SellerAgentAddress,
                    p.SellerAvatarAddress))
                .ToList();
            Assert.NotEmpty(products);

            var balance = _initialState.GetBalance(_buyerAgentAddress, _goldCurrencyState.Currency);
            _initialState = _initialState.BurnAsset(_buyerAgentAddress, balance);

            var action = new BuyMultiple
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = products,
            };

            action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Signer = _buyerAgentAddress,
            });

            var results = action.buyerResult.purchaseResults;
            var isAllFailed = results.Any(r => r.errorCode == BuyMultiple.ERROR_CODE_INSUFFICIENT_BALANCE);
            Assert.True(isAllFailed);
        }

        [Fact]
        public void ExecuteThrowShopItemExpiredError()
        {
            var sellerAvatarAddress = new PrivateKey().ToAddress();
            var sellerAgentAddress = new PrivateKey().ToAddress();
            var (avatarState, agentState) = CreateAvatarState(sellerAgentAddress, sellerAvatarAddress);

            IAccountStateDelta previousStates = _initialState;
            var shopState = previousStates.GetShopState();

            var productId = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                10);
            shopState.Register(new ShopItem(
                sellerAgentAddress,
                sellerAvatarAddress,
                productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                10,
                equipment));

            previousStates = previousStates
                .SetState(Addresses.Shop, shopState.Serialize());
            shopState = previousStates.GetShopState();

            Assert.True(shopState.Products.ContainsKey(productId));
            var products = shopState.Products.Values
                .Select(p => new BuyMultiple.PurchaseInfo(
                    p.ProductId,
                    p.SellerAgentAddress,
                    p.SellerAvatarAddress))
                .ToList();

            var action = new BuyMultiple
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = products,
            };

            action.Execute(new ActionContext()
            {
                BlockIndex = 11,
                PreviousStates = previousStates,
                Random = new TestRandom(),
                Signer = _buyerAgentAddress,
            });

            var results = action.buyerResult.purchaseResults;
            var isAllFailed = results.Any(r => r.errorCode == BuyMultiple.ERROR_CODE_SHOPITEM_EXPIRED);
            Assert.True(isAllFailed);
        }

        [Fact]
        public void SerializeWithDotnetAPI()
        {
            var sellerAvatarAddress = new PrivateKey().ToAddress();
            var sellerAgentAddress = new PrivateKey().ToAddress();
            CreateAvatarState(sellerAgentAddress, sellerAvatarAddress);

            IAccountStateDelta previousStates = _initialState;
            var shopState = previousStates.GetShopState();

            var productId = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                0);
            shopState.Register(new ShopItem(
                sellerAgentAddress,
                sellerAvatarAddress,
                productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                100,
                equipment));
            shopState.Register(new ShopItem(
                sellerAgentAddress,
                sellerAvatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                100,
                equipment));
            var products = shopState.Products.Values
                .Select(p => new BuyMultiple.PurchaseInfo(
                    p.ProductId,
                    p.SellerAgentAddress,
                    p.SellerAvatarAddress))
                .ToList();

            previousStates = previousStates
                .SetState(Addresses.Shop, shopState.Serialize());

            var action = new BuyMultiple
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = products,
            };
            action.Execute(new ActionContext()
            {
                BlockIndex = 1,
                PreviousStates = previousStates,
                Random = new TestRandom(),
                Signer = _buyerAgentAddress,
            });

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, action);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (BuyMultiple)formatter.Deserialize(ms);
            Assert.Equal(action.PlainValue, deserialized.PlainValue);
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
            _sellerAgentStateMap[avatarState] = agentState;

            _initialState = _initialState
                .SetState(agentAddress, agentState.Serialize())
                .SetState(avatarAddress, avatarState.Serialize());
            return (avatarState, agentState);
        }

        private struct PriceData
        {
            public FungibleAssetValue TaxSum;
            public Dictionary<Address, FungibleAssetValue> TaxedPriceSum;
            public FungibleAssetValue PriceSum;

            public PriceData(Currency currency)
            {
                TaxSum = new FungibleAssetValue(currency, 0, 0);
                TaxedPriceSum = new Dictionary<Address, FungibleAssetValue>();
                PriceSum = new FungibleAssetValue(currency, 0, 0);
            }
        }

        public class ShopItemData
        {
            public ItemType ItemType { get; set; }

            public Guid ItemId { get; set; }

            public Address SellerAgentAddress { get; set; }

            public Address SellerAvatarAddress { get; set; }

            public BigInteger Price { get; set; }

            public long RequiredBlockIndex { get; set; }

            public bool Buy { get; set; }

            public bool ContainsInInventory { get; set; }
        }
    }
}
