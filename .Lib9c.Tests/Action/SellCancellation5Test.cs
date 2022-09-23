namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
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

    public class SellCancellation5Test
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly TableSheets _tableSheets;

        public SellCancellation5Test(ITestOutputHelper outputHelper)
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
        [InlineData(ItemType.Equipment, "F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4", true)]
        [InlineData(ItemType.Costume, "936DA01F-9ABD-4d9d-80C7-02AF85C822A8", true)]
        [InlineData(ItemType.Equipment, "F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4", false)]
        [InlineData(ItemType.Costume, "936DA01F-9ABD-4d9d-80C7-02AF85C822A8", false)]
        public void Execute(ItemType itemType, string guid, bool contain)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            INonFungibleItem nonFungibleItem;
            Guid itemId = new Guid(guid);
            Guid productId = itemId;
            ItemSubType itemSubType;
            const long requiredBlockIndex = 0;
            ShopState legacyShopState = _initialState.GetShopState();
            if (itemType == ItemType.Equipment)
            {
                var itemUsable = ItemFactory.CreateItemUsable(
                    _tableSheets.EquipmentItemSheet.First,
                    itemId,
                    requiredBlockIndex);
                nonFungibleItem = itemUsable;
                itemSubType = itemUsable.ItemSubType;
            }
            else
            {
                var costume = ItemFactory.CreateCostume(_tableSheets.CostumeItemSheet.First, itemId);
                costume.Update(requiredBlockIndex);
                nonFungibleItem = costume;
                itemSubType = costume.ItemSubType;
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
                nonFungibleItem);

            if (contain)
            {
                shopState.Register(shopItem);
                avatarState.inventory.AddItem2((ItemBase)nonFungibleItem);
                Assert.Empty(legacyShopState.Products);
                Assert.Single(shopState.Products);
            }
            else
            {
                legacyShopState.Register(shopItem);
                Assert.Single(legacyShopState.Products);
                Assert.Empty(shopState.Products);
            }

            Assert.Equal(requiredBlockIndex, nonFungibleItem.RequiredBlockIndex);
            Assert.Equal(contain, avatarState.inventory.TryGetNonFungibleItem(itemId, out _));

            IAccountStateDelta prevState = _initialState
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(Addresses.Shop, legacyShopState.Serialize())
                .SetState(shardedShopAddress, shopState.Serialize());

            var sellCancellationAction = new SellCancellation5
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
            Assert.True(nextAvatarState.inventory.TryGetNonFungibleItem(itemId, out INonFungibleItem nextNonFungibleItem));
            Assert.Equal(1, nextNonFungibleItem.RequiredBlockIndex);
            Assert.Equal(30, nextAvatarState.mailBox.Count);
            ShopState nextLegacyShopState = nextState.GetShopState();
            Assert.Empty(nextLegacyShopState.Products);
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException()
        {
            var action = new SellCancellation5
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

            var action = new SellCancellation5
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

        [Fact]
        public void Execute_Throw_ItemDoesNotExistException()
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
                _avatarAddress,
                productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                Sell6.ExpiredBlockIndex,
                itemUsable);

            IAccountStateDelta prevState = _initialState
                .SetState(shardedShopAddress, shopState.Serialize());

            var action = new SellCancellation5
            {
                productId = shopItem.ProductId,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = itemUsable.ItemSubType,
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

            var action = new SellCancellation5
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

            var action = new SellCancellation5
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
