namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.Market;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class MarketScenarioTest
    {
        private readonly Address _sellerAgentAddress;
        private readonly Address _sellerAvatarAddress;
        private readonly AvatarState _sellerAvatarState;
        private readonly Address _sellerAgentAddress2;
        private readonly Address _sellerAvatarAddress2;
        private readonly AvatarState _sellerAvatarState2;
        private readonly Address _buyerAgentAddress;
        private readonly Address _buyerAvatarAddress;
        private readonly TableSheets _tableSheets;
        private readonly Currency _currency;
        private readonly GameConfigState _gameConfigState;
        private IAccountStateDelta _initialState;

        public MarketScenarioTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _sellerAgentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_sellerAgentAddress);
            _sellerAvatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _gameConfigState = new GameConfigState((Text)_tableSheets.GameConfigSheet.Serialize());
            _sellerAvatarState = new AvatarState(
                _sellerAvatarAddress,
                _sellerAgentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                _gameConfigState,
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            agentState.avatarAddresses[0] = _sellerAvatarAddress;

            _sellerAgentAddress2 = new PrivateKey().ToAddress();
            var agentState2 = new AgentState(_sellerAgentAddress2);
            _sellerAvatarAddress2 = new PrivateKey().ToAddress();
            _sellerAvatarState2 = new AvatarState(
                _sellerAvatarAddress2,
                _sellerAgentAddress2,
                0,
                _tableSheets.GetAvatarSheets(),
                _gameConfigState,
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            agentState2.avatarAddresses[0] = _sellerAvatarAddress2;

            _buyerAgentAddress = new PrivateKey().ToAddress();
            var agentState3 = new AgentState(_buyerAgentAddress);
            _buyerAvatarAddress = new PrivateKey().ToAddress();
            var buyerAvatarState = new AvatarState(
                _buyerAvatarAddress,
                _buyerAgentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                _gameConfigState,
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            agentState3.avatarAddresses[0] = _buyerAvatarAddress;

            _currency = Currency.Legacy("NCG", 2, minters: null);
            _initialState = new Tests.Action.MockStateDelta()
                .SetState(GoldCurrencyState.Address, new GoldCurrencyState(_currency).Serialize())
                .SetState(Addresses.GameConfig, _gameConfigState.Serialize())
                .SetState(Addresses.GetSheetAddress<MaterialItemSheet>(), _tableSheets.MaterialItemSheet.Serialize())
                .SetState(Addresses.GetSheetAddress<ArenaSheet>(), _tableSheets.ArenaSheet.Serialize())
                .SetState(_sellerAgentAddress, agentState.Serialize())
                .SetState(_sellerAvatarAddress, _sellerAvatarState.Serialize())
                .SetState(_sellerAgentAddress2, agentState2.Serialize())
                .SetState(_sellerAvatarAddress2, _sellerAvatarState2.Serialize())
                .SetState(_buyerAgentAddress, agentState3.Serialize())
                .SetState(_buyerAvatarAddress, buyerAvatarState.Serialize());
        }

        [Fact]
        public void Register_And_Buy()
        {
            var context = new ActionContext();
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var tradableMaterial = ItemFactory.CreateTradableMaterial(materialRow);
            _sellerAvatarState.inventory.AddItem(tradableMaterial);
            var id = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, id, 0L);
            _sellerAvatarState2.inventory.AddItem(equipment);
            _initialState = _initialState
                .SetState(_sellerAvatarAddress, _sellerAvatarState.Serialize())
                .SetState(_sellerAvatarAddress2, _sellerAvatarState2.Serialize())
                .MintAsset(context, _buyerAgentAddress, 4 * _currency)
                .MintAsset(context, _sellerAvatarAddress, 1 * RuneHelper.StakeRune)
                .MintAsset(context, _sellerAvatarAddress2, 1 * RuneHelper.DailyRewardRune);

            var random = new TestRandom();
            var productInfoList = new List<IProductInfo>();
            var action = new RegisterProduct
            {
                AvatarAddress = _sellerAvatarAddress,
                RegisterInfos = new List<IRegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = tradableMaterial.TradableId,
                        Type = ProductType.Fungible,
                    },
                    new AssetInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        Price = 1 * _currency,
                        Asset = 1 * RuneHelper.StakeRune,
                        Type = ProductType.FungibleAssetValue,
                    },
                },
            };
            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 1L,
                PreviousState = _initialState,
                Random = random,
                Signer = _sellerAgentAddress,
            });
            var nextAvatarState = nextState.GetAvatarStateV2(_sellerAvatarAddress);
            Assert.Empty(nextAvatarState.inventory.Items);
            Assert.Equal(_gameConfigState.ActionPointMax - RegisterProduct.CostAp, nextAvatarState.actionPoint);

            var productsState =
                new ProductsState((List)nextState.GetState(ProductsState.DeriveAddress(_sellerAvatarAddress)));
            Assert.Equal(2, productsState.ProductIds.Count);
            foreach (var productId in productsState.ProductIds)
            {
                var product =
                    ProductFactory.DeserializeProduct((List)nextState.GetState(Product.DeriveAddress(productId)));
                ProductType productType;
                switch (product)
                {
                    case FavProduct favProduct:
                        productType = ProductType.FungibleAssetValue;
                        productInfoList.Add(new FavProductInfo
                        {
                            AgentAddress = _sellerAgentAddress,
                            AvatarAddress = _sellerAvatarAddress,
                            Price = product.Price,
                            ProductId = productId,
                            Type = productType,
                        });
                        break;
                    case ItemProduct itemProduct:
                        productType = itemProduct.TradableItem is TradableMaterial
                            ? ProductType.Fungible
                            : ProductType.NonFungible;
                        productInfoList.Add(new ItemProductInfo
                        {
                            AgentAddress = _sellerAgentAddress,
                            AvatarAddress = _sellerAvatarAddress,
                            Price = product.Price,
                            ProductId = productId,
                            Type = productType,
                            ItemSubType = itemProduct.TradableItem.ItemSubType,
                            TradableId = itemProduct.TradableItem.TradableId,
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(product));
                }
            }

            var action2 = new RegisterProduct
            {
                AvatarAddress = _sellerAvatarAddress2,
                RegisterInfos = new List<IRegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress2,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = equipment.TradableId,
                        Type = ProductType.NonFungible,
                    },
                    new AssetInfo
                    {
                        AvatarAddress = _sellerAvatarAddress2,
                        Price = 1 * _currency,
                        Asset = 1 * RuneHelper.DailyRewardRune,
                        Type = ProductType.FungibleAssetValue,
                    },
                },
            };
            var nextState2 = action2.Execute(new ActionContext
            {
                BlockIndex = 2L,
                PreviousState = nextState,
                Random = random,
                Signer = _sellerAgentAddress2,
            });
            var nextAvatarState2 = nextState2.GetAvatarStateV2(_sellerAvatarAddress2);
            Assert.Empty(nextAvatarState2.inventory.Items);
            Assert.Equal(_gameConfigState.ActionPointMax - RegisterProduct.CostAp, nextAvatarState2.actionPoint);

            var productList2 =
                new ProductsState((List)nextState2.GetState(ProductsState.DeriveAddress(_sellerAvatarAddress2)));
            Assert.Equal(2, productList2.ProductIds.Count);
            foreach (var productId in productList2.ProductIds)
            {
                var product =
                    ProductFactory.DeserializeProduct((List)nextState2.GetState(Product.DeriveAddress(productId)));
                ProductType productType;
                switch (product)
                {
                    case FavProduct favProduct:
                        productType = ProductType.FungibleAssetValue;
                        productInfoList.Add(new FavProductInfo
                        {
                            AgentAddress = _sellerAgentAddress2,
                            AvatarAddress = _sellerAvatarAddress2,
                            Price = product.Price,
                            ProductId = productId,
                            Type = productType,
                        });
                        break;
                    case ItemProduct itemProduct:
                        productType = itemProduct.TradableItem is TradableMaterial
                            ? ProductType.Fungible
                            : ProductType.NonFungible;
                        productInfoList.Add(new ItemProductInfo
                        {
                            AgentAddress = _sellerAgentAddress2,
                            AvatarAddress = _sellerAvatarAddress2,
                            Price = product.Price,
                            ProductId = productId,
                            Type = productType,
                            ItemSubType = itemProduct.TradableItem.ItemSubType,
                            TradableId = itemProduct.TradableItem.TradableId,
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(product));
                }
            }

            var action3 = new BuyProduct
            {
                AvatarAddress = _buyerAvatarAddress,
                ProductInfos = productInfoList,
            };

            var latestState = action3.Execute(new ActionContext
            {
                BlockIndex = 3L,
                PreviousState = nextState2,
                Random = random,
                Signer = _buyerAgentAddress,
            });

            var buyerAvatarState = latestState.GetAvatarStateV2(_buyerAvatarAddress);
            var arenaData = _tableSheets.ArenaSheet.GetRoundByBlockIndex(3L);
            var feeStoreAddress = Addresses.GetShopFeeAddress(arenaData.ChampionshipId, arenaData.Round);
            var totalTax = 0 * _currency;
            foreach (var group in action3.ProductInfos.GroupBy(p => p.AgentAddress))
            {
                var sellerAgentAddress = group.Key;
                var totalPrice = 2 * _currency;
                var tax = totalPrice.DivRem(100, out _) * Buy.TaxRate;
                totalTax += tax;
                var taxedPrice = totalPrice - tax;
                Assert.Equal(taxedPrice, latestState.GetBalance(sellerAgentAddress, _currency));
                foreach (var productInfo in group)
                {
                    var sellerAvatarState = latestState.GetAvatarStateV2(productInfo.AvatarAddress);
                    var sellProductList = new ProductsState((List)latestState.GetState(ProductsState.DeriveAddress(productInfo.AvatarAddress)));
                    var productId = productInfo.ProductId;
                    Assert.Empty(sellProductList.ProductIds);
                    Assert.Equal(Null.Value, latestState.GetState(Product.DeriveAddress(productId)));
                    var product = ProductFactory.DeserializeProduct((List)nextState2.GetState(Product.DeriveAddress(productId)));
                    switch (product)
                    {
                        case FavProduct favProduct:
                            Assert.Equal(favProduct.Asset, latestState.GetBalance(_buyerAvatarAddress, favProduct.Asset.Currency));
                            break;
                        case ItemProduct itemProduct:
                            Assert.True(buyerAvatarState.inventory.HasTradableItem(itemProduct.TradableItem.TradableId, 1L, itemProduct.ItemCount));
                            break;
                    }

                    var receipt = new ProductReceipt((List)latestState.GetState(ProductReceipt.DeriveAddress(productId)));
                    Assert.Equal(productId, receipt.ProductId);
                    Assert.Equal(productInfo.AvatarAddress, receipt.SellerAvatarAddress);
                    Assert.Equal(_buyerAvatarAddress, receipt.BuyerAvatarAddress);
                    Assert.Equal(1 * _currency, receipt.Price);
                    Assert.Equal(3L, receipt.PurchasedBlockIndex);
                    Assert.Contains(sellerAvatarState.mailBox.OfType<ProductSellerMail>(), m => m.ProductId == productInfo.ProductId);
                    Assert.Contains(buyerAvatarState.mailBox.OfType<ProductBuyerMail>(), m => m.ProductId == productInfo.ProductId);
                }
            }

            Assert.True(totalTax > 0 * _currency);
            Assert.Equal(0 * _currency, latestState.GetBalance(_buyerAgentAddress, _currency));
            Assert.Equal(totalTax, latestState.GetBalance(feeStoreAddress, _currency));
        }

        [Fact]
        public void Register_And_Cancel()
        {
            var context = new ActionContext();
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var tradableMaterial = ItemFactory.CreateTradableMaterial(materialRow);
            _sellerAvatarState.inventory.AddItem(tradableMaterial);
            var id = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, id, 0L);
            _sellerAvatarState.inventory.AddItem(equipment);
            Assert.Equal(2, _sellerAvatarState.inventory.Items.Count);
            _initialState = _initialState
                    .SetState(_sellerAvatarAddress, _sellerAvatarState.Serialize())
                    .MintAsset(context, _sellerAvatarAddress, 1 * RuneHelper.StakeRune);
            var action = new RegisterProduct
            {
                AvatarAddress = _sellerAvatarAddress,
                RegisterInfos = new List<IRegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = tradableMaterial.TradableId,
                        Type = ProductType.Fungible,
                    },
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = equipment.TradableId,
                        Type = ProductType.NonFungible,
                    },
                    new AssetInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        Price = 1 * _currency,
                        Type = ProductType.FungibleAssetValue,
                        Asset = 1 * RuneHelper.StakeRune,
                    },
                },
            };
            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 1L,
                PreviousState = _initialState,
                Random = new TestRandom(),
                Signer = _sellerAgentAddress,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(_sellerAvatarAddress);
            Assert.Empty(nextAvatarState.inventory.Items);
            Assert.Equal(_gameConfigState.ActionPointMax - RegisterProduct.CostAp, nextAvatarState.actionPoint);

            var marketState = new MarketState(nextState.GetState(Addresses.Market));
            Assert.Contains(_sellerAvatarAddress, marketState.AvatarAddresses);

            var productsStateAddress = ProductsState.DeriveAddress(_sellerAvatarAddress);
            var productsState = new ProductsState((List)nextState.GetState(productsStateAddress));
            var random = new TestRandom();
            Guid fungibleProductId = default;
            Guid nonFungibleProductId = default;
            Guid assetProductId = default;
            for (int i = 0; i < 3; i++)
            {
                var guid = random.GenerateRandomGuid();

                Assert.Contains(guid, productsState.ProductIds);
                var productAddress = Product.DeriveAddress(guid);
                var product = ProductFactory.DeserializeProduct((List)nextState.GetState(productAddress));
                Assert.Equal(product.ProductId, guid);
                Assert.Equal(1 * _currency, product.Price);
                switch (product)
                {
                    case FavProduct favProduct:
                        assetProductId = favProduct.ProductId;
                        Assert.Equal(1 * RuneHelper.StakeRune, favProduct.Asset);
                        break;
                    case ItemProduct itemProduct:
                        if (itemProduct.Type == ProductType.Fungible)
                        {
                            fungibleProductId = itemProduct.ProductId;
                        }
                        else
                        {
                            nonFungibleProductId = itemProduct.ProductId;
                        }

                        Assert.Equal(1, itemProduct.ItemCount);
                        Assert.NotNull(itemProduct.TradableItem);
                        break;
                }
            }

            Assert.All(new[] { nonFungibleProductId, fungibleProductId, assetProductId }, productId => Assert.NotEqual(default, productId));
            var action2 = new CancelProductRegistration
            {
                AvatarAddress = _sellerAvatarAddress,
                ProductInfos = new List<IProductInfo>
                {
                    new ItemProductInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        AgentAddress = _sellerAgentAddress,
                        Price = 1 * _currency,
                        ProductId = fungibleProductId,
                        Type = ProductType.Fungible,
                        ItemSubType = tradableMaterial.ItemSubType,
                        TradableId = tradableMaterial.TradableId,
                    },
                    new ItemProductInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        AgentAddress = _sellerAgentAddress,
                        Price = 1 * _currency,
                        ProductId = nonFungibleProductId,
                        Type = ProductType.NonFungible,
                        ItemSubType = equipment.ItemSubType,
                        TradableId = equipment.TradableId,
                    },
                    new FavProductInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        AgentAddress = _sellerAgentAddress,
                        Price = 1 * _currency,
                        ProductId = assetProductId,
                        Type = ProductType.FungibleAssetValue,
                    },
                },
            };
            var latestState = action2.Execute(new ActionContext
            {
                BlockIndex = 2L,
                PreviousState = nextState,
                Random = new TestRandom(),
                Signer = _sellerAgentAddress,
            });

            var latestAvatarState = latestState.GetAvatarStateV2(_sellerAvatarAddress);
            foreach (var productInfo in action2.ProductInfos)
            {
                Assert.Contains(
                    latestAvatarState.mailBox.OfType<ProductCancelMail>(),
                    m => m.ProductId == productInfo.ProductId
                );
            }

            Assert.Equal(_gameConfigState.ActionPointMax - RegisterProduct.CostAp - CancelProductRegistration.CostAp, latestAvatarState.actionPoint);

            var sellProductList = new ProductsState((List)latestState.GetState(productsStateAddress));
            Assert.Empty(sellProductList.ProductIds);

            foreach (var productAddress in action2.ProductInfos.Select(productInfo => Product.DeriveAddress(productInfo.ProductId)))
            {
                Assert.Equal(Null.Value, latestState.GetState(productAddress));
                var product = ProductFactory.DeserializeProduct((List)nextState.GetState(productAddress));
                switch (product)
                {
                    case FavProduct favProduct:
                        Assert.Equal(0 * RuneHelper.StakeRune, latestState.GetBalance(Product.DeriveAddress(favProduct.ProductId), RuneHelper.StakeRune));
                        Assert.Equal(favProduct.Asset, latestState.GetBalance(_sellerAvatarAddress, RuneHelper.StakeRune));
                        break;
                    case ItemProduct itemProduct:
                        Assert.True(latestAvatarState.inventory.HasTradableItem(itemProduct.TradableItem.TradableId, 1L, itemProduct.ItemCount));
                        break;
                }
            }
        }

        [Fact]
        public void Register_And_ReRegister()
        {
            var context = new ActionContext();
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var tradableMaterial = ItemFactory.CreateTradableMaterial(materialRow);
            _sellerAvatarState.inventory.AddItem(tradableMaterial, 2);
            var id = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, id, 0L);
            _sellerAvatarState.inventory.AddItem(equipment);
            Assert.Equal(2, _sellerAvatarState.inventory.Items.Count);
            _initialState = _initialState
                .MintAsset(context, _sellerAvatarAddress, 2 * RuneHelper.StakeRune)
                .SetState(_sellerAvatarAddress, _sellerAvatarState.Serialize());
            var action = new RegisterProduct
            {
                AvatarAddress = _sellerAvatarAddress,
                RegisterInfos = new List<IRegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        ItemCount = 2,
                        Price = 1 * _currency,
                        TradableId = tradableMaterial.TradableId,
                        Type = ProductType.Fungible,
                    },
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = equipment.TradableId,
                        Type = ProductType.NonFungible,
                    },
                    new AssetInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        Price = 1 * _currency,
                        Asset = 2 * RuneHelper.StakeRune,
                        Type = ProductType.FungibleAssetValue,
                    },
                },
            };
            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 1L,
                PreviousState = _initialState,
                Random = new TestRandom(),
                Signer = _sellerAgentAddress,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(_sellerAvatarAddress);
            Assert.Empty(nextAvatarState.inventory.Items);
            Assert.Equal(_gameConfigState.ActionPointMax - RegisterProduct.CostAp, nextAvatarState.actionPoint);

            var marketState = new MarketState(nextState.GetState(Addresses.Market));
            Assert.Contains(_sellerAvatarAddress, marketState.AvatarAddresses);

            var productsStateAddress = ProductsState.DeriveAddress(_sellerAvatarAddress);
            var productsState = new ProductsState((List)nextState.GetState(productsStateAddress));
            var random = new TestRandom();
            Guid fungibleProductId = default;
            Guid nonFungibleProductId = default;
            Guid assetProductId = default;
            for (int i = 0; i < 3; i++)
            {
                var guid = random.GenerateRandomGuid();
                switch (i)
                {
                    case 0:
                        fungibleProductId = guid;
                        break;
                    case 1:
                        assetProductId = guid;
                        break;
                    case 2:
                        nonFungibleProductId = guid;
                        break;
                }

                Assert.Contains(guid, productsState.ProductIds);
                var productAddress = Product.DeriveAddress(guid);
                var product = ProductFactory.DeserializeProduct((List)nextState.GetState(productAddress));
                switch (product)
                {
                    case FavProduct favProduct:
                        Assert.Equal(0 * RuneHelper.StakeRune, nextState.GetBalance(_sellerAvatarAddress, RuneHelper.StakeRune));
                        Assert.Equal(favProduct.Asset, nextState.GetBalance(Product.DeriveAddress(favProduct.ProductId), RuneHelper.StakeRune));
                        break;
                    case ItemProduct itemProduct:
                    {
                        var registerInfo =
                            action.RegisterInfos.OfType<RegisterInfo>().First(r =>
                                r.TradableId == itemProduct.TradableItem.TradableId);
                        Assert.Equal(product.ProductId, guid);
                        Assert.Equal(registerInfo.Price, product.Price);
                        Assert.Equal(registerInfo.ItemCount, itemProduct.ItemCount);
                        Assert.NotNull(itemProduct.TradableItem);
                        break;
                    }
                }
            }

            var action2 = new ReRegisterProduct
            {
                AvatarAddress = _sellerAvatarAddress,
                ReRegisterInfos = new List<(IProductInfo, IRegisterInfo)>
                {
                    (
                        new ItemProductInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            AgentAddress = _sellerAgentAddress,
                            Price = 1 * _currency,
                            ProductId = fungibleProductId,
                            Type = ProductType.Fungible,
                            ItemSubType = tradableMaterial.ItemSubType,
                            TradableId = tradableMaterial.TradableId,
                        },
                        new RegisterInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            ItemCount = 1,
                            Price = 1 * _currency,
                            TradableId = tradableMaterial.TradableId,
                            Type = ProductType.Fungible,
                        }
                    ),
                    (
                        new ItemProductInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            AgentAddress = _sellerAgentAddress,
                            Price = 1 * _currency,
                            ProductId = nonFungibleProductId,
                            Type = ProductType.NonFungible,
                            ItemSubType = equipment.ItemSubType,
                            TradableId = equipment.TradableId,
                        },
                        new RegisterInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            ItemCount = 1,
                            Price = 1 * _currency,
                            TradableId = equipment.TradableId,
                            Type = ProductType.NonFungible,
                        }
                    ),
                    (
                        new FavProductInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            AgentAddress = _sellerAgentAddress,
                            Price = 1 * _currency,
                            ProductId = assetProductId,
                            Type = ProductType.FungibleAssetValue,
                        },
                        new AssetInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            Price = 1 * _currency,
                            Asset = 1 * RuneHelper.StakeRune,
                            Type = ProductType.FungibleAssetValue,
                        }
                    ),
                },
            };
            var latestState = action2.Execute(new ActionContext
            {
                BlockIndex = 2L,
                PreviousState = nextState,
                Random = random,
                Signer = _sellerAgentAddress,
            });

            var latestAvatarState = latestState.GetAvatarStateV2(_sellerAvatarAddress);
            Assert.Equal(_gameConfigState.ActionPointMax - RegisterProduct.CostAp - ReRegisterProduct.CostAp, latestAvatarState.actionPoint);
            var inventoryItem = Assert.Single(latestAvatarState.inventory.Items);
            Assert.Equal(1, inventoryItem.count);
            Assert.IsType<TradableMaterial>(inventoryItem.item);
            var sellProductList = new ProductsState((List)latestState.GetState(productsStateAddress));
            Assert.Equal(3, sellProductList.ProductIds.Count);
            foreach (var prevProductId in productsState.ProductIds)
            {
                Assert.DoesNotContain(prevProductId, sellProductList.ProductIds);
            }

            foreach (var newProductId in sellProductList.ProductIds)
            {
                var productAddress = Product.DeriveAddress(newProductId);
                var product = ProductFactory.DeserializeProduct((List)latestState.GetState(productAddress));
                switch (product)
                {
                    case FavProduct favProduct:
                        Assert.Equal(0 * RuneHelper.StakeRune, latestState.GetBalance(Product.DeriveAddress(assetProductId), RuneHelper.StakeRune));
                        Assert.Equal(1 * RuneHelper.StakeRune, latestState.GetBalance(_sellerAvatarAddress, RuneHelper.StakeRune));
                        Assert.Equal(favProduct.Asset, latestState.GetBalance(Product.DeriveAddress(favProduct.ProductId), RuneHelper.StakeRune));
                        break;
                    case ItemProduct itemProduct:
                    {
                        var registerInfo =
                            action2.ReRegisterInfos.Select(r => r.Item2).OfType<RegisterInfo>().First(r =>
                                r.TradableId == itemProduct.TradableItem.TradableId);
                        Assert.Equal(product.ProductId, newProductId);
                        Assert.Equal(registerInfo.Price, product.Price);
                        Assert.Equal(registerInfo.ItemCount, itemProduct.ItemCount);
                        Assert.NotNull(itemProduct.TradableItem);
                        break;
                    }
                }
            }
        }

        [Fact]
        public void ReRegister_Order()
        {
            var materialRow = _tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Hourglass);
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var tradableMaterial = ItemFactory.CreateTradableMaterial(materialRow);
            _sellerAvatarState.inventory.AddItem(tradableMaterial);
            var id = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, id, 0L);
            _sellerAvatarState.inventory.AddItem(equipment);
            _initialState = _initialState
                .SetState(_sellerAvatarAddress, _sellerAvatarState.Serialize());

            var digestListAddress = OrderDigestListState.DeriveAddress(_sellerAvatarAddress);
            var orderDigestList = new OrderDigestListState(digestListAddress);
            var reRegisterInfoList = new List<(IProductInfo, IRegisterInfo)>();
            var shopAddressList = new List<Address>();
            foreach (var inventoryItem in _sellerAvatarState.inventory.Items.ToList())
            {
                var tradableItem = (ITradableItem)inventoryItem.item;
                var itemSubType = tradableItem.ItemSubType;
                var orderId = Guid.NewGuid();
                var shardedShopAddress = ShardedShopStateV2.DeriveAddress(itemSubType, orderId);
                var shopState = new ShardedShopStateV2(shardedShopAddress);
                var order = OrderFactory.Create(
                    _sellerAgentAddress,
                    _sellerAvatarAddress,
                    orderId,
                    _currency * 1,
                    tradableItem.TradableId,
                    Order.ExpirationInterval,
                    itemSubType,
                    1
                );
                var sellItem = order.Sell(_sellerAvatarState);
                var orderDigest = order.Digest(_sellerAvatarState, _tableSheets.CostumeStatSheet);
                shopState.Add(orderDigest, Order.ExpirationInterval);
                orderDigestList.Add(orderDigest);
                Assert.True(_sellerAvatarState.inventory.TryGetLockedItem(new OrderLock(orderId), out _));
                _initialState = _initialState.SetState(Addresses.GetItemAddress(tradableItem.TradableId), sellItem.Serialize())
                    .SetState(Order.DeriveAddress(order.OrderId), order.Serialize())
                    .SetState(digestListAddress, orderDigestList.Serialize())
                    .SetState(shardedShopAddress, shopState.Serialize())
                    .SetState(_sellerAvatarAddress, _sellerAvatarState.Serialize());

                var productType = tradableItem is TradableMaterial
                    ? ProductType.Fungible
                    : ProductType.NonFungible;
                var productInfo = new ItemProductInfo
                {
                    AgentAddress = _sellerAgentAddress,
                    AvatarAddress = _sellerAvatarAddress,
                    Price = 1 * _currency,
                    ProductId = orderId,
                    Type = productType,
                    Legacy = true,
                };
                var registerInfo = new RegisterInfo
                {
                    AvatarAddress = _sellerAvatarAddress,
                    ItemCount = 1,
                    Price = 100 * _currency,
                    TradableId = tradableItem.TradableId,
                    Type = productType,
                };

                reRegisterInfoList.Add((productInfo, registerInfo));
                shopAddressList.Add(shardedShopAddress);
            }

            var action = new ReRegisterProduct
            {
                AvatarAddress = _sellerAvatarAddress,
                ReRegisterInfos = reRegisterInfoList,
            };
            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 2L,
                PreviousState = _initialState,
                Random = new TestRandom(),
                Signer = _sellerAgentAddress,
            });

            Assert.Empty(new OrderDigestListState((Dictionary)nextState.GetState(digestListAddress)).OrderDigestList);
            Assert.Contains(
                _sellerAvatarAddress,
                new MarketState((List)nextState.GetState(Addresses.Market)).AvatarAddresses
            );
            var productsState =
                new ProductsState(
                    (List)nextState.GetState(ProductsState.DeriveAddress(_sellerAvatarAddress)));
            Assert.Equal(2, productsState.ProductIds.Count);
            foreach (var productId in productsState.ProductIds)
            {
                var productAddress = Product.DeriveAddress(productId);
                var product = ProductFactory.DeserializeProduct((List)nextState.GetState(productAddress));
                Assert.Equal(100 * _currency, product.Price);
            }

            var nextAvatarState = nextState.GetAvatarStateV2(_sellerAvatarAddress);
            Assert.Equal(_gameConfigState.ActionPointMax - ReRegisterProduct.CostAp, nextAvatarState.actionPoint);
            Assert.Empty(nextAvatarState.inventory.Items);

            foreach (var shopAddress in shopAddressList)
            {
                var shopState =
                    new ShardedShopStateV2((Dictionary)nextState.GetState(shopAddress));
                Assert.Empty(shopState.OrderDigestList);
            }
        }

        [Fact]
        public void HardFork()
        {
            var context = new ActionContext();
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var tradableMaterial = ItemFactory.CreateTradableMaterial(materialRow);
            _sellerAvatarState.inventory.AddItem(tradableMaterial);
            var id = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, id, 100L);
            _sellerAvatarState.inventory.AddItem(equipment);
            Assert.Equal(2, _sellerAvatarState.inventory.Items.Count);
            _initialState = _initialState
                .SetState(_sellerAvatarAddress, _sellerAvatarState.Serialize())
                .MintAsset(context, _buyerAgentAddress, 3 * _currency)
                .MintAsset(context, _sellerAvatarAddress, 1 * RuneHelper.StakeRune);
            var action = new RegisterProduct0
            {
                AvatarAddress = _sellerAvatarAddress,
                RegisterInfos = new List<IRegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = tradableMaterial.TradableId,
                        Type = ProductType.Fungible,
                    },
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = equipment.TradableId,
                        Type = ProductType.NonFungible,
                    },
                    new AssetInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        Price = 1 * _currency,
                        Type = ProductType.FungibleAssetValue,
                        Asset = 1 * RuneHelper.StakeRune,
                    },
                },
            };
            var random = new TestRandom();
            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 1L,
                PreviousState = _initialState,
                Random = random,
                Signer = _sellerAgentAddress,
            });
            Guid fungibleProductId = default;
            Guid nonFungibleProductId = default;
            Guid assetProductId = default;
            var productsStateAddress = ProductsState.DeriveAddress(_sellerAvatarAddress);
            var productsState = new ProductsState((List)nextState.GetState(productsStateAddress));
            foreach (var product in productsState.ProductIds.Select(Product.DeriveAddress)
                         .Select(productAddress =>
                             ProductFactory.DeserializeProduct(
                                 (List)nextState.GetState(productAddress))))
            {
                switch (product)
                {
                    case FavProduct favProduct:
                        assetProductId = favProduct.ProductId;
                        break;
                    case ItemProduct itemProduct:
                        if (itemProduct.Type == ProductType.Fungible)
                        {
                            fungibleProductId = itemProduct.ProductId;
                            Assert.Equal(0L, itemProduct.TradableItem.RequiredBlockIndex);
                        }
                        else
                        {
                            nonFungibleProductId = itemProduct.ProductId;
                            Assert.Equal(100L, itemProduct.TradableItem.RequiredBlockIndex);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(product));
                }
            }

            var productInfos = new List<IProductInfo>
            {
                new ItemProductInfo
                {
                    AvatarAddress = _sellerAvatarAddress,
                    AgentAddress = _sellerAgentAddress,
                    Price = 1 * _currency,
                    ProductId = fungibleProductId,
                    Type = ProductType.Fungible,
                    ItemSubType = tradableMaterial.ItemSubType,
                    TradableId = tradableMaterial.TradableId,
                },
                new ItemProductInfo
                {
                    AvatarAddress = _sellerAvatarAddress,
                    AgentAddress = _sellerAgentAddress,
                    Price = 1 * _currency,
                    ProductId = nonFungibleProductId,
                    Type = ProductType.NonFungible,
                    ItemSubType = equipment.ItemSubType,
                    TradableId = equipment.TradableId,
                },
                new FavProductInfo
                {
                    AvatarAddress = _sellerAvatarAddress,
                    AgentAddress = _sellerAgentAddress,
                    Price = 1 * _currency,
                    ProductId = assetProductId,
                    Type = ProductType.FungibleAssetValue,
                },
            };
            //Cancel
            var cancelAction = new CancelProductRegistration
            {
                AvatarAddress = _sellerAvatarAddress,
                ProductInfos = productInfos,
            };
            var canceledState = cancelAction.Execute(new ActionContext
            {
                BlockIndex = 2L,
                PreviousState = nextState,
                Random = random,
                Signer = _sellerAgentAddress,
            });
            var avatarState = canceledState.GetAvatarStateV2(_sellerAvatarAddress);
            Assert.Equal(2, avatarState.inventory.Items.Count);
            Assert.True(avatarState.inventory.TryGetTradableItem(tradableMaterial.TradableId, 0L, 1, out var materialItem));
            Assert.True(avatarState.inventory.TryGetNonFungibleItem(equipment.TradableId, out var equipmentItem));
            var canceledEquipment = Assert.IsAssignableFrom<ItemUsable>(equipmentItem.item);
            Assert.Equal(100L, canceledEquipment.RequiredBlockIndex);
            Assert.Equal(
                1 * Currencies.StakeRune,
                canceledState.GetBalance(_sellerAvatarAddress, Currencies.StakeRune)
            );

            //ReRegister
            var reRegisterAction = new ReRegisterProduct
            {
                AvatarAddress = _sellerAvatarAddress,
                ReRegisterInfos = new List<(IProductInfo, IRegisterInfo)>
                {
                    (
                        new ItemProductInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            AgentAddress = _sellerAgentAddress,
                            Price = 1 * _currency,
                            ProductId = fungibleProductId,
                            Type = ProductType.Fungible,
                            ItemSubType = tradableMaterial.ItemSubType,
                            TradableId = tradableMaterial.TradableId,
                        },
                        new RegisterInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            ItemCount = 1,
                            Price = 2 * _currency,
                            TradableId = tradableMaterial.TradableId,
                            Type = ProductType.Fungible,
                        }
                    ),
                    (
                        new ItemProductInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            AgentAddress = _sellerAgentAddress,
                            Price = 1 * _currency,
                            ProductId = nonFungibleProductId,
                            Type = ProductType.NonFungible,
                            ItemSubType = equipment.ItemSubType,
                            TradableId = equipment.TradableId,
                        },
                        new RegisterInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            ItemCount = 1,
                            Price = 2 * _currency,
                            TradableId = equipment.TradableId,
                            Type = ProductType.NonFungible,
                        }
                    ),
                    (
                        new FavProductInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            AgentAddress = _sellerAgentAddress,
                            Price = 1 * _currency,
                            ProductId = assetProductId,
                            Type = ProductType.FungibleAssetValue,
                        },
                        new AssetInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            Price = 2 * _currency,
                            Asset = 1 * RuneHelper.StakeRune,
                            Type = ProductType.FungibleAssetValue,
                        }
                    ),
                },
            };
            Assert.Throws<ItemDoesNotExistException>(() => reRegisterAction.Execute(new ActionContext
            {
                BlockIndex = 2L,
                PreviousState = nextState,
                Random = random,
                Signer = _sellerAgentAddress,
            }));

            //Buy
            var buyAction = new BuyProduct
            {
                AvatarAddress = _buyerAvatarAddress,
                ProductInfos = productInfos,
            };

            var tradedState = buyAction.Execute(new ActionContext
            {
                BlockIndex = 3L,
                PreviousState = nextState,
                Random = random,
                Signer = _buyerAgentAddress,
            });

            var buyerAvatarState = tradedState.GetAvatarStateV2(_buyerAvatarAddress);
            var arenaData = _tableSheets.ArenaSheet.GetRoundByBlockIndex(3L);
            var feeStoreAddress = Addresses.GetShopFeeAddress(arenaData.ChampionshipId, arenaData.Round);
            var totalTax = 0 * _currency;
            foreach (var group in buyAction.ProductInfos.GroupBy(p => p.AgentAddress))
            {
                var sellerAgentAddress = group.Key;
                var totalPrice = 3 * _currency;
                var tax = totalPrice.DivRem(100, out _) * Buy.TaxRate;
                totalTax += tax;
                var taxedPrice = totalPrice - tax;
                Assert.Equal(taxedPrice, tradedState.GetBalance(sellerAgentAddress, _currency));
                foreach (var productInfo in group)
                {
                    var sellerAvatarState = tradedState.GetAvatarStateV2(productInfo.AvatarAddress);
                    var sellProductList = new ProductsState((List)tradedState.GetState(ProductsState.DeriveAddress(productInfo.AvatarAddress)));
                    var productId = productInfo.ProductId;
                    Assert.Empty(sellProductList.ProductIds);
                    Assert.Equal(Null.Value, tradedState.GetState(Product.DeriveAddress(productId)));
                    var product = ProductFactory.DeserializeProduct((List)nextState.GetState(Product.DeriveAddress(productId)));
                    switch (product)
                    {
                        case FavProduct favProduct:
                            Assert.Equal(favProduct.Asset, tradedState.GetBalance(_buyerAvatarAddress, favProduct.Asset.Currency));
                            break;
                        case ItemProduct itemProduct:
                            Assert.True(buyerAvatarState.inventory.HasTradableItem(itemProduct.TradableItem.TradableId, 3L, itemProduct.ItemCount));
                            break;
                    }

                    var receipt = new ProductReceipt((List)tradedState.GetState(ProductReceipt.DeriveAddress(productId)));
                    Assert.Equal(productId, receipt.ProductId);
                    Assert.Equal(productInfo.AvatarAddress, receipt.SellerAvatarAddress);
                    Assert.Equal(_buyerAvatarAddress, receipt.BuyerAvatarAddress);
                    Assert.Equal(1 * _currency, receipt.Price);
                    Assert.Equal(3L, receipt.PurchasedBlockIndex);
                    Assert.Contains(sellerAvatarState.mailBox.OfType<ProductSellerMail>(), m => m.ProductId == productInfo.ProductId);
                    Assert.Contains(buyerAvatarState.mailBox.OfType<ProductBuyerMail>(), m => m.ProductId == productInfo.ProductId);
                }
            }

            Assert.True(totalTax > 0 * _currency);
            Assert.Equal(0 * _currency, tradedState.GetBalance(_buyerAgentAddress, _currency));
            Assert.Equal(totalTax, tradedState.GetBalance(feeStoreAddress, _currency));
        }
    }
}
