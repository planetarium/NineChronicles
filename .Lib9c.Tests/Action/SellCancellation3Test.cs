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

    public class SellCancellation3Test
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;

        public SellCancellation3Test(ITestOutputHelper outputHelper)
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

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(currency);

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

            var consumable = ItemFactory.CreateItemUsable(
                tableSheets.ConsumableItemSheet.First,
                Guid.NewGuid(),
                0);

            var costume = ItemFactory.CreateCostume(
                tableSheets.CostumeItemSheet.First,
                Guid.NewGuid());

            var shopState = new ShopState();
            shopState.Register(new ShopItem(
                _agentAddress,
                _avatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(goldCurrencyState.Currency, 100, 0),
                equipment));

            shopState.Register(new ShopItem(
                _agentAddress,
                _avatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(goldCurrencyState.Currency, 100, 0),
                consumable));

            shopState.Register(new ShopItem(
                _agentAddress,
                _avatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(goldCurrencyState.Currency, 100, 0),
                costume));

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
            var productsCount = shopState.Products.Count;
            var shopItems = shopState.Products.Values.ToList();
            Assert.NotNull(shopItems);
            var previousStates = _initialState;
            var avatarState = previousStates.GetAvatarState(_avatarAddress);

            foreach (var shopItem in shopItems)
            {
                if (shopItem.ItemUsable != null)
                {
                    Assert.False(avatarState.inventory.TryGetNonFungibleItem<ItemUsable>(
                        shopItem.ItemUsable.ItemId, out _));

                    var sellCancellationAction = new SellCancellation3
                    {
                        productId = shopItem.ProductId,
                        sellerAvatarAddress = _avatarAddress,
                    };
                    var nextState = sellCancellationAction.Execute(new ActionContext
                    {
                        BlockIndex = 0,
                        PreviousStates = previousStates,
                        Random = new TestRandom(),
                        Rehearsal = false,
                        Signer = _agentAddress,
                    });
                    productsCount--;

                    var nextShopState = nextState.GetShopState();
                    Assert.Equal(productsCount, nextShopState.Products.Count);

                    var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
                    Assert.True(nextAvatarState.inventory.TryGetNonFungibleItem<ItemUsable>(
                        shopItem.ItemUsable.ItemId, out _));

                    previousStates = nextState;
                }

                if (shopItem.Costume != null)
                {
                    Assert.False(avatarState.inventory.TryGetNonFungibleItem<Costume>(
                        shopItem.Costume.ItemId, out var _));

                    var sellCancellationAction = new SellCancellation3
                    {
                        productId = shopItem.ProductId,
                        sellerAvatarAddress = _avatarAddress,
                    };
                    var nextState = sellCancellationAction.Execute(new ActionContext
                    {
                        BlockIndex = 0,
                        PreviousStates = previousStates,
                        Random = new TestRandom(),
                        Rehearsal = false,
                        Signer = _agentAddress,
                    });
                    productsCount--;

                    var nextShopState = nextState.GetShopState();
                    Assert.Equal(productsCount, nextShopState.Products.Count);

                    var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
                    Assert.True(nextAvatarState.inventory.TryGetNonFungibleItem<Costume>(
                        shopItem.Costume.ItemId, out _));

                    previousStates = nextState;
                }
            }
        }
    }
}
