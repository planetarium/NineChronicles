namespace Lib9c.Tests.Action.Scenario
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
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static SerializeKeys;

    public class SellAndCancellationAndSellTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly IAccountStateDelta _initialState;

        public SellAndCancellationAndSellTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var gold = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);

            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = _agentAddress.Derive("avatar");
            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses[0] = _avatarAddress;
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            )
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };

            _initialState = new Tests.Action.State()
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(
                    _avatarAddress.Derive(LegacyInventoryKey),
                    avatarState.inventory.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyQuestListKey),
                    avatarState.questList.Serialize());

            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Fact]
        public void Execute_With_TradableMaterial()
        {
            var previousStates = _initialState;
            var apStoneRow = _tableSheets.MaterialItemSheet.OrderedList!.First(row =>
                row.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(apStoneRow);
            var inventoryAddr = _avatarAddress.Derive(LegacyInventoryKey);
            var inventory = new Inventory((List)previousStates.GetState(inventoryAddr));
            // Add 10 ap stones to inventory.
            inventory.AddFungibleItem(apStone, 10);
            previousStates = previousStates.SetState(inventoryAddr, inventory.Serialize());

            // sell ap stones with count 1, 2, 3, 4.
            var sellBlockIndex = 1L;
            var random = new TestRandom();
            var orderIds = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
            var sellActions = new[]
            {
                GetSell(apStone, 1, orderIds[0]),
                GetSell(apStone, 2, orderIds[1]),
                GetSell(apStone, 3, orderIds[2]),
                GetSell(apStone, 4, orderIds[3]),
            };
            var nextStates = previousStates;
            foreach (var sellAction in sellActions)
            {
                nextStates = sellAction.Execute(new ActionContext
                {
                    Signer = _agentAddress,
                    PreviousStates = nextStates,
                    BlockIndex = sellBlockIndex,
                    Random = random,
                    Rehearsal = false,
                });
                // TODO: Check state.. inventory, orders..
            }

            // Check inventory does not have ap stones.
            var nextInventory = new Inventory((List)nextStates.GetState(inventoryAddr));
            Assert.False(nextInventory.RemoveFungibleItem(
                apStone.FungibleId,
                sellBlockIndex,
                1));

            // Cancel sell orders.
            var sellCancellationActions = new[]
            {
                GetSellCancellation(orderIds[0], apStone),
                GetSellCancellation(orderIds[1], apStone),
                GetSellCancellation(orderIds[2], apStone),
                GetSellCancellation(orderIds[3], apStone),
            };
            foreach (var sellCancellationAction in sellCancellationActions)
            {
                nextStates = sellCancellationAction.Execute(new ActionContext
                {
                    Signer = _agentAddress,
                    PreviousStates = nextStates,
                    BlockIndex = sellBlockIndex + 1L,
                    Random = random,
                    Rehearsal = false,
                });
                // TODO: Check state.. inventory, orders..
            }

            // Check inventory has 10 ap stones.
            nextInventory = new Inventory((List)nextStates.GetState(inventoryAddr));
            Assert.True(nextInventory.RemoveFungibleItem(
                apStone.FungibleId,
                sellBlockIndex + 1L,
                10));

            // Sell 10 ap stones at once.
            var newSellOrderId = Guid.NewGuid();
            var newSellAction = GetSell(apStone, 10, newSellOrderId);
            nextStates = newSellAction.Execute(new ActionContext
            {
                Signer = _agentAddress,
                PreviousStates = nextStates,
                BlockIndex = sellBlockIndex + 2L,
                Random = random,
                Rehearsal = false,
            });

            // Check inventory does not have ap stones.
            nextInventory = new Inventory((List)nextStates.GetState(inventoryAddr));
            Assert.False(nextInventory.RemoveFungibleItem(
                apStone.FungibleId,
                sellBlockIndex + 2L,
                1));
        }

        private Sell GetSell(ITradableItem tradableItem, int count, Guid orderId) =>
            new Sell
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = tradableItem.TradableId,
                count = count,
                price = new FungibleAssetValue(
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                    Currency.Legacy("NCG", 2, null),
#pragma warning restore CS0618
                    1,
                    0),
                itemSubType = tradableItem.ItemSubType,
                orderId = orderId,
            };

        private SellCancellation GetSellCancellation(Guid orderId, ITradableItem tradableItem) =>
            new SellCancellation
            {
                orderId = orderId,
                tradableId = tradableItem.TradableId,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = tradableItem.ItemSubType,
            };
    }
}
