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

    public class SellTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;

        public SellTest(ITestOutputHelper outputHelper)
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

            var shopState = new ShopState();

            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);
            _avatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
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
            agentState.avatarAddresses[0] = _avatarAddress;

            var equipment = ItemFactory.CreateItemUsable(
                tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                0);
            avatarState.inventory.AddItem(equipment);

            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, goldCurrencyState.Serialize())
                .SetState(Addresses.Shop, shopState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize());
        }

        [Fact]
        public void Execute()
        {
            var shopState = _initialState.GetShopState();
            Assert.Empty(shopState.Products);

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            Assert.Single(avatarState.inventory.Equipments);

            var equipment = avatarState.inventory.Equipments.FirstOrDefault();
            Assert.NotNull(equipment);

            var currencyState = _initialState.GetGoldCurrency();
            var price = new FungibleAssetValue(currencyState, 100, 0);
            var productId = Guid.NewGuid();
            var sellAction = new Sell
            {
                itemId = equipment.ItemId,
                price = price,
                productId = productId,
                sellerAvatarAddress = _avatarAddress,
            };
            var nextState = sellAction.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Rehearsal = false,
                Signer = _agentAddress,
            });

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.Empty(nextAvatarState.inventory.Equipments);

            var nextShopState = nextState.GetShopState();
            Assert.Single(nextShopState.Products);

            var (_, shopItem) = nextShopState.Products.FirstOrDefault();
            Assert.NotNull(shopItem);
            Assert.Equal(equipment.ItemId, shopItem.ItemUsable.ItemId);
            Assert.Equal(price, shopItem.Price);
            Assert.Equal(productId, shopItem.ProductId);
            Assert.Equal(_agentAddress, shopItem.SellerAgentAddress);
            Assert.Equal(_avatarAddress, shopItem.SellerAvatarAddress);
        }
    }
}
