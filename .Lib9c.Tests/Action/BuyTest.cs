namespace Lib9c.Tests.Action
{
    using System;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class BuyTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Address _sellerAgentAddress;
        private readonly Address _sellerAvatarAddress;
        private readonly Address _buyerAgentAddress;
        private readonly Address _buyerAvatarAddress;

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

            var tableSheets = new TableSheets(sheets);

            var currency = new Currency("NCG", 2, minters: null);
            var goldCurrencyState = new GoldCurrencyState(currency);

            _sellerAgentAddress = new PrivateKey().ToAddress();
            var sellerAgentState = new AgentState(_sellerAgentAddress);
            _sellerAvatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
            var sellerAvatarState = new AvatarState(
                _sellerAvatarAddress,
                _sellerAgentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            sellerAgentState.avatarAddresses[0] = _sellerAvatarAddress;

            _buyerAgentAddress = new PrivateKey().ToAddress();
            var buyerAgentState = new AgentState(_buyerAgentAddress);
            _buyerAvatarAddress = new PrivateKey().ToAddress();
            var buyerAvatarState = new AvatarState(
                _buyerAvatarAddress,
                _buyerAgentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            buyerAgentState.avatarAddresses[0] = _buyerAvatarAddress;

            var equipment = ItemFactory.CreateItemUsable(
                tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                0);
            var shopState = new ShopState();
            shopState.Register(new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                Guid.NewGuid(),
                equipment,
                new FungibleAssetValue(goldCurrencyState.Currency, 100, 0)));

            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, goldCurrencyState.Serialize())
                .SetState(Addresses.Shop, shopState.Serialize())
                .SetState(_sellerAgentAddress, sellerAgentState.Serialize())
                .SetState(_sellerAvatarAddress, sellerAvatarState.Serialize())
                .SetState(_buyerAgentAddress, buyerAgentState.Serialize())
                .SetState(_buyerAvatarAddress, buyerAvatarState.Serialize())
                .MintAsset(_buyerAgentAddress, goldCurrencyState.Currency * 100);
        }

        [Fact]
        public void Execute()
        {
            var shopState = _initialState.GetShopState();
            Assert.NotEmpty(shopState.Products);

            var (productId, shopItem) = shopState.Products.FirstOrDefault();
            Assert.NotNull(shopItem);

            var tax = shopItem.Price.DivRem(100, out _) * Buy.TaxRate;
            var taxedPrice = shopItem.Price - tax;

            var buyAction = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                productId = productId,
                sellerAgentAddress = _sellerAgentAddress,
                sellerAvatarAddress = _sellerAvatarAddress,
            };
            var nextState = buyAction.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Random = new ItemEnhancementTest.TestRandom(),
                Rehearsal = false,
                Signer = _buyerAgentAddress,
            });

            var nextShopState = nextState.GetShopState();
            Assert.Empty(nextShopState.Products);

            var nextBuyerAvatarState = nextState.GetAvatarState(_buyerAvatarAddress);
            Assert.True(nextBuyerAvatarState.inventory.TryGetNonFungibleItem(shopItem.ItemUsable.ItemId, out ItemUsable _));

            var goldCurrencyState = nextState.GetGoldCurrency();
            var goldCurrencyGold = nextState.GetBalance(Addresses.GoldCurrency, goldCurrencyState);
            Assert.Equal(tax, goldCurrencyGold);
            var sellerGold = nextState.GetBalance(_sellerAgentAddress, goldCurrencyState);
            Assert.Equal(taxedPrice, sellerGold);
            var buyerGold = nextState.GetBalance(_buyerAgentAddress, goldCurrencyState);
            Assert.Equal(new FungibleAssetValue(goldCurrencyState, 0, 0), buyerGold);
        }
    }
}
