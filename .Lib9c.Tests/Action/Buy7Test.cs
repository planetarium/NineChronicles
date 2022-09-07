namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Bencodex.Types;
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

    public class Buy7Test
    {
        private readonly Address _sellerAgentAddress;
        private readonly Address _sellerAvatarAddress;
        private readonly Address _buyerAgentAddress;
        private readonly Address _buyerAvatarAddress;
        private readonly AvatarState _buyerAvatarState;
        private readonly TableSheets _tableSheets;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly Guid _productId;
        private IAccountStateDelta _initialState;

        public Buy7Test(ITestOutputHelper outputHelper)
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

            _productId = new Guid("6d460c1a-755d-48e4-ad67-65d5f519dbc8");
            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .SetState(_sellerAgentAddress, sellerAgentState.Serialize())
                .SetState(_sellerAvatarAddress, sellerAvatarState.Serialize())
                .SetState(_buyerAgentAddress, buyerAgentState.Serialize())
                .SetState(_buyerAvatarAddress, _buyerAvatarState.Serialize())
                .SetState(Addresses.Shop, new ShopState().Serialize())
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
                    ProductId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell6.ExpiredBlockIndex,
                    Price = 10,
                    ContainsInInventory = true,
                    ItemCount = 1,
                },
                new ShopItemData()
                {
                    ItemType = ItemType.Costume,
                    ItemId = Guid.NewGuid(),
                    ProductId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = 0,
                    Price = 20,
                    ContainsInInventory = false,
                    ItemCount = 1,
                },
            };
            yield return new object[]
            {
                new ShopItemData()
                {
                    ItemType = ItemType.Costume,
                    ItemId = Guid.NewGuid(),
                    ProductId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = 0,
                    Price = 10,
                    ContainsInInventory = false,
                    ItemCount = 1,
                },
                new ShopItemData()
                {
                    ItemType = ItemType.Equipment,
                    ItemId = Guid.NewGuid(),
                    ProductId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell6.ExpiredBlockIndex,
                    Price = 50,
                    ContainsInInventory = true,
                    ItemCount = 1,
                },
            };
            yield return new object[]
            {
                new ShopItemData()
                {
                    ItemType = ItemType.Material,
                    ItemId = new Guid("15396359-04db-68d5-f24a-d89c18665900"),
                    ProductId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell6.ExpiredBlockIndex,
                    Price = 50,
                    ContainsInInventory = true,
                    ItemCount = 1,
                },
                new ShopItemData()
                {
                    ItemType = ItemType.Material,
                    ItemId = new Guid("15396359-04db-68d5-f24a-d89c18665900"),
                    ProductId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = 0,
                    Price = 10,
                    ContainsInInventory = true,
                    ItemCount = 2,
                },
            };
        }

        [Theory]
        [MemberData(nameof(GetExecuteMemberData))]
        public void Execute(params ShopItemData[] shopItemMembers)
        {
            AvatarState buyerAvatarState = _initialState.GetAvatarState(_buyerAvatarAddress);
            List<PurchaseInfo0> purchaseInfos = new List<PurchaseInfo0>();
            Dictionary<Address, ShardedShopState> shardedShopStates = new Dictionary<Address, ShardedShopState>();
            ShopState legacyShopState = _initialState.GetShopState();
            foreach (var shopItemData in shopItemMembers)
            {
                (AvatarState sellerAvatarState, AgentState sellerAgentState) = CreateAvatarState(
                    shopItemData.SellerAgentAddress,
                    shopItemData.SellerAvatarAddress
                );
                ITradableItem tradableItem;
                Guid productId = shopItemData.ProductId;
                Guid itemId = shopItemData.ItemId;
                long requiredBlockIndex = shopItemData.RequiredBlockIndex;
                ItemSubType itemSubType;
                if (shopItemData.ItemType == ItemType.Equipment)
                {
                    var itemUsable = ItemFactory.CreateItemUsable(
                        _tableSheets.EquipmentItemSheet.First,
                        itemId,
                        requiredBlockIndex);
                    tradableItem = itemUsable;
                    itemSubType = itemUsable.ItemSubType;
                }
                else if (shopItemData.ItemType == ItemType.Costume)
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
                    material.RequiredBlockIndex = requiredBlockIndex;
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
                    sellerAvatarState.Update2(mail);
                    buyerAvatarState.Update2(mail);
                }

                Address shardedShopAddress = ShardedShopState.DeriveAddress(itemSubType, productId);
                ShardedShopState shopState = shardedShopStates.ContainsKey(shardedShopAddress)
                    ? shardedShopStates[shardedShopAddress]
                    : new ShardedShopState(shardedShopAddress);
                var shopItem = new ShopItem(
                    sellerAgentState.address,
                    sellerAvatarState.address,
                    productId,
                    new FungibleAssetValue(_goldCurrencyState.Currency, shopItemData.Price, 0),
                    requiredBlockIndex,
                    tradableItem,
                    shopItemData.ItemCount
                );

                // Case for backward compatibility.
                if (shopItemData.ContainsInInventory)
                {
                    shopState.Register(shopItem);
                    shardedShopStates[shardedShopAddress] = shopState;
                    sellerAvatarState.inventory.AddItem2((ItemBase)tradableItem, shopItemData.ItemCount);
                    _initialState = _initialState.SetState(shardedShopAddress, shopState.Serialize());
                }
                else
                {
                    legacyShopState.Register(shopItem);
                }

                Assert.Equal(requiredBlockIndex, tradableItem.RequiredBlockIndex);
                Assert.Equal(
                    shopItemData.ContainsInInventory,
                    sellerAvatarState.inventory.TryGetTradableItems(
                        shopItemData.ItemId,
                        shopItemData.RequiredBlockIndex,
                        shopItemData.ItemCount,
                        out _
                    )
                );
                Assert.DoesNotContain(((ItemBase)tradableItem).Id, buyerAvatarState.itemMap.Keys);

                var purchaseInfo = new PurchaseInfo0(
                    shopItem.ProductId,
                    shopItem.SellerAgentAddress,
                    shopItem.SellerAvatarAddress,
                    itemSubType,
                    shopItem.Price
                );
                purchaseInfos.Add(purchaseInfo);

                _initialState = _initialState
                    .SetState(_buyerAvatarAddress, buyerAvatarState.Serialize())
                    .SetState(sellerAvatarState.address, sellerAvatarState.Serialize())
                    .SetState(shardedShopAddress, shopState.Serialize())
                    .SetState(Addresses.Shop, legacyShopState.Serialize());
            }

            if (shopItemMembers.Any(i => i.ItemType == ItemType.Material))
            {
                Assert.Empty(legacyShopState.Products);
                Assert.Equal(2, shardedShopStates.Sum(r => r.Value.Products.Count));
            }
            else
            {
                Assert.Single(legacyShopState.Products);
                Assert.True(shardedShopStates.All(r => r.Value.Products.Count == 1));
            }

            var buyAction = new Buy7
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = purchaseInfos,
            };
            var nextState = buyAction.Execute(new ActionContext()
            {
                BlockIndex = 1,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _buyerAgentAddress,
            });

            FungibleAssetValue totalTax = 0 * _goldCurrencyState.Currency;
            FungibleAssetValue totalPrice = 0 * _goldCurrencyState.Currency;
            Currency goldCurrencyState = nextState.GetGoldCurrency();
            AvatarState nextBuyerAvatarState = nextState.GetAvatarState(_buyerAvatarAddress);

            Assert.True(buyAction.buyerMultipleResult.purchaseResults.All(r => r.errorCode == 0));

            foreach (var purchaseInfo in purchaseInfos)
            {
                Address shardedShopAddress =
                    ShardedShopState.DeriveAddress(purchaseInfo.itemSubType, purchaseInfo.productId);
                var nextShopState = new ShardedShopState((Dictionary)nextState.GetState(shardedShopAddress));
                Assert.Empty(nextShopState.Products);
                Guid itemId = shopItemMembers
                    .Where(i => i.ProductId == purchaseInfo.productId)
                    .Select(i => i.ItemId).First();
                Buy7.PurchaseResult pr =
                    buyAction.buyerMultipleResult.purchaseResults.First(r => r.productId == purchaseInfo.productId);
                ShopItem shopItem = pr.shopItem;
                FungibleAssetValue tax = shopItem.Price.DivRem(100, out _) * Buy.TaxRate;
                FungibleAssetValue taxedPrice = shopItem.Price - tax;
                totalTax += tax;
                totalPrice += shopItem.Price;

                int itemCount = shopItem.TradableFungibleItemCount == 0 ? 1 : shopItem.TradableFungibleItemCount;
                Assert.Equal(shopItem.TradableFungibleItemCount == 0, pr.tradableFungibleItem is null);
                Assert.Equal(shopItem.TradableFungibleItemCount, pr.tradableFungibleItemCount);
                Assert.True(
                    nextBuyerAvatarState.inventory.TryGetTradableItems(
                        itemId,
                        1,
                        itemCount,
                        out List<Inventory.Item> inventoryItems)
                );
                Assert.Single(inventoryItems);
                Inventory.Item inventoryItem = inventoryItems.First();
                ITradableItem tradableItem = (ITradableItem)inventoryItem.item;
                Assert.Equal(1, tradableItem.RequiredBlockIndex);
                int expectedCount = tradableItem is TradableMaterial
                    ? shopItemMembers.Sum(i => i.ItemCount)
                    : itemCount;
                Assert.Equal(expectedCount, inventoryItem.count);
                Assert.Equal(expectedCount, nextBuyerAvatarState.itemMap[((ItemBase)tradableItem).Id]);

                var nextSellerAvatarState = nextState.GetAvatarState(purchaseInfo.sellerAvatarAddress);
                Assert.False(
                    nextSellerAvatarState.inventory.TryGetTradableItems(
                        itemId,
                        1,
                        itemCount,
                        out _)
                );
                Assert.Equal(30, nextSellerAvatarState.mailBox.Count);

                FungibleAssetValue sellerGold =
                    nextState.GetBalance(purchaseInfo.sellerAgentAddress, goldCurrencyState);
                Assert.Equal(taxedPrice, sellerGold);
            }

            Assert.Equal(30, nextBuyerAvatarState.mailBox.Count);

            var goldCurrencyGold = nextState.GetBalance(Addresses.GoldCurrency, goldCurrencyState);
            Assert.Equal(totalTax, goldCurrencyGold);
            var buyerGold = nextState.GetBalance(_buyerAgentAddress, goldCurrencyState);
            var prevBuyerGold = _initialState.GetBalance(_buyerAgentAddress, goldCurrencyState);
            Assert.Equal(prevBuyerGold - totalPrice, buyerGold);
            ShopState nextLegacyShopState = nextState.GetShopState();
            Assert.Empty(nextLegacyShopState.Products);
        }

        [Fact]
        public void Execute_ErrorCode_InvalidAddress()
        {
            PurchaseInfo0 purchaseInfo0 = new PurchaseInfo0(
                default,
                _buyerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Food
            );

            var action = new Buy7
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo0 },
            };

            action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Signer = _buyerAgentAddress,
            });

            Assert.Contains(
                Buy.ErrorCodeInvalidAddress,
                action.buyerMultipleResult.purchaseResults.Select(r => r.errorCode)
            );
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException()
        {
            PurchaseInfo0 purchaseInfo0 = new PurchaseInfo0(
                default,
                _buyerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Food
            );

            var action = new Buy7
            {
                buyerAvatarAddress = default,
                purchaseInfos = new[] { purchaseInfo0 },
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
        public void Execute_Throw_NotEnoughClearedStageLevel()
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

            PurchaseInfo0 purchaseInfo0 = new PurchaseInfo0(
                default,
                _buyerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Food
            );

            var action = new Buy7
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo0 },
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
        public void Execute_ErrorCode_ItemDoesNotExist()
        {
            PurchaseInfo0 purchaseInfo0 = new PurchaseInfo0(
                default,
                default,
                _sellerAvatarAddress,
                ItemSubType.Weapon
            );

            var action = new Buy7
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo0 },
            };

            action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Signer = _buyerAgentAddress,
            });

            Assert.Contains(
                Buy.ErrorCodeItemDoesNotExist,
                action.buyerMultipleResult.purchaseResults.Select(r => r.errorCode)
            );
        }

        [Theory]
        [InlineData(ItemSubType.Hourglass)]
        [InlineData(ItemSubType.ApStone)]
        public void Execute_ErrorCode_ItemDoesNotExist_Material(ItemSubType itemSubType)
        {
            TradableMaterial material =
                ItemFactory.CreateTradableMaterial(
                    _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == itemSubType));
            PurchaseInfo0 purchaseInfo0 = new PurchaseInfo0(
                default,
                default,
                _sellerAvatarAddress,
                itemSubType
            );

            var shopItem = new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                _productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                Sell6.ExpiredBlockIndex,
                material);

            Address shardedShopAddress = ShardedShopState.DeriveAddress(itemSubType, _productId);
            ShardedShopState shopState = new ShardedShopState(shardedShopAddress);
            shopState.Register(shopItem);

            _initialState = _initialState.SetState(shardedShopAddress, shopState.Serialize());

            var action = new Buy7
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo0 },
            };

            action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Signer = _buyerAgentAddress,
            });

            Assert.Contains(
                Buy.ErrorCodeItemDoesNotExist,
                action.buyerMultipleResult.purchaseResults.Select(r => r.errorCode)
            );
        }

        [Theory]
        [InlineData(ItemSubType.Weapon, false, false)]
        [InlineData(ItemSubType.Hourglass, false, false)]
        [InlineData(ItemSubType.ApStone, false, false)]
        [InlineData(ItemSubType.Weapon, true, false)]
        [InlineData(ItemSubType.Hourglass, true, false)]
        [InlineData(ItemSubType.ApStone, true, false)]
        [InlineData(ItemSubType.Weapon, false, true)]
        [InlineData(ItemSubType.Hourglass, false, true)]
        [InlineData(ItemSubType.ApStone, false, true)]
        public void Execute_ErrorCode_ItemDoesNotExist_20210604(ItemSubType itemSubType, bool useAgentAddress, bool useAvatarAddress)
        {
            ITradableItem tradableItem = null;
            switch (itemSubType)
            {
                case ItemSubType.Hourglass:
                case ItemSubType.ApStone:
                    tradableItem = ItemFactory.CreateTradableMaterial(
                        _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == itemSubType));
                    break;
                case ItemSubType.Weapon:
                    tradableItem = (ITradableItem)ItemFactory.CreateItem(
                        _tableSheets.EquipmentItemSheet.OrderedList.First(r => r.ItemSubType == itemSubType),
                        new TestRandom());
                    break;
            }

            Address agentAddress = useAgentAddress ? _sellerAgentAddress : default;
            Address avatarAddress = useAvatarAddress ? _sellerAvatarAddress : default;
            PurchaseInfo0 purchaseInfo0 = new PurchaseInfo0(
                default,
                agentAddress,
                avatarAddress,
                itemSubType
            );

            var shopItem = new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                _productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                Sell6.ExpiredBlockIndex,
                tradableItem);

            Address shardedShopAddress = ShardedShopState.DeriveAddress(itemSubType, _productId);
            ShardedShopState shopState = new ShardedShopState(shardedShopAddress);
            shopState.Register(shopItem);

            _initialState = _initialState.SetState(shardedShopAddress, shopState.Serialize());

            var action = new Buy7
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo0 },
            };

            action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Signer = _buyerAgentAddress,
            });

            Assert.Contains(
                Buy.ErrorCodeItemDoesNotExist,
                action.buyerMultipleResult.purchaseResults.Select(r => r.errorCode)
            );
        }

        [Fact]
        public void Execute_ErrorCode_InsufficientBalance()
        {
            Address shardedShopAddress = ShardedShopState.DeriveAddress(ItemSubType.Weapon, _productId);
            var itemUsable = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                Sell6.ExpiredBlockIndex);

            var shopItem = new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                _productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                Sell6.ExpiredBlockIndex,
                itemUsable);

            ShardedShopState shopState = new ShardedShopState(shardedShopAddress);
            shopState.Register(shopItem);

            var balance = _initialState.GetBalance(_buyerAgentAddress, _goldCurrencyState.Currency);
            _initialState = _initialState.BurnAsset(_buyerAgentAddress, balance)
                .SetState(shardedShopAddress, shopState.Serialize());

            PurchaseInfo0 purchaseInfo0 = new PurchaseInfo0(
                _productId,
                _sellerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Weapon,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0)
            );

            var action = new Buy7
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo0 },
            };

            action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Signer = _buyerAgentAddress,
            });

            Assert.Contains(
                Buy.ErrorCodeInsufficientBalance,
                action.buyerMultipleResult.purchaseResults.Select(r => r.errorCode)
            );
        }

        [Theory]
        [InlineData(ItemType.Equipment)]
        [InlineData(ItemType.Consumable)]
        [InlineData(ItemType.Costume)]
        [InlineData(ItemType.Material)]
        public void Execute_ErrorCode_ItemDoesNotExist_By_SellerAvatar(ItemType itemType)
        {
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

            Address shardedShopAddress = ShardedShopState.DeriveAddress(tradableItem.ItemSubType, _productId);

            var shopItem = new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                _productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                Sell6.ExpiredBlockIndex,
                tradableItem);

            ShardedShopState shopState = new ShardedShopState(shardedShopAddress);
            shopState.Register(shopItem);
            _initialState = _initialState.SetState(shardedShopAddress, shopState.Serialize());

            Assert.True(shopItem.ExpiredBlockIndex > 0);

            PurchaseInfo0 purchaseInfo0 = new PurchaseInfo0(
                _productId,
                _sellerAgentAddress,
                _sellerAvatarAddress,
                tradableItem.ItemSubType,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0)
            );

            var action = new Buy7
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo0 },
            };

            var nextState = action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Signer = _buyerAgentAddress,
            });

            Assert.Contains(
                Buy.ErrorCodeItemDoesNotExist,
                action.buyerMultipleResult.purchaseResults.Select(r => r.errorCode)
            );
            foreach (var address in new[] { _buyerAgentAddress, _sellerAgentAddress, Addresses.GoldCurrency })
            {
                Assert.Equal(
                    _initialState.GetBalance(address, _goldCurrencyState.Currency),
                    nextState.GetBalance(address, _goldCurrencyState.Currency)
                );
            }
        }

        [Fact]
        public void Execute_ErrorCode_ShopItemExpired()
        {
            IAccountStateDelta previousStates = _initialState;
            Address shardedShopStateAddress = ShardedShopState.DeriveAddress(ItemSubType.Weapon, _productId);
            ShardedShopState shopState = new ShardedShopState(shardedShopStateAddress);
            Weapon itemUsable = (Weapon)ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                10);
            var shopItem = new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                _productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                10,
                itemUsable);

            shopState.Register(shopItem);
            previousStates = previousStates.SetState(shardedShopStateAddress, shopState.Serialize());

            Assert.True(shopState.Products.ContainsKey(_productId));

            PurchaseInfo0 purchaseInfo0 = new PurchaseInfo0(
                _productId,
                _sellerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Weapon
            );

            var action = new Buy7
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo0 },
            };

            action.Execute(new ActionContext()
            {
                BlockIndex = 11,
                PreviousStates = previousStates,
                Random = new TestRandom(),
                Signer = _buyerAgentAddress,
            });

            Assert.Contains(
                Buy.ErrorCodeShopItemExpired,
                action.buyerMultipleResult.purchaseResults.Select(r => r.errorCode)
            );
        }

        [Theory]
        [InlineData(100, 10)]
        [InlineData(10, 20)]
        public void Execute_ErrorCode_InvalidPrice(int shopPrice, int price)
        {
            IAccountStateDelta previousStates = _initialState;
            Address shardedShopStateAddress = ShardedShopState.DeriveAddress(ItemSubType.Weapon, _productId);
            ShardedShopState shopState = new ShardedShopState(shardedShopStateAddress);
            Weapon itemUsable = (Weapon)ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                10);
            var shopItem = new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                _productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, shopPrice, 0),
                10,
                itemUsable);

            shopState.Register(shopItem);
            previousStates = previousStates.SetState(shardedShopStateAddress, shopState.Serialize());

            Assert.True(shopState.Products.ContainsKey(_productId));

            PurchaseInfo0 purchaseInfo0 = new PurchaseInfo0(
                _productId,
                _sellerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Weapon,
                new FungibleAssetValue(_goldCurrencyState.Currency, price, 0)
            );

            var action = new Buy7
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo0 },
            };

            action.Execute(new ActionContext()
            {
                BlockIndex = 10,
                PreviousStates = previousStates,
                Random = new TestRandom(),
                Signer = _buyerAgentAddress,
            });

            Assert.Single(action.buyerMultipleResult.purchaseResults);
            Buy7.PurchaseResult purchaseResult = action.buyerMultipleResult.purchaseResults.First();
            Assert.Equal(Buy.ErrorCodeInvalidPrice, purchaseResult.errorCode);
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

        public class ShopItemData
        {
            public ItemType ItemType { get; set; }

            public Guid ItemId { get; set; }

            public Guid ProductId { get; set; }

            public Address SellerAgentAddress { get; set; }

            public Address SellerAvatarAddress { get; set; }

            public BigInteger Price { get; set; }

            public long RequiredBlockIndex { get; set; }

            public bool ContainsInInventory { get; set; }

            public int ItemCount { get; set; }
        }
    }
}
