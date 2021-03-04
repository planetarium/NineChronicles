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

    public class SellCancellationTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private GoldCurrencyState _goldCurrencyState;
        private TableSheets _tableSheets;

        public SellCancellationTest(ITestOutputHelper outputHelper)
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

            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);
            _avatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
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
            agentState.avatarAddresses[0] = _avatarAddress;

            var shopState = new ShopState();

            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .SetState(Addresses.Shop, shopState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize());
        }

        [Theory]
        [InlineData(ItemType.Equipment, "F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4", Sell.ExpiredBlockIndex, true)]
        [InlineData(ItemType.Costume, "936DA01F-9ABD-4d9d-80C7-02AF85C822A8", Sell.ExpiredBlockIndex, true)]
        [InlineData(ItemType.Equipment, "F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4", 0, false)]
        [InlineData(ItemType.Costume, "936DA01F-9ABD-4d9d-80C7-02AF85C822A8", 0, false)]
        public void Execute(ItemType itemType, string guid, long requiredBlockIndex, bool contain)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            INonFungibleItem nonFungibleItem;
            Guid itemId = new Guid(guid);
            if (itemType == ItemType.Equipment)
            {
                var itemUsable = ItemFactory.CreateItemUsable(
                    _tableSheets.EquipmentItemSheet.First,
                    itemId,
                    requiredBlockIndex);
                nonFungibleItem = itemUsable;
            }
            else
            {
                var costume = ItemFactory.CreateCostume(_tableSheets.CostumeItemSheet.First, itemId);
                costume.Update(requiredBlockIndex);
                nonFungibleItem = costume;
            }

            if (contain)
            {
                avatarState.inventory.AddItem((ItemBase)nonFungibleItem);
            }

            var result = new DailyReward.DailyRewardResult()
            {
                id = default,
                materials = new Dictionary<Material, int>(),
            };

            for (var i = 0; i < 100; i++)
            {
                var mail = new DailyRewardMail(result, i, default, 0);
                avatarState.Update(mail);
            }

            ShopState shopState = _initialState.GetShopState();
            var shopItem = new ShopItem(
                _agentAddress,
                _avatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                requiredBlockIndex,
                nonFungibleItem);
            shopState.Register(shopItem);

            Assert.Equal(requiredBlockIndex, nonFungibleItem.RequiredBlockIndex);
            Assert.Equal(contain, avatarState.inventory.TryGetNonFungibleItem(itemId, out _));

            IAccountStateDelta prevState = _initialState
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(Addresses.Shop, shopState.Serialize());

            var sellCancellationAction = new SellCancellation
            {
                productId = shopItem.ProductId,
                sellerAvatarAddress = _avatarAddress,
            };
            var nextState = sellCancellationAction.Execute(new ActionContext
            {
                BlockIndex = 1,
                PreviousStates = prevState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _agentAddress,
            });

            var nextShopState = nextState.GetShopState();
            Assert.Empty(nextShopState.Products);

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.True(nextAvatarState.inventory.TryGetNonFungibleItem(itemId, out INonFungibleItem nextNonFungibleItem));
            Assert.Equal(1, nextNonFungibleItem.RequiredBlockIndex);
            Assert.Single(nextAvatarState.mailBox);
            Assert.IsType<SellCancelMail>(nextAvatarState.mailBox.First());
        }

        [Fact]
        public void ExecuteThrowItemDoesNotExistException()
        {
            ShopState shopState = _initialState.GetShopState();
            ItemUsable itemUsable = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                Sell.ExpiredBlockIndex);

            var shopItem = new ShopItem(
                _agentAddress,
                _avatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                Sell.ExpiredBlockIndex,
                itemUsable);
            shopState.Register(shopItem);

            IAccountStateDelta prevState = _initialState
                .SetState(Addresses.Shop, shopState.Serialize());

            var action = new SellCancellation
            {
                productId = shopItem.ProductId,
                sellerAvatarAddress = _avatarAddress,
            };

            Assert.Throws<ItemDoesNotExistException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = prevState,
                    Random = new TestRandom(),
                    Signer = _agentAddress,
                })
            );
        }
    }
}
