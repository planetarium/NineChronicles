namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

    public class Buy4Test
    {
        private const long ProductPrice = 100;

        private readonly Address _sellerAgentAddress;
        private readonly Address _sellerAvatarAddress;
        private readonly Address _buyerAgentAddress;
        private readonly Address _buyerAvatarAddress;
        private readonly AvatarState _buyerAvatarState;
        private readonly TableSheets _tableSheets;
        private readonly GoldCurrencyState _goldCurrencyState;
        private IAccountStateDelta _initialState;

        public Buy4Test(ITestOutputHelper outputHelper)
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
                new FungibleAssetValue(_goldCurrencyState.Currency, ProductPrice, 0),
                equipment));

            shopState.Register(new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(_goldCurrencyState.Currency, ProductPrice, 0),
                consumable));

            shopState.Register(new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(_goldCurrencyState.Currency, ProductPrice, 0),
                costume));

            var result = new CombinationConsumable5.ResultModel()
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
                _buyerAvatarState.Update2(mail);
                sellerAvatarState.Update2(mail);
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
            var goldCurrencyState = previousStates.GetGoldCurrency();
            var shopState = previousStates.GetShopState();
            Assert.Equal(3, shopState.Products.Count);
            Assert.NotNull(shopState.Products);

            var buyerGold = previousStates.GetBalance(_buyerAgentAddress, goldCurrencyState);
            var loopCount = 0;
            foreach (var (productId, shopItem) in shopState.Products)
            {
                loopCount++;
                var tax = shopItem.Price.DivRem(100, out _) * Buy.TaxRate;
                var taxedPrice = shopItem.Price - tax;

                var buyAction = new Buy4
                {
                    buyerAvatarAddress = _buyerAvatarAddress,
                    productId = productId,
                    sellerAgentAddress = _sellerAgentAddress,
                    sellerAvatarAddress = _sellerAvatarAddress,
                };
                var nextState = buyAction.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = previousStates,
                    Random = new TestRandom(),
                    Rehearsal = false,
                    Signer = _buyerAgentAddress,
                });

                var nextBuyerAvatarState = nextState.GetAvatarState(_buyerAvatarAddress);
                if (shopItem.ItemUsable != null)
                {
                    Assert.True(nextBuyerAvatarState.inventory.TryGetNonFungibleItem<ItemUsable>(
                        shopItem.ItemUsable.ItemId, out _));
                }

                Assert.Equal(30, nextBuyerAvatarState.mailBox.Count);

                var nextSellerAvatarState = nextState.GetAvatarState(_sellerAvatarAddress);
                Assert.Equal(30, nextSellerAvatarState.mailBox.Count);

                if (shopItem.Costume != null)
                {
                    Assert.True(nextBuyerAvatarState.inventory.TryGetNonFungibleItem<Costume>(
                        shopItem.Costume.ItemId, out _));
                }

                var nextGoldCurrencyGold = nextState.GetBalance(Addresses.GoldCurrency, goldCurrencyState);
                Assert.Equal(tax * loopCount, nextGoldCurrencyGold);
                var nextSellerGold = nextState.GetBalance(_sellerAgentAddress, goldCurrencyState);
                Assert.Equal(taxedPrice * loopCount, nextSellerGold);
                var nextBuyerGold = nextState.GetBalance(_buyerAgentAddress, goldCurrencyState);
                Assert.Equal(buyerGold - shopItem.Price * loopCount, nextBuyerGold);

                previousStates = nextState;
            }
        }

        [Fact]
        public void ExecuteThrowInvalidAddressException()
        {
            var action = new Buy4
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
            var action = new Buy4
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

            var action = new Buy4
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
            var action = new Buy4
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productId = default,
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

            var (productId, shopItem) = shopState.Products.FirstOrDefault();
            Assert.NotNull(shopItem);

            var balance = _initialState.GetBalance(_buyerAgentAddress, _goldCurrencyState.Currency);
            _initialState = _initialState.BurnAsset(_buyerAgentAddress, balance);

            var action = new Buy4
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productId = productId,
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
    }
}
