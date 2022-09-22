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

    public class Sell5Test
    {
        private const long ProductPrice = 100;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Currency _currency;
        private readonly AvatarState _avatarState;
        private readonly TableSheets _tableSheets;
        private IAccountStateDelta _initialState;

        public Sell5Test(ITestOutputHelper outputHelper)
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

            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, goldCurrencyState.Serialize())
                .SetState(Addresses.Shop, shopState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize());
        }

        [Theory]
        [InlineData(ItemType.Consumable, true, 2, 1, 1, 1)]
        [InlineData(ItemType.Costume, true, 2, 1, 1, 1)]
        [InlineData(ItemType.Equipment, true, 2, 1, 1, 1)]
        [InlineData(ItemType.Consumable, false, 0, 1, 1, 1)]
        [InlineData(ItemType.Costume, false, 0, 1, 1, 1)]
        [InlineData(ItemType.Equipment, false, 0, 1, 1, 1)]
        [InlineData(ItemType.Material, true, 1, 2, 1, 1)]
        [InlineData(ItemType.Material, true, 1, 1, 2, 1)]
        [InlineData(ItemType.Material, true, 2, 1, 2, 2)]
        [InlineData(ItemType.Material, true, 3, 2, 2, 2)]
        [InlineData(ItemType.Material, false, 1, 1, 1, 1)]
        public void Execute(
            ItemType itemType,
            bool shopItemExist,
            long blockIndex,
            int itemCount,
            int prevCount,
            int expectedProductsCount
        )
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);

            ITradableItem tradableItem;
            switch (itemType)
            {
                case ItemType.Consumable:
                    tradableItem = ItemFactory.CreateItemUsable(
                        _tableSheets.ConsumableItemSheet.First,
                        Guid.NewGuid(),
                        0);
                    break;
                case ItemType.Costume:
                    tradableItem = ItemFactory.CreateCostume(
                        _tableSheets.CostumeItemSheet.First,
                        Guid.NewGuid());
                    break;
                case ItemType.Equipment:
                    tradableItem = ItemFactory.CreateItemUsable(
                        _tableSheets.EquipmentItemSheet.First,
                        Guid.NewGuid(),
                        0);
                    break;
                case ItemType.Material:
                    var tradableMaterialRow = _tableSheets.MaterialItemSheet.OrderedList
                        .First(row => row.ItemSubType == ItemSubType.Hourglass);
                    tradableItem = ItemFactory.CreateTradableMaterial(tradableMaterialRow);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null);
            }

            Assert.Equal(0, tradableItem.RequiredBlockIndex);
            avatarState.inventory.AddItem2((ItemBase)tradableItem, itemCount);

            var previousStates = _initialState;
            previousStates = previousStates.SetState(_avatarAddress, avatarState.Serialize());
            var currencyState = previousStates.GetGoldCurrency();
            var price = new FungibleAssetValue(currencyState, ProductPrice, 0);
            var productId = new Guid("6f460c1a755d48e4ad6765d5f519dbc8");
            var shardedShopAddress = ShardedShopState.DeriveAddress(
                tradableItem.ItemSubType,
                productId);
            if (shopItemExist)
            {
                tradableItem.RequiredBlockIndex = blockIndex;
                Assert.Equal(blockIndex, tradableItem.RequiredBlockIndex);
                var shopItem = new ShopItem(
                    _agentAddress,
                    _avatarAddress,
                    expectedProductsCount == 2 ? Guid.NewGuid() : productId,
                    price,
                    blockIndex,
                    tradableItem,
                    prevCount
                );

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

            var sellAction = new Sell5
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = tradableItem.TradableId,
                count = itemCount,
                price = price,
                itemSubType = tradableItem.ItemSubType,
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

            // Check AvatarState and Inventory
            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.Single(nextAvatarState.inventory.Items);
            Assert.True(nextAvatarState.inventory.TryGetTradableItems(
                tradableItem.TradableId,
                expiredBlockIndex,
                1,
                out var inventoryItems));
            Assert.Single(inventoryItems);
            ITradableItem nextTradableItem = (ITradableItem)inventoryItems.First().item;
            Assert.Equal(expiredBlockIndex, nextTradableItem.RequiredBlockIndex);

            // Check ShardedShopState and ShopItem
            var nextSerializedShardedShopState = nextState.GetState(shardedShopAddress);
            Assert.NotNull(nextSerializedShardedShopState);
            var nextShardedShopState =
                new ShardedShopState((Dictionary)nextSerializedShardedShopState);
            Assert.Equal(expectedProductsCount, nextShardedShopState.Products.Count);

            var nextShopItem = nextShardedShopState.Products.Values.First(s => s.ExpiredBlockIndex == expiredBlockIndex);
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
                    nextTradableItemInShopItem = nextShopItem.TradableFungibleItem;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null);
            }

            Assert.Equal(price, nextShopItem.Price);
            Assert.Equal(expiredBlockIndex, nextShopItem.ExpiredBlockIndex);
            Assert.Equal(_agentAddress, nextShopItem.SellerAgentAddress);
            Assert.Equal(_avatarAddress, nextShopItem.SellerAvatarAddress);
            Assert.Equal(expiredBlockIndex, nextTradableItemInShopItem.RequiredBlockIndex);

            var mailList = nextAvatarState.mailBox.Where(m => m is SellCancelMail).ToList();
            Assert.Single(mailList);
            var mail = mailList.First() as SellCancelMail;
            Assert.NotNull(mail);
            Assert.Equal(expiredBlockIndex, mail.requiredBlockIndex);

            ITradableItem attachmentItem;
            int attachmentCount = 0;
            switch (itemType)
            {
                case ItemType.Consumable:
                case ItemType.Equipment:
                    Assert.NotNull(mail.attachment.itemUsable);
                    attachmentItem = mail.attachment.itemUsable;
                    Assert.Equal(tradableItem, mail.attachment.itemUsable);
                    break;
                case ItemType.Costume:
                    Assert.NotNull(mail.attachment.costume);
                    attachmentItem = mail.attachment.costume;
                    Assert.Equal(tradableItem, mail.attachment.costume);
                    break;
                case ItemType.Material:
                    Assert.NotNull(mail.attachment.tradableFungibleItem);
                    attachmentItem = mail.attachment.tradableFungibleItem;
                    attachmentCount = mail.attachment.tradableFungibleItemCount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null);
            }

            Assert.Equal(attachmentCount, nextShopItem.TradableFungibleItemCount);
            Assert.Equal(nextTradableItem, attachmentItem);
            Assert.Equal(nextTradableItemInShopItem, attachmentItem);
        }

        [Fact]
        public void Execute_Throw_InvalidPriceException()
        {
            var action = new Sell5
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = default,
                count = 1,
                price = -1 * _currency,
                itemSubType = default,
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
            var action = new Sell5
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = default,
                count = 1,
                price = 0 * _currency,
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

            var action = new Sell5
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = default,
                count = 1,
                price = 0 * _currency,
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
            var action = new Sell5
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = default,
                count = 1,
                price = 0 * _currency,
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

            var action = new Sell5
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = equipmentId,
                count = 1,
                price = 0 * _currency,
                itemSubType = ItemSubType.Food,
            };

            Assert.Throws<InvalidItemTypeException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 11,
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }
    }
}
