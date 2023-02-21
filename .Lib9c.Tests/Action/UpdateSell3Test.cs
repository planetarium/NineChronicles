namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.BlockChain.Policy;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class UpdateSell3Test
    {
        private const long ProductPrice = 100;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Currency _currency;
        private readonly AvatarState _avatarState;
        private readonly TableSheets _tableSheets;
        private readonly GoldCurrencyState _goldCurrencyState;
        private IAccountStateDelta _initialState;

        public UpdateSell3Test(ITestOutputHelper outputHelper)
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
            _goldCurrencyState = new GoldCurrencyState(_currency);

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
                .SetState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .SetState(Addresses.Shop, shopState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize());
        }

        [Theory]
        [InlineData(ItemType.Equipment, "F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4", 1, 1, 1, true)]
        [InlineData(ItemType.Costume, "936DA01F-9ABD-4d9d-80C7-02AF85C822A8", 1, 1, 1, true)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 1, 1, 1, true)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 2, 1, 2, true)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 2, 2, 3, true)]
        [InlineData(ItemType.Equipment, "F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4", 1, 1, 1, false)]
        [InlineData(ItemType.Costume, "936DA01F-9ABD-4d9d-80C7-02AF85C822A8", 1, 1, 1, false)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 1, 1, 1, false)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 2, 1, 2, false)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 2, 2, 3, false)]
        public void Execute(
            ItemType itemType,
            string guid,
            int itemCount,
            int inventoryCount,
            int expectedCount,
            bool fromPreviousAction
        )
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            ITradableItem tradableItem;
            var itemId = new Guid(guid);
            var orderId = Guid.NewGuid();
            var updateSellOrderId = Guid.NewGuid();
            ItemSubType itemSubType;
            const long requiredBlockIndex = Order.ExpirationInterval;
            switch (itemType)
            {
                case ItemType.Equipment:
                {
                    var itemUsable = ItemFactory.CreateItemUsable(
                        _tableSheets.EquipmentItemSheet.First,
                        itemId,
                        requiredBlockIndex);
                    tradableItem = itemUsable;
                    itemSubType = itemUsable.ItemSubType;
                    break;
                }

                case ItemType.Costume:
                {
                    var costume = ItemFactory.CreateCostume(_tableSheets.CostumeItemSheet.First, itemId);
                    costume.Update(requiredBlockIndex);
                    tradableItem = costume;
                    itemSubType = costume.ItemSubType;
                    break;
                }

                default:
                {
                    var material = ItemFactory.CreateTradableMaterial(
                        _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Hourglass));
                    itemSubType = material.ItemSubType;
                    material.RequiredBlockIndex = requiredBlockIndex;
                    tradableItem = material;
                    break;
                }
            }

            var shardedShopAddress = ShardedShopStateV2.DeriveAddress(itemSubType, orderId);
            var shopState = new ShardedShopStateV2(shardedShopAddress);
            var order = OrderFactory.Create(
                _agentAddress,
                _avatarAddress,
                orderId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                tradableItem.TradableId,
                requiredBlockIndex,
                itemSubType,
                itemCount
            );

            var orderDigestList = new OrderDigestListState(OrderDigestListState.DeriveAddress(_avatarAddress));
            var prevState = _initialState;

            if (inventoryCount > 1)
            {
                for (int i = 0; i < inventoryCount; i++)
                {
                    // Different RequiredBlockIndex for divide inventory slot.
                    if (tradableItem is ITradableFungibleItem tradableFungibleItem)
                    {
                        var tradable = (TradableMaterial)tradableFungibleItem.Clone();
                        tradable.RequiredBlockIndex = tradableItem.RequiredBlockIndex - i;
                        avatarState.inventory.AddItem(tradable, 2 - i);
                    }
                }
            }
            else
            {
                avatarState.inventory.AddItem((ItemBase)tradableItem, itemCount);
            }

            var sellItem = order.Sell(avatarState);
            var orderDigest = order.Digest(avatarState, _tableSheets.CostumeStatSheet);
            shopState.Add(orderDigest, requiredBlockIndex);
            orderDigestList.Add(orderDigest);

            Assert.True(avatarState.inventory.TryGetLockedItem(new OrderLock(orderId), out _));

            Assert.Equal(inventoryCount, avatarState.inventory.Items.Count);
            Assert.Equal(expectedCount, avatarState.inventory.Items.Sum(i => i.count));

            Assert.Single(shopState.OrderDigestList);
            Assert.Single(orderDigestList.OrderDigestList);

            Assert.Equal(requiredBlockIndex * 2, sellItem.RequiredBlockIndex);

            if (fromPreviousAction)
            {
                prevState = prevState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                prevState = prevState
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(_avatarAddress, avatarState.SerializeV2());
            }

            prevState = prevState
                .SetState(Addresses.GetItemAddress(itemId), sellItem.Serialize())
                .SetState(Order.DeriveAddress(order.OrderId), order.Serialize())
                .SetState(orderDigestList.Address, orderDigestList.Serialize())
                .SetState(shardedShopAddress, shopState.Serialize());

            var currencyState = prevState.GetGoldCurrency();
            var price = new FungibleAssetValue(currencyState, ProductPrice, 0);

            var updateSellInfo = new UpdateSellInfo(
                orderId,
                updateSellOrderId,
                itemId,
                itemSubType,
                price,
                itemCount
            );

            var action = new UpdateSell3
            {
                sellerAvatarAddress = _avatarAddress,
                updateSellInfos = new[] { updateSellInfo },
            };

            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 101,
                PreviousStates = prevState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _agentAddress,
            });

            var updateSellShopAddress = ShardedShopStateV2.DeriveAddress(itemSubType, updateSellOrderId);
            var nextShopState = new ShardedShopStateV2((Dictionary)nextState.GetState(updateSellShopAddress));
            Assert.Equal(1, nextShopState.OrderDigestList.Count);
            Assert.NotEqual(orderId, nextShopState.OrderDigestList.First().OrderId);
            Assert.Equal(updateSellOrderId, nextShopState.OrderDigestList.First().OrderId);
            Assert.Equal(itemId, nextShopState.OrderDigestList.First().TradableId);
            Assert.Equal(requiredBlockIndex + 101, nextShopState.OrderDigestList.First().ExpiredBlockIndex);
        }

        [Fact]
        public void Execute_Throw_ListEmptyException()
        {
            var action = new UpdateSell3
            {
                sellerAvatarAddress = _avatarAddress,
                updateSellInfos = new List<UpdateSellInfo>(),
            };

            Assert.Throws<ListEmptyException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = new State(),
                Signer = _agentAddress,
            }));
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException()
        {
            var updateSellInfo = new UpdateSellInfo(
                default,
                default,
                default,
                ItemSubType.Food,
                0 * _currency,
                1);

            var action = new UpdateSell3
            {
                sellerAvatarAddress = _avatarAddress,
                updateSellInfos = new[] { updateSellInfo },
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

            var updateSellInfo = new UpdateSellInfo(
                default,
                default,
                default,
                ItemSubType.Food,
                0 * _currency,
                1);

            var action = new UpdateSell3
            {
                sellerAvatarAddress = _avatarAddress,
                updateSellInfos = new[] { updateSellInfo },
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Signer = _agentAddress,
            }));
        }

        [Fact]
        public void Execute_Throw_InvalidPriceException()
        {
            var avatarState = new AvatarState(_avatarState)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop
                ),
            };
            var digestListAddress = OrderDigestListState.DeriveAddress(_avatarAddress);
            var digestList = new OrderDigestListState(digestListAddress);
            _initialState = _initialState
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(digestListAddress, digestList.Serialize());

            var updateSellInfo = new UpdateSellInfo(
                default,
                default,
                default,
                default,
                -1 * _currency,
                1);

            var action = new UpdateSell3
            {
                sellerAvatarAddress = _avatarAddress,
                updateSellInfos = new[] { updateSellInfo },
            };

            Assert.Throws<InvalidPriceException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Signer = _agentAddress,
            }));
        }

        [Fact]
        public void Execute_ActionObsoletedException()
        {
            var updateSellInfo = new UpdateSellInfo(
                default,
                default,
                default,
                default,
                -1 * _currency,
                1);

            var action = new UpdateSell3
            {
                sellerAvatarAddress = _avatarAddress,
                updateSellInfos = new[] { updateSellInfo },
            };

            Assert.Throws<ActionObsoletedException>(() => action.Execute(new ActionContext
            {
                BlockIndex = ActionObsoleteConfig.V100320ObsoleteIndex + 1,
                PreviousStates = _initialState,
                Signer = _agentAddress,
            }));
        }
    }
}
