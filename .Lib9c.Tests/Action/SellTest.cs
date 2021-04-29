namespace Lib9c.Tests.Action
{
    using System;
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

    public class SellTest
    {
        private const long ProductPrice = 100;

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

            var consumable = ItemFactory.CreateItemUsable(
                _tableSheets.ConsumableItemSheet.First,
                Guid.NewGuid(),
                0);
            _avatarState.inventory.AddItem(consumable);

            var costume = ItemFactory.CreateCostume(
                _tableSheets.CostumeItemSheet.First,
                Guid.NewGuid());
            _avatarState.inventory.AddItem(costume);

            var tradableMaterialRow =
                _tableSheets.MaterialItemSheet.OrderedList.FirstOrDefault(row =>
                    row.ItemSubType == ItemSubType.Hourglass);
            var tradableMaterial = ItemFactory.CreateMaterial(
                tradableMaterialRow,
                true);
            _avatarState.inventory.AddItem(tradableMaterial);

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
        [InlineData(ItemType.Material, true, 2)]
        [InlineData(ItemType.Consumable, false, 0)]
        [InlineData(ItemType.Costume, false, 0)]
        [InlineData(ItemType.Equipment, false, 0)]
        [InlineData(ItemType.Material, false, 0)]
        public void Execute(ItemType itemType, bool shopItemExist, int blockIndex)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var inventoryItems = avatarState.inventory.Items
                .Where(i => i.item.ItemType == itemType)
                .ToList();
            Assert.NotEmpty(inventoryItems);

            var previousStates = _initialState;
            var currencyState = previousStates.GetGoldCurrency();
            var price = new FungibleAssetValue(currencyState, ProductPrice, 0);
            var tradableItem = (ITradableItem)inventoryItems.First().item;
            var productId = new Guid("6f460c1a755d48e4ad6765d5f519dbc8");
            var shardedShopAddress = ShardedShopState.DeriveAddress(
                tradableItem.ItemSubType,
                productId);
            if (shopItemExist)
            {
                ShopItem shopItem;
                if (tradableItem is INonFungibleItem nonFungibleItem)
                {
                    nonFungibleItem.Update(blockIndex);
                    Assert.Equal(blockIndex, nonFungibleItem.RequiredBlockIndex);

                    shopItem = new ShopItem(
                        _agentAddress,
                        _avatarAddress,
                        productId,
                        price,
                        blockIndex,
                        nonFungibleItem);
                }
                else
                {
                    shopItem = new ShopItem(
                        _agentAddress,
                        _avatarAddress,
                        productId,
                        price,
                        blockIndex,
                        tradableItem,
                        1);
                }

                var shardedShopState = new ShardedShopState(shardedShopAddress);
                shardedShopState.Register(shopItem);
                Assert.Single(shardedShopState.Products);
                previousStates = previousStates.SetState(
                    shardedShopAddress,
                    shardedShopState.Serialize());
            }
            else
            {
                Assert.Null(previousStates.GetState(shardedShopAddress));
            }

            var sellAction = new Sell
            {
                itemId = tradableItem.TradableId,
                itemSubType = tradableItem.ItemSubType,
                itemCount = 1,
                price = price,
                sellerAvatarAddress = _avatarAddress,
            };
            var nextState = sellAction.Execute(new ActionContext
            {
                BlockIndex = 1,
                PreviousStates = previousStates,
                Rehearsal = false,
                Signer = _agentAddress,
                Random = new TestRandom(),
            });

            const long expiredBlockIndex = Sell.ExpiredBlockIndex + 1;

            // Check AvatarState and Inventory
            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.True(nextAvatarState.inventory.TryGetTradableItemWithoutNonTradableFungibleItem(
                tradableItem.TradableId,
                out var nextInventoryItem));
            if (nextInventoryItem.item is INonFungibleItem nextNonFungibleItemInInventory)
            {
                Assert.Equal(expiredBlockIndex, nextNonFungibleItemInInventory.RequiredBlockIndex);
            }

            // Check ShardedShopState and ShopItem
            var nextSerializedShardedShopState = nextState.GetState(shardedShopAddress);
            Assert.NotNull(nextSerializedShardedShopState);
            var nextShardedShopState =
                new ShardedShopState((Dictionary)nextSerializedShardedShopState);
            Assert.Single(nextShardedShopState.Products);

            var nextShopItem = nextShardedShopState.Products.Values.First();
            ITradableItem nextTradableItemInShopItem;
            switch (itemType)
            {
                case ItemType.Consumable:
                case ItemType.Equipment:
                    nextTradableItemInShopItem = nextShopItem.ItemUsable;
                    break;
                case ItemType.Costume:
                    nextTradableItemInShopItem = nextShopItem.Costume;
                    break;
                case ItemType.Material:
                    nextTradableItemInShopItem = nextShopItem.Material;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null);
            }

            Assert.Equal(price, nextShopItem.Price);
            Assert.Equal(expiredBlockIndex, nextShopItem.ExpiredBlockIndex);
            Assert.Equal(_agentAddress, nextShopItem.SellerAgentAddress);
            Assert.Equal(_avatarAddress, nextShopItem.SellerAvatarAddress);

            if (nextTradableItemInShopItem is INonFungibleItem nextNonFungibleItemInShopItem)
            {
                Assert.Equal(expiredBlockIndex, nextNonFungibleItemInShopItem.RequiredBlockIndex);
            }

            var mailList = nextAvatarState.mailBox.Where(m => m is SellCancelMail).ToList();
            Assert.Single(mailList);
            Assert.Equal(expiredBlockIndex, mailList.First().requiredBlockIndex);
        }

        [Fact]
        public void Execute_Throw_InvalidPriceException()
        {
            var action = new Sell
            {
                itemId = default,
                itemSubType = default,
                itemCount = 1,
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
            var action = new Sell
            {
                itemId = default,
                itemSubType = ItemSubType.Food,
                itemCount = 1,
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

            var action = new Sell
            {
                itemId = default,
                itemSubType = ItemSubType.Food,
                itemCount = 1,
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
        public void Execute_Throw_ItemDoesNotExistException()
        {
            var action = new Sell
            {
                itemId = default,
                itemSubType = ItemSubType.Food,
                itemCount = 1,
                price = 0 * _currency,
                sellerAvatarAddress = _avatarAddress,
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
            _avatarState.inventory.AddItem(equipment);

            _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());

            var action = new Sell
            {
                itemId = equipmentId,
                itemSubType = ItemSubType.Food,
                itemCount = 1,
                price = 0 * _currency,
                sellerAvatarAddress = _avatarAddress,
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
            _avatarState.inventory.AddItem(equipment);

            _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());

            var action = new Sell
            {
                itemId = equipmentId,
                itemSubType = equipment.ItemSubType,
                itemCount = 1,
                price = 0 * _currency,
                sellerAvatarAddress = _avatarAddress,
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
