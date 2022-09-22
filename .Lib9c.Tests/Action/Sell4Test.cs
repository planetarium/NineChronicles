namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

    public class Sell4Test
    {
        private const long ProductPrice = 100;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Currency _currency;
        private readonly AvatarState _avatarState;
        private readonly TableSheets _tableSheets;
        private IAccountStateDelta _initialState;

        public Sell4Test(ITestOutputHelper outputHelper)
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
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
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
            _avatarState.inventory.AddItem2(equipment);

            var consumable = ItemFactory.CreateItemUsable(
                _tableSheets.ConsumableItemSheet.First,
                Guid.NewGuid(),
                0);
            _avatarState.inventory.AddItem2(consumable);

            var costume = ItemFactory.CreateCostume(
                _tableSheets.CostumeItemSheet.First,
                Guid.NewGuid());
            _avatarState.inventory.AddItem2(costume);

            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, goldCurrencyState.Serialize())
                .SetState(Addresses.Shop, shopState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize());
        }

        [Theory]
        [InlineData(ItemType.Consumable, true, 2)]
        [InlineData(ItemType.Costume, true, 2)]
        [InlineData(ItemType.Equipment, true, 2)]
        [InlineData(ItemType.Consumable, false, 0)]
        [InlineData(ItemType.Costume, false, 0)]
        [InlineData(ItemType.Equipment, false, 0)]
        public void Execute(ItemType itemType, bool shopItemExist, int blockIndex)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            List<Inventory.Item> inventoryItem = avatarState.inventory.Items.Where(i => i.item.ItemType == itemType).ToList();
            Assert.Single(inventoryItem);
            var previousStates = _initialState;
            var currencyState = previousStates.GetGoldCurrency();
            var price = new FungibleAssetValue(currencyState, ProductPrice, 0);
            INonFungibleItem nonFungibleItem = (INonFungibleItem)inventoryItem.First().item;
            nonFungibleItem.RequiredBlockIndex = blockIndex;
            Assert.Equal(blockIndex, nonFungibleItem.RequiredBlockIndex);
            ItemSubType itemSubType = ItemSubType.Food;
            Guid productId = new Guid("6f460c1a-755d-48e4-ad67-65d5f519dbc8");
            if (nonFungibleItem is ItemUsable itemUsable)
            {
                itemSubType = itemUsable.ItemSubType;
            }
            else if (nonFungibleItem is Costume costume)
            {
                itemSubType = costume.ItemSubType;
            }

            Address shopAddress = ShardedShopState.DeriveAddress(itemSubType, productId);

            if (shopItemExist)
            {
                var si = new ShopItem(
                    _agentAddress,
                    _avatarAddress,
                    productId,
                    new FungibleAssetValue(currencyState, 100, 0),
                    blockIndex,
                    nonFungibleItem);
                ShardedShopState shardedShopState =
                    new ShardedShopState(shopAddress);
                shardedShopState.Register(si);
                Assert.Single(shardedShopState.Products);
                previousStates = previousStates.SetState(shopAddress, shardedShopState.Serialize());
            }
            else
            {
                Assert.Null(previousStates.GetState(shopAddress));
            }

            var sellAction = new Sell4
            {
                itemId = nonFungibleItem.NonFungibleId,
                price = price,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = itemSubType,
            };

            var nextState = sellAction.Execute(new ActionContext
            {
                BlockIndex = 1,
                PreviousStates = previousStates,
                Rehearsal = false,
                Signer = _agentAddress,
                Random = new TestRandom(),
            });

            const long expiredBlockIndex = Sell6.ExpiredBlockIndex + 1;
            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.True(nextAvatarState.inventory.TryGetNonFungibleItem(nonFungibleItem.NonFungibleId, out var nextItem));
            INonFungibleItem nextNonFungibleItem = (INonFungibleItem)nextItem.item;
            Assert.Equal(expiredBlockIndex, nextNonFungibleItem.RequiredBlockIndex);

            var nextShopState = new ShardedShopState((Dictionary)nextState.GetState(shopAddress));

            Assert.Single(nextShopState.Products);

            var products = nextShopState.Products.Values;

            var shopItem = products.First();
            INonFungibleItem item = itemType == ItemType.Costume ? (INonFungibleItem)shopItem.Costume : shopItem.ItemUsable;

            Assert.Equal(price, shopItem.Price);
            Assert.Equal(expiredBlockIndex, shopItem.ExpiredBlockIndex);
            Assert.Equal(expiredBlockIndex, item.RequiredBlockIndex);
            Assert.Equal(_agentAddress, shopItem.SellerAgentAddress);
            Assert.Equal(_avatarAddress, shopItem.SellerAvatarAddress);

            var mailList = nextAvatarState.mailBox.Where(m => m is SellCancelMail).ToList();
            Assert.Single(mailList);

            Assert.Equal(expiredBlockIndex, mailList.First().requiredBlockIndex);
        }

        [Fact]
        public void Execute_Throw_InvalidPriceException()
        {
            var action = new Sell4
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
        public void Execute_Throw_FailedLoadStateException()
        {
            var action = new Sell4
            {
                itemId = default,
                price = 0 * _currency,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = ItemSubType.Food,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = new State(),
                Signer = _agentAddress,
            }));
        }

        [Fact]
        public void Execute_Throw_NotEnoughClearedStageLevelException()
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

            var action = new Sell4
            {
                itemId = default,
                price = 0 * _currency,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = ItemSubType.Food,
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Signer = _agentAddress,
            }));
        }

        [Fact]
        public void Execute_Throw_ItemDoesNotExistException()
        {
            var action = new Sell4
            {
                itemId = default,
                price = 0 * _currency,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = ItemSubType.Food,
            };

            Assert.Throws<ItemDoesNotExistException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_Throw_InvalidItemTypeException()
        {
            var equipmentId = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                equipmentId,
                10);
            _avatarState.inventory.AddItem2(equipment);

            _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());

            var action = new Sell4
            {
                itemId = equipmentId,
                price = 0 * _currency,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = ItemSubType.Food,
            };

            Assert.Throws<InvalidItemTypeException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_Throw_RequiredBlockIndexException()
        {
            var equipmentId = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                equipmentId,
                10);
            _avatarState.inventory.AddItem2(equipment);

            _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());

            var action = new Sell4
            {
                itemId = equipmentId,
                price = 0 * _currency,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = equipment.ItemSubType,
            };

            Assert.Throws<RequiredBlockIndexException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }
    }
}
