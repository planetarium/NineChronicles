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
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class Buy5Test
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

        public Buy5Test(ITestOutputHelper outputHelper)
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
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell6.ExpiredBlockIndex,
                    Price = 10,
                    ContainsInInventory = true,
                },
                new ShopItemData()
                {
                    ItemType = ItemType.Costume,
                    ItemId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = 0,
                    Price = 20,
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
                    RequiredBlockIndex = 0,
                    Price = 10,
                    ContainsInInventory = false,
                },
                new ShopItemData()
                {
                    ItemType = ItemType.Equipment,
                    ItemId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell6.ExpiredBlockIndex,
                    Price = 50,
                    ContainsInInventory = true,
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
                INonFungibleItem nonFungibleItem;
                Guid productId = shopItemData.ItemId;
                long requiredBlockIndex = shopItemData.RequiredBlockIndex;
                ItemSubType itemSubType;
                if (shopItemData.ItemType == ItemType.Equipment)
                {
                    var itemUsable = ItemFactory.CreateItemUsable(
                        _tableSheets.EquipmentItemSheet.First,
                        productId,
                        requiredBlockIndex);
                    nonFungibleItem = itemUsable;
                    itemSubType = itemUsable.ItemSubType;
                }
                else
                {
                    var costume = ItemFactory.CreateCostume(_tableSheets.CostumeItemSheet.First, productId);
                    costume.Update(requiredBlockIndex);
                    nonFungibleItem = costume;
                    itemSubType = costume.ItemSubType;
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
                    nonFungibleItem);

                // Case for backward compatibility.
                if (shopItemData.ContainsInInventory)
                {
                    shopState.Register(shopItem);
                    shardedShopStates[shardedShopAddress] = shopState;
                    sellerAvatarState.inventory.AddItem2((ItemBase)nonFungibleItem);
                    _initialState = _initialState.SetState(shardedShopAddress, shopState.Serialize());
                }
                else
                {
                    legacyShopState.Register(shopItem);
                }

                Assert.Equal(requiredBlockIndex, nonFungibleItem.RequiredBlockIndex);
                Assert.Equal(
                    shopItemData.ContainsInInventory,
                    sellerAvatarState.inventory.TryGetNonFungibleItem(productId, out _)
                );

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

            Assert.Single(legacyShopState.Products);
            Assert.True(shardedShopStates.All(r => r.Value.Products.Count == 1));

            var buyAction = new Buy5
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
                Guid itemId = purchaseInfo.productId;
                Buy7.PurchaseResult pr = buyAction.buyerMultipleResult.purchaseResults.First(r => r.productId == itemId);
                ShopItem shopItem = pr.shopItem;
                FungibleAssetValue tax = shopItem.Price.DivRem(100, out _) * Buy.TaxRate;
                FungibleAssetValue taxedPrice = shopItem.Price - tax;
                totalTax += tax;
                totalPrice += shopItem.Price;

                Assert.True(
                    nextBuyerAvatarState.inventory.TryGetNonFungibleItem(
                        itemId,
                        out INonFungibleItem outNonFungibleItem)
                );
                Assert.Equal(1, outNonFungibleItem.RequiredBlockIndex);

                var nextSellerAvatarState = nextState.GetAvatarState(purchaseInfo.sellerAvatarAddress);
                Assert.False(
                    nextSellerAvatarState.inventory.TryGetNonFungibleItem(
                        itemId,
                        out INonFungibleItem _)
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

            var action = new Buy5
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

            var action = new Buy5
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

            var action = new Buy5
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
                _sellerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Weapon
            );

            var action = new Buy5
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

            var action = new Buy5
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

        [Fact]
        public void Execute_ErrorCode_ItemDoesNotExist_By_SellerAvatar()
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
            _initialState = _initialState.SetState(shardedShopAddress, shopState.Serialize());

            Assert.True(shopItem.ExpiredBlockIndex > 0);
            Assert.True(shopItem.ItemUsable.RequiredBlockIndex > 0);

            PurchaseInfo0 purchaseInfo0 = new PurchaseInfo0(
                _productId,
                _sellerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Weapon,
                shopItem.Price
            );

            var action = new Buy5
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

            var action = new Buy5
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

            public Address SellerAgentAddress { get; set; }

            public Address SellerAvatarAddress { get; set; }

            public BigInteger Price { get; set; }

            public long RequiredBlockIndex { get; set; }

            public bool ContainsInInventory { get; set; }
        }
    }
}
