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

    public class SellCancellation2Test
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;

        public SellCancellation2Test(ITestOutputHelper outputHelper)
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
            var shopState = new ShopState();
            shopState.Register(new ShopItem(
                _agentAddress,
                _avatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(goldCurrencyState.Currency, 100, 0),
                equipment));

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
                avatarState.Update2(mail);
            }

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
            Assert.Single(shopState.Products);

            var (_, shopItem) = shopState.Products.FirstOrDefault();
            Assert.NotNull(shopItem);

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            Assert.False(avatarState.inventory.TryGetNonFungibleItem(
                shopItem.ItemUsable.ItemId,
                out ItemUsable _));

            var sellCancellationAction = new SellCancellation2
            {
                productId = shopItem.ProductId,
                sellerAvatarAddress = _avatarAddress,
            };
            var nextState = sellCancellationAction.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _agentAddress,
            });

            var nextShopState = nextState.GetShopState();
            Assert.Empty(nextShopState.Products);

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.True(nextAvatarState.inventory.TryGetNonFungibleItem(
                shopItem.ItemUsable.ItemId,
                out ItemUsable _));
            Assert.Equal(30, nextAvatarState.mailBox.Count);
        }
    }
}
