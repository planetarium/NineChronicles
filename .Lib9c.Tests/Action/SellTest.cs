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
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Currency _currency;
        private readonly AvatarState _avatarState;
        private readonly TableSheets _tableSheets;
        private IAccountStateDelta _initialState;

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

            _tableSheets = new TableSheets(sheets);

            _currency = new Currency("NCG", 2, minters: null);
            var goldCurrencyState = new GoldCurrencyState(_currency);

            var shopState = new ShopState();

            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);
            _avatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
            _avatarState = new AvatarState(
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

            var equipment = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                0);
            _avatarState.inventory.AddItem(equipment);

            var costume = ItemFactory.CreateCostume(
                _tableSheets.CostumeItemSheet.First,
                Guid.NewGuid());
            _avatarState.inventory.AddItem(costume);

            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, goldCurrencyState.Serialize())
                .SetState(Addresses.Shop, shopState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize());
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

            var costume = avatarState.inventory.Costumes.FirstOrDefault();
            Assert.NotNull(costume);

            var items = new INonFungibleItem[] { equipment, costume };

            var previousStates = _initialState;
            var currencyState = previousStates.GetGoldCurrency();
            var price = new FungibleAssetValue(currencyState, 100, 0);

            foreach (var nonFungibleItem in items)
            {
                var sellAction = new Sell2
                {
                    itemId = nonFungibleItem.ItemId,
                    price = price,
                    sellerAvatarAddress = _avatarAddress,
                };
                var nextState = sellAction.Execute(new ActionContext
                {
                    BlockIndex = 0,
                    PreviousStates = previousStates,
                    Rehearsal = false,
                    Signer = _agentAddress,
                    Random = new ItemEnhancementTest.TestRandom(),
                });

                var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
                Assert.Empty(nextAvatarState.inventory.Equipments);

                var nextShopState = nextState.GetShopState();
                Assert.Single(nextShopState.Products);

                var (_, shopItem) = nextShopState.Products.FirstOrDefault();
                Assert.NotNull(shopItem);
                Assert.Equal(nonFungibleItem.ItemId, shopItem.ItemUsable.ItemId);
                Assert.Equal(price, shopItem.Price);
                Assert.Equal(_agentAddress, shopItem.SellerAgentAddress);
                Assert.Equal(_avatarAddress, shopItem.SellerAvatarAddress);

                previousStates = nextState;
            }
        }

        [Fact]
        public void ExecuteThrowInvalidPriceException()
        {
            var action = new Sell
            {
                itemId = default,
                price = -1 * _currency,
                sellerAvatarAddress = _avatarAddress,
            };

            Assert.Throws<InvalidPriceException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Signer = _agentAddress,
            }));
        }

        [Fact]
        public void ExecuteThrowFailedLoadStateException()
        {
            var action = new Sell
            {
                itemId = default,
                price = 0 * _currency,
                sellerAvatarAddress = _avatarAddress,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = new State(),
                Signer = _agentAddress,
            }));
        }

        [Fact]
        public void ExecuteThrowNotEnoughClearedStageLevelException()
        {
            var avatarState = new AvatarState(_avatarState)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    0
                ),
            };

            _initialState = _initialState.SetState(_avatarAddress, avatarState.Serialize());

            var action = new Sell
            {
                itemId = default,
                price = 0 * _currency,
                sellerAvatarAddress = _avatarAddress,
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Signer = _agentAddress,
            }));
        }

        [Fact]
        public void ExecuteThrowItemDoesNotExistException()
        {
            var action = new Sell
            {
                itemId = default,
                price = 0 * _currency,
                sellerAvatarAddress = _avatarAddress,
            };

            Assert.Throws<ItemDoesNotExistException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Signer = _agentAddress,
            }));
        }

        [Fact]
        public void ExecuteThrowRequiredBlockIndexException()
        {
            var equipmentId = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                equipmentId,
                10);
            _avatarState.inventory.AddItem(equipment);

            _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());

            var action = new Sell
            {
                itemId = equipmentId,
                price = 0 * _currency,
                sellerAvatarAddress = _avatarAddress,
            };

            Assert.Throws<RequiredBlockIndexException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Signer = _agentAddress,
            }));
        }
    }
}
