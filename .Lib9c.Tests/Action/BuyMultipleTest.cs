namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections;
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

            var equipment = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                0);

            var consumable = ItemFactory.CreateItemUsable(
                _tableSheets.ConsumableItemSheet.First,
                Guid.NewGuid(),
                0);

            var costume = ItemFactory.CreateCostume(
                _tableSheets.CostumeItemSheet.First,
                Guid.NewGuid());

            var shopState = new ShopState();
            shopState.Register(new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(_goldCurrencyState.Currency, 10, 0),
                equipment));

            shopState.Register(new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(_goldCurrencyState.Currency, 20, 0),
                consumable));

            shopState.Register(new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(_goldCurrencyState.Currency, 30, 0),
                costume));

            var result = new CombinationConsumable.ResultModel()
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
            };

            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                _buyerAvatarState.Update(mail);
                sellerAvatarState.Update(mail);
            }

            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .SetState(Addresses.Shop, shopState.Serialize())
                .SetState(_sellerAgentAddress, sellerAgentState.Serialize())
                .SetState(_sellerAvatarAddress, sellerAvatarState.Serialize())
                .SetState(_buyerAgentAddress, buyerAgentState.Serialize())
                .SetState(_buyerAvatarAddress, _buyerAvatarState.Serialize())
                .MintAsset(_buyerAgentAddress, shopState.Products
                    .Select(pair => pair.Value.Price)
                    .Aggregate((totalPrice, next) => totalPrice + next));
        }

        [Fact]
        public void Execute()
        {
            var previousStates = _initialState;
            var goldCurrency = previousStates.GetGoldCurrency();
            var shopState = previousStates.GetShopState();
            Assert.Equal(3, shopState.Products.Count);
            Assert.NotNull(shopState.Products);

            var buyerGold = previousStates.GetBalance(_buyerAgentAddress, goldCurrency);
            var priceData = new PriceData(goldCurrency);
            var priceSumData = shopState.Products.Values.Aggregate(priceData, (priceSum, next) =>
            {
                var price = next.Price;
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
                productIds = shopState.Products.Keys,
                sellerAgentAddress = _sellerAgentAddress,
                sellerAvatarAddress = _sellerAvatarAddress,
            };
            var nextState = buyMultipleAction.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = previousStates,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _buyerAgentAddress,
            });

            var nextBuyerAvatarState = nextState.GetAvatarState(_buyerAvatarAddress);
            foreach (var (productId, shopItem) in shopState.Products)
            {
                if (shopItem.ItemUsable != null)
                {
                    Assert.True(nextBuyerAvatarState.inventory.TryGetNonFungibleItem<ItemUsable>(
                        shopItem.ItemUsable.ItemId, out _));
                }
                else if (shopItem.Costume != null)
                {
                    Assert.True(nextBuyerAvatarState.inventory.TryGetNonFungibleItem<Costume>(
                        shopItem.Costume.ItemId, out _));
                }
            }

            var nextSellerAvatarState = nextState.GetAvatarState(_sellerAvatarAddress);
            Assert.Equal(30, nextBuyerAvatarState.mailBox.Count);
            Assert.Equal(30, nextSellerAvatarState.mailBox.Count);

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
            Assert.NotEmpty(shopState.Products);

            var productIds = shopState.Products.Keys.Take(3).ToList();
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
        public void ExecuteThrowInsufficientBalanceException()
        {
            var shopState = _initialState.GetShopState();
            Assert.NotEmpty(shopState.Products);

            var productIds = shopState.Products.Keys.Take(3);
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
    }
}
