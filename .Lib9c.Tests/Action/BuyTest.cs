namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
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

    public class BuyTest
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

        public BuyTest(ITestOutputHelper outputHelper)
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

        [Theory]
        [InlineData(ItemType.Equipment, "F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4", true)]
        [InlineData(ItemType.Costume, "936DA01F-9ABD-4d9d-80C7-02AF85C822A8", true)]
        [InlineData(ItemType.Equipment, "F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4", false)]
        [InlineData(ItemType.Costume, "936DA01F-9ABD-4d9d-80C7-02AF85C822A8", false)]
        public void Execute(ItemType itemType, string guid, bool contain)
        {
            var sellerAvatarState = _initialState.GetAvatarState(_sellerAvatarAddress);
            var buyerAvatarState = _initialState.GetAvatarState(_buyerAvatarAddress);
            INonFungibleItem nonFungibleItem;
            Guid itemId = new Guid(guid);
            Guid productId = itemId;
            ItemSubType itemSubType;
            ShopState legacyShopState = _initialState.GetShopState();
            if (itemType == ItemType.Equipment)
            {
                var itemUsable = ItemFactory.CreateItemUsable(
                    _tableSheets.EquipmentItemSheet.First,
                    itemId,
                    Sell.ExpiredBlockIndex);
                nonFungibleItem = itemUsable;
                itemSubType = itemUsable.ItemSubType;
            }
            else
            {
                var costume = ItemFactory.CreateCostume(_tableSheets.CostumeItemSheet.First, itemId);
                costume.Update(Sell.ExpiredBlockIndex);
                nonFungibleItem = costume;
                itemSubType = costume.ItemSubType;
            }

            var result = new DailyReward.DailyRewardResult()
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

            Address shardedShopAddress = ShardedShopState.DeriveAddress(itemSubType, productId);
            var shopItem = new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                Sell.ExpiredBlockIndex,
                nonFungibleItem);
            Assert.Equal(Sell.ExpiredBlockIndex, nonFungibleItem.RequiredBlockIndex);

            // Case for backward compatibility.
            if (contain)
            {
                ShardedShopState shopState = new ShardedShopState(shardedShopAddress);
                shopState.Register(shopItem);
                sellerAvatarState.inventory.AddItem((ItemBase)nonFungibleItem);
                Assert.Empty(legacyShopState.Products);
                Assert.Single(shopState.Products);
                _initialState = _initialState.SetState(shardedShopAddress, shopState.Serialize());
            }
            else
            {
                legacyShopState.Register(shopItem);
                Assert.Single(legacyShopState.Products);
                Assert.Null(_initialState.GetState(shardedShopAddress));
            }

            Assert.Equal(contain, sellerAvatarState.inventory.TryGetNonFungibleItem(itemId, out _));

            IAccountStateDelta prevState = _initialState
                .SetState(_sellerAvatarAddress, sellerAvatarState.Serialize())
                .SetState(_buyerAvatarAddress, buyerAvatarState.Serialize())
                .SetState(Addresses.Shop, legacyShopState.Serialize());

            var tax = shopItem.Price.DivRem(100, out _) * Buy.TaxRate;
            var taxedPrice = shopItem.Price - tax;

            var buyAction = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productId = shopItem.ProductId,
                sellerAgentAddress = _sellerAgentAddress,
                sellerAvatarAddress = _sellerAvatarAddress,
                itemSubType = itemSubType,
            };
            var nextState = buyAction.Execute(new ActionContext()
            {
                BlockIndex = 1,
                PreviousStates = prevState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _buyerAgentAddress,
            });

            var nextShopState = new ShardedShopState((Dictionary)nextState.GetState(shardedShopAddress));
            Assert.Empty(nextShopState.Products);

            var nextBuyerAvatarState = nextState.GetAvatarState(_buyerAvatarAddress);
            Assert.True(
                nextBuyerAvatarState.inventory.TryGetNonFungibleItem(
                    nonFungibleItem.ItemId,
                    out INonFungibleItem outNonFungibleItem)
            );
            Assert.Equal(1, outNonFungibleItem.RequiredBlockIndex);
            Assert.Equal(30, nextBuyerAvatarState.mailBox.Count);

            var nextSellerAvatarState = nextState.GetAvatarState(_sellerAvatarAddress);
            Assert.False(
                nextSellerAvatarState.inventory.TryGetNonFungibleItem(
                    nonFungibleItem.ItemId,
                    out INonFungibleItem _)
            );
            Assert.Equal(30, nextSellerAvatarState.mailBox.Count);

            var goldCurrencyState = nextState.GetGoldCurrency();
            var goldCurrencyGold = nextState.GetBalance(Addresses.GoldCurrency, goldCurrencyState);
            Assert.Equal(tax, goldCurrencyGold);
            var sellerGold = nextState.GetBalance(_sellerAgentAddress, goldCurrencyState);
            Assert.Equal(taxedPrice, sellerGold);
            var buyerGold = nextState.GetBalance(_buyerAgentAddress, goldCurrencyState);
            Assert.Equal(new FungibleAssetValue(goldCurrencyState, 0, 0), buyerGold);

            ShopState nextLegacyShopState = nextState.GetShopState();
            Assert.Empty(nextLegacyShopState.Products);
        }

        [Fact]
        public void ExecuteThrowInvalidAddressException()
        {
            var action = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productId = default,
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
            var action = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productId = default,
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

            var action = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productId = default,
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
            var action = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productId = default,
                sellerAgentAddress = _sellerAgentAddress,
                sellerAvatarAddress = _sellerAvatarAddress,
                itemSubType = ItemSubType.Weapon,
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
            Address shardedShopAddress = ShardedShopState.DeriveAddress(ItemSubType.Weapon, _productId);
            var itemUsable = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                Sell.ExpiredBlockIndex);

            var shopItem = new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                _productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                Sell.ExpiredBlockIndex,
                itemUsable);

            ShardedShopState shopState = new ShardedShopState(shardedShopAddress);
            shopState.Register(shopItem);

            var balance = _initialState.GetBalance(_buyerAgentAddress, _goldCurrencyState.Currency);
            _initialState = _initialState.BurnAsset(_buyerAgentAddress, balance)
                .SetState(shardedShopAddress, shopState.Serialize());

            var action = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productId = _productId,
                sellerAgentAddress = _sellerAgentAddress,
                sellerAvatarAddress = _sellerAvatarAddress,
                itemSubType = ItemSubType.Weapon,
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
        public void ExecuteThrowItemDoesNotExistExceptionBySellerAvatar()
        {
            Address shardedShopAddress = ShardedShopState.DeriveAddress(ItemSubType.Weapon, _productId);
            var itemUsable = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                Sell.ExpiredBlockIndex);

            var shopItem = new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                _productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                Sell.ExpiredBlockIndex,
                itemUsable);

            ShardedShopState shopState = new ShardedShopState(shardedShopAddress);
            shopState.Register(shopItem);
            _initialState = _initialState.SetState(shardedShopAddress, shopState.Serialize());

            Assert.True(shopItem.ExpiredBlockIndex > 0);
            Assert.True(shopItem.ItemUsable.RequiredBlockIndex > 0);

            var action = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productId = _productId,
                sellerAgentAddress = _sellerAgentAddress,
                sellerAvatarAddress = _sellerAvatarAddress,
                itemSubType = ItemSubType.Weapon,
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
        public void ExecuteThrowShopItemExpiredException()
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

            var action = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productId = _productId,
                sellerAgentAddress = _sellerAgentAddress,
                sellerAvatarAddress = _sellerAvatarAddress,
                itemSubType = ItemSubType.Weapon,
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
    }
}
