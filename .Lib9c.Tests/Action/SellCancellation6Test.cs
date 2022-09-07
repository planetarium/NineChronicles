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
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class SellCancellation6Test
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly TableSheets _tableSheets;

        public SellCancellation6Test(ITestOutputHelper outputHelper)
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

            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(Addresses.Shop, new ShopState().Serialize())
                .SetState(_avatarAddress, avatarState.Serialize());
        }

        [Theory]
        [InlineData(ItemType.Equipment, "F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4", true, 1, 1, 1, 1)]
        [InlineData(ItemType.Costume, "936DA01F-9ABD-4d9d-80C7-02AF85C822A8", true, 1, 1, 1, 1)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", true, 1, 1, 1, 1)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", true, 2, 1, 2, 2)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", true, 2, 2, 3, 3)]
        [InlineData(ItemType.Equipment, "F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4", false, 1, 1, 0, 1)]
        [InlineData(ItemType.Costume, "936DA01F-9ABD-4d9d-80C7-02AF85C822A8", false, 1, 1, 0, 1)]
        public void Execute(ItemType itemType, string guid, bool contain, int itemCount, int inventoryCount, int prevCount, int expectedCount)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            ITradableItem tradableItem;
            Guid itemId = new Guid(guid);
            Guid productId = itemId;
            ItemSubType itemSubType;
            const long requiredBlockIndex = Sell6.ExpiredBlockIndex;
            ShopState legacyShopState = _initialState.GetShopState();
            if (itemType == ItemType.Equipment)
            {
                var itemUsable = ItemFactory.CreateItemUsable(
                    _tableSheets.EquipmentItemSheet.First,
                    itemId,
                    requiredBlockIndex);
                tradableItem = itemUsable;
                itemSubType = itemUsable.ItemSubType;
            }
            else if (itemType == ItemType.Costume)
            {
                var costume = ItemFactory.CreateCostume(_tableSheets.CostumeItemSheet.First, itemId);
                costume.Update(requiredBlockIndex);
                tradableItem = costume;
                itemSubType = costume.ItemSubType;
            }
            else
            {
                var material = ItemFactory.CreateTradableMaterial(
                    _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Hourglass));
                itemSubType = material.ItemSubType;
                material.RequiredBlockIndex = requiredBlockIndex;
                tradableItem = material;
            }

            var result = new DailyReward2.DailyRewardResult()
            {
                id = default,
                materials = new Dictionary<Material, int>(),
            };

            for (var i = 0; i < 100; i++)
            {
                var mail = new DailyRewardMail(result, i, default, 0);
                avatarState.Update2(mail);
            }

            Address shardedShopAddress = ShardedShopState.DeriveAddress(itemSubType, productId);
            ShardedShopState shopState = new ShardedShopState(shardedShopAddress);
            var shopItem = new ShopItem(
                _agentAddress,
                _avatarAddress,
                productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                requiredBlockIndex,
                tradableItem,
                itemCount);

            if (contain)
            {
                shopState.Register(shopItem);
                if (inventoryCount > 1)
                {
                    for (int i = 0; i < inventoryCount; i++)
                    {
                        // Different RequiredBlockIndex for divide inventory slot.
                        var tradable = new TradableMaterial((Dictionary)tradableItem.Serialize())
                        {
                            RequiredBlockIndex = tradableItem.RequiredBlockIndex - i,
                        };
                        avatarState.inventory.AddItem2(tradable, 2 - i);
                    }
                }
                else
                {
                    avatarState.inventory.AddItem2((ItemBase)tradableItem, itemCount);
                }

                Assert.Empty(legacyShopState.Products);
                Assert.Single(shopState.Products);
                Assert.Equal(inventoryCount, avatarState.inventory.Items.Count);
                Assert.Equal(prevCount, avatarState.inventory.Items.Sum(i => i.count));
            }
            else
            {
                legacyShopState.Register(shopItem);
                Assert.Single(legacyShopState.Products);
                Assert.Empty(shopState.Products);
            }

            Assert.Equal(requiredBlockIndex, tradableItem.RequiredBlockIndex);
            Assert.Equal(
                contain,
                avatarState.inventory.TryGetTradableItems(itemId, requiredBlockIndex, itemCount, out _)
            );

            IAccountStateDelta prevState = _initialState
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(Addresses.Shop, legacyShopState.Serialize())
                .SetState(shardedShopAddress, shopState.Serialize());

            var sellCancellationAction = new SellCancellation6
            {
                productId = shopItem.ProductId,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = itemSubType,
            };
            var nextState = sellCancellationAction.Execute(new ActionContext
            {
                BlockIndex = 1,
                PreviousStates = prevState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _agentAddress,
            });

            ShardedShopState nextShopState = new ShardedShopState((Dictionary)nextState.GetState(shardedShopAddress));
            Assert.Empty(nextShopState.Products);

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.Equal(expectedCount, nextAvatarState.inventory.Items.Sum(i => i.count));
            Assert.False(nextAvatarState.inventory.TryGetTradableItems(
                itemId,
                0,
                itemCount,
                out List<Inventory.Item> _
            ));
            Assert.True(nextAvatarState.inventory.TryGetTradableItems(
                itemId,
                requiredBlockIndex,
                itemCount,
                out List<Inventory.Item> inventoryItems
            ));
            Assert.Empty(inventoryItems.Select(i => (ITradableItem)i.item).Where(item => item.RequiredBlockIndex == requiredBlockIndex));
            Assert.Equal(inventoryCount, inventoryItems.Count);
            Inventory.Item inventoryItem = inventoryItems.First();
            Assert.Equal(itemCount, inventoryItem.count);
            Assert.Equal(inventoryCount, nextAvatarState.inventory.Items.Count);
            ITradableItem nextTradableItem = (ITradableItem)inventoryItem.item;
            Assert.Equal(1, nextTradableItem.RequiredBlockIndex);
            Assert.Equal(30, nextAvatarState.mailBox.Count);
            ShopState nextLegacyShopState = nextState.GetShopState();
            Assert.Empty(nextLegacyShopState.Products);
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException()
        {
            var action = new SellCancellation6
            {
                productId = default,
                sellerAvatarAddress = default,
                itemSubType = ItemSubType.Weapon,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = _initialState,
                    Random = new TestRandom(),
                    Signer = default,
                })
            );
        }

        [Fact]
        public void Execute_Throw_NotEnoughClearedStageLevelException()
        {
            var avatarState = new AvatarState(_initialState.GetAvatarState(_avatarAddress))
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    0
                ),
            };

            IAccountStateDelta prevState = _initialState.SetState(_avatarAddress, avatarState.Serialize());

            var action = new SellCancellation6
            {
                productId = default,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = ItemSubType.Weapon,
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = prevState,
                Signer = _agentAddress,
            }));
        }

        [Theory]
        [InlineData(ItemType.Equipment)]
        [InlineData(ItemType.Consumable)]
        [InlineData(ItemType.Costume)]
        [InlineData(ItemType.Material)]
        public void Execute_Throw_ItemDoesNotExistException(ItemType itemType)
        {
            Guid productId = Guid.NewGuid();
            ITradableItem tradableItem;
            ItemSheet.Row row;
            switch (itemType)
            {
                case ItemType.Consumable:
                    row = _tableSheets.ConsumableItemSheet.First;
                    break;
                case ItemType.Costume:
                    row = _tableSheets.CostumeItemSheet.First;
                    break;
                case ItemType.Equipment:
                    row = _tableSheets.EquipmentItemSheet.First;
                    break;
                case ItemType.Material:
                    row = _tableSheets.MaterialItemSheet.OrderedList
                        .First(r => r.ItemSubType == ItemSubType.Hourglass);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null);
            }

            if (itemType == ItemType.Material)
            {
                tradableItem = ItemFactory.CreateTradableMaterial((MaterialItemSheet.Row)row);
            }
            else
            {
                tradableItem = (ITradableItem)ItemFactory.CreateItem(row, new TestRandom());
            }

            tradableItem.RequiredBlockIndex = Sell6.ExpiredBlockIndex;

            Address shardedShopAddress = ShardedShopState.DeriveAddress(tradableItem.ItemSubType, productId);
            ShardedShopState shopState = new ShardedShopState(shardedShopAddress);

            var shopItem = new ShopItem(
                _agentAddress,
                _avatarAddress,
                productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                Sell6.ExpiredBlockIndex,
                tradableItem);

            IAccountStateDelta prevState = _initialState
                .SetState(shardedShopAddress, shopState.Serialize());

            var action = new SellCancellation6
            {
                productId = shopItem.ProductId,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = tradableItem.ItemSubType,
            };

            ItemDoesNotExistException exc = Assert.Throws<ItemDoesNotExistException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = prevState,
                    Random = new TestRandom(),
                    Signer = _agentAddress,
                })
            );
            Assert.Equal(itemType == ItemType.Material, !exc.Message.Contains("legacy"));
        }

        [Fact]
        public void Execute_Throw_InvalidAddressException_From_Agent()
        {
            Guid productId = Guid.NewGuid();
            ItemUsable itemUsable = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                Sell6.ExpiredBlockIndex);
            Address shardedShopAddress = ShardedShopState.DeriveAddress(itemUsable.ItemSubType, productId);
            ShardedShopState shopState = new ShardedShopState(shardedShopAddress);

            var shopItem = new ShopItem(
                default,
                _avatarAddress,
                productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                Sell6.ExpiredBlockIndex,
                itemUsable);
            shopState.Register(shopItem);

            IAccountStateDelta prevState = _initialState
                .SetState(shardedShopAddress, shopState.Serialize());

            var action = new SellCancellation6
            {
                productId = shopItem.ProductId,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = itemUsable.ItemSubType,
            };

            Assert.Throws<InvalidAddressException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = prevState,
                    Random = new TestRandom(),
                    Signer = _agentAddress,
                })
            );
        }

        [Fact]
        public void Execute_Throw_InvalidAddressException_From_Avatar()
        {
            Guid productId = Guid.NewGuid();
            ItemUsable itemUsable = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                Sell6.ExpiredBlockIndex);
            Address shardedShopAddress = ShardedShopState.DeriveAddress(itemUsable.ItemSubType, productId);
            ShardedShopState shopState = new ShardedShopState(shardedShopAddress);

            var shopItem = new ShopItem(
                _agentAddress,
                default,
                productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                Sell6.ExpiredBlockIndex,
                itemUsable);
            shopState.Register(shopItem);

            IAccountStateDelta prevState = _initialState
                .SetState(shardedShopAddress, shopState.Serialize());

            var action = new SellCancellation6
            {
                productId = shopItem.ProductId,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = itemUsable.ItemSubType,
            };

            Assert.Throws<InvalidAddressException>(() => action.Execute(new ActionContext()
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
