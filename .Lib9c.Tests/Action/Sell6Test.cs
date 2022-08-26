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

    public class Sell6Test
    {
        private const long ProductPrice = 100;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Currency _currency;
        private readonly AvatarState _avatarState;
        private readonly TableSheets _tableSheets;
        private IAccountStateDelta _initialState;

        public Sell6Test(ITestOutputHelper outputHelper)
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
            var expectedProductId = new Guid("6f460c1a755d48e4ad6765d5f519dbc8");
            var productId = new Guid("229e5f8c-fabe-4c04-bab9-45325cfa69a4");
            var shardedShopAddress = ShardedShopState.DeriveAddress(
                tradableItem.ItemSubType,
                expectedProductId);
            if (shopItemExist)
            {
                tradableItem.RequiredBlockIndex = blockIndex;
                Assert.Equal(blockIndex, tradableItem.RequiredBlockIndex);
                var shopItem = new ShopItem(
                    _agentAddress,
                    _avatarAddress,
                    productId,
                    new FungibleAssetValue(currencyState, 1, 0),
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

            var sellAction = new Sell6
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
            Assert.Equal(expectedProductId, nextShopItem.ProductId);
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

        [Theory]
        [InlineData(
            1617615,
            ItemSubType.Hourglass,
            "Bb2E9752C66B909a31CA8Db19927e02749d45157",
            "B4F6c2D629D287D0ee8ab847B5Ee5761eC530E4d",
            1614868,
            1633615,
            500,
            1250,
            5000,
            1225
        )]
        [InlineData(
            1617615,
            ItemSubType.Weapon,
            "138bF0C6d9534Ef0b51AeFa1e10EFcBeF7eb491b",
            "e0bD7637429040Ae0aAa0a2Ec4E4f1b7CEE19166",
            1552871,
            1633615,
            0,
            1,
            10,
            10
        )]
        [InlineData(
            1618153,
            ItemSubType.Hourglass,
            "F28B7D3B537148AF741b979094769F8d9BdF551f",
            "06f5bed46Cf84932afBB3B73D232b21D47d48b8B",
            1614910,
            1634153,
            80,
            15,
            800,
            12
        )]
        [InlineData(
            1618617,
            ItemSubType.Hourglass,
            "f189C04126E2E708Cd7D17CD68a7B7f10Bbb6f16",
            "F1A005C01E683dBcAb9A306d5cC70D5E57fccFa9",
            1613335,
            1634617,
            1265,
            80,
            12650,
            69657440
        )]
        [InlineData(
            1618700,
            ItemSubType.Hourglass,
            "dDCe3c1416fbD7d0533145bE281FBF5efA90f000",
            "3B56f1E3Ea3f37B50CcDA73470d94E312f5883f5",
            1613273,
            1634700,
            2000,
            1,
            20000,
            100010
        )]
        [InlineData(
            1618778,
            ItemSubType.Hourglass,
            "dDCe3c1416fbD7d0533145bE281FBF5efA90f000",
            "3B56f1E3Ea3f37B50CcDA73470d94E312f5883f5",
            1613273,
            1634778,
            1500,
            1000,
            15000,
            10000
        )]
        [InlineData(
            1618779,
            ItemSubType.Hourglass,
            "dDCe3c1416fbD7d0533145bE281FBF5efA90f000",
            "3B56f1E3Ea3f37B50CcDA73470d94E312f5883f5",
            1613275,
            1634779,
            1300,
            1000,
            13000,
            10000
        )]
        [InlineData(
            1618781,
            ItemSubType.Hourglass,
            "Ff008DD3070405c1B783BfEAbf2CE733646Bb726",
            "a4A2cCE75484911F74FD74Ac850446BAb160c610",
            1615455,
            1634781,
            500,
            1000,
            5000,
            10000
        )]
        [InlineData(
            1618976,
            ItemSubType.Hourglass,
            "69449ee94DCe543D4a060AE9E92aa2C122b76B3a",
            "59112DB0B61857213EdF15621DC7D2d27B0d5869",
            1618090,
            1634976,
            80,
            4,
            800,
            80842416
        )]
        [InlineData(
            1619118,
            ItemSubType.Hourglass,
            "Bb2E9752C66B909a31CA8Db19927e02749d45157",
            "B4F6c2D629D287D0ee8ab847B5Ee5761eC530E4d",
            1614817,
            1635118,
            500,
            1,
            5000,
            20106042
        )]
        [InlineData(
            1619326,
            ItemSubType.Hourglass,
            "B8CB064514cF38e4D50B8EB75E34a928091511B9",
            "53b1Bc1564977AAa1ff40A23834482a3722aff2C",
            1615750,
            1635326,
            10000,
            2,
            100000,
            42120802
        )]
        [InlineData(
            1619356,
            ItemSubType.Hourglass,
            "3d08eeC5c7FED047777f0e771A29237400e559BA",
            "2F4f646f9Ee52Ff3b9B3880a3166682E7284024B",
            1613346,
            1635356,
            8000,
            80,
            160000,
            800
        )]
        [InlineData(
            1625533,
            ItemSubType.Hourglass,
            "1C9b7B4119E6E2919C35b6899b00257291D2531A",
            "DF85d94517828D202641A6C973Cd10F9563BC835",
            1486926,
            1641533,
            0,
            1,
            69,
            249
        )]
        [InlineData(
            1625997,
            ItemSubType.Hourglass,
            "1090DF3A5743dCDA546b33d02d5067765394f146",
            "7F9EB9E3A73a6F242cA4Aa2329e4F1cc8263417B",
            1575429,
            1641997,
            9,
            1,
            1500,
            1250
        )]
        public void Execute_20210604(
            long blockIndex,
            ItemSubType itemSubType,
            string agentAddressHex,
            string avatarAddressHex,
            long shopExpiredBlockIndex,
            long expectedBlockIndex,
            int shopItemCount,
            int itemCount,
            int shopPrice,
            int expectedPrice)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var sellerAgentAddress = new Address(agentAddressHex);
            var sellerAvatarAddress = new Address(avatarAddressHex);

            ITradableItem tradableItem;
            switch (itemSubType)
            {
                case ItemSubType.Weapon:
                    tradableItem = ItemFactory.CreateItemUsable(
                        _tableSheets.EquipmentItemSheet.OrderedList.First(row => row.ItemSubType == ItemSubType.Weapon),
                        Guid.NewGuid(),
                        1);
                    break;
                case ItemSubType.Hourglass:
                    var tradableMaterialRow = _tableSheets.MaterialItemSheet.OrderedList
                        .First(row => row.ItemSubType == ItemSubType.Hourglass);
                    tradableItem = ItemFactory.CreateTradableMaterial(tradableMaterialRow);
                    tradableItem.RequiredBlockIndex = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemSubType), itemSubType, null);
            }

            Assert.Equal(1, tradableItem.RequiredBlockIndex);
            avatarState.inventory.AddItem2((ItemBase)tradableItem, itemCount);

            var previousStates = _initialState;
            previousStates = previousStates.SetState(_avatarAddress, avatarState.Serialize());
            var currencyState = previousStates.GetGoldCurrency();
            var price = new FungibleAssetValue(currencyState, expectedPrice, 0);
            var productId = new Guid("6f460c1a755d48e4ad6765d5f519dbc8");
            var shardedShopAddress = ShardedShopState.DeriveAddress(
                tradableItem.ItemSubType,
                productId);
            Assert.Equal(1, tradableItem.RequiredBlockIndex);
            var shopItem = new ShopItem(
                sellerAgentAddress,
                sellerAvatarAddress,
                Guid.NewGuid(),
                new FungibleAssetValue(currencyState, shopPrice, 0),
                shopExpiredBlockIndex,
                tradableItem,
                shopItemCount
            );

            var shardedShopState = new ShardedShopState(shardedShopAddress);
            shardedShopState.Register(shopItem);
            Assert.Single(shardedShopState.Products);
            previousStates = previousStates.SetState(
                shardedShopAddress,
                shardedShopState.Serialize());

            var sellAction = new Sell6
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = tradableItem.TradableId,
                count = itemCount,
                price = price,
                itemSubType = tradableItem.ItemSubType,
            };
            var nextState = sellAction.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = previousStates,
                Rehearsal = false,
                Signer = _agentAddress,
                Random = new TestRandom(),
            });

            // Check AvatarState and Inventory
            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.Single(nextAvatarState.inventory.Items);
            Assert.True(nextAvatarState.inventory.TryGetTradableItems(
                tradableItem.TradableId,
                expectedBlockIndex,
                itemCount,
                out var inventoryItems));
            Assert.Single(inventoryItems);
            ITradableItem nextTradableItem = (ITradableItem)inventoryItems.First().item;
            Assert.Equal(expectedBlockIndex, nextTradableItem.RequiredBlockIndex);

            // Check ShardedShopState and ShopItem
            var nextSerializedShardedShopState = nextState.GetState(shardedShopAddress);
            Assert.NotNull(nextSerializedShardedShopState);
            var nextShardedShopState =
                new ShardedShopState((Dictionary)nextSerializedShardedShopState);
            Assert.Equal(2, nextShardedShopState.Products.Count);
            Assert.Single(nextShardedShopState.Products.Values.Where(s => s.Equals(shopItem)));
            Assert.Single(nextShardedShopState.Products.Values.Where(s => !s.Equals(shopItem)));
            ShopItem nextShopItem = nextShardedShopState.Products.Values.First(s => !s.Equals(shopItem));

            ITradableItem innerShopItem = nextShopItem.TradableFungibleItem;
            int fungibleCount = itemCount;
            if (itemSubType == ItemSubType.Weapon)
            {
                innerShopItem = nextShopItem.ItemUsable;
                fungibleCount = 0;
            }

            Assert.Equal(price, nextShopItem.Price);
            Assert.Equal(expectedBlockIndex, nextShopItem.ExpiredBlockIndex);
            Assert.Equal(_agentAddress, nextShopItem.SellerAgentAddress);
            Assert.Equal(_avatarAddress, nextShopItem.SellerAvatarAddress);
            Assert.Equal(expectedBlockIndex, innerShopItem.RequiredBlockIndex);
            Assert.Equal(fungibleCount, nextShopItem.TradableFungibleItemCount);

            var mailList = nextAvatarState.mailBox.Where(m => m is SellCancelMail).ToList();
            Assert.Single(mailList);
            var mail = mailList.First() as SellCancelMail;
            Assert.NotNull(mail);
            Assert.Equal(expectedBlockIndex, mail.requiredBlockIndex);

            ITradableItem attachmentItem = itemSubType == ItemSubType.Weapon
                ? (ITradableItem)mail.attachment.itemUsable
                : mail.attachment.tradableFungibleItem;
            Assert.Equal(itemSubType == ItemSubType.Weapon, mail.attachment.tradableFungibleItem is null);
            Assert.Equal(fungibleCount, mail.attachment.tradableFungibleItemCount);
            Assert.Equal(nextTradableItem, attachmentItem);
        }

        [Fact]
        public void Execute_Throw_InvalidPriceException()
        {
            var action = new Sell6
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
            var action = new Sell6
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

            var action = new Sell6
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
            var action = new Sell6
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

            var action = new Sell6
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
