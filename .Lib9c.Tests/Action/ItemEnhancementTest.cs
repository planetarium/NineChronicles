namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static SerializeKeys;

    public class ItemEnhancementTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;
        private readonly Currency _currency;
        private IAccountStateDelta _initialState;

        public ItemEnhancementTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.ToAddress();
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            agentState.avatarAddresses.Add(0, _avatarAddress);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var gold = new GoldCurrencyState(_currency);
            var slotAddress = _avatarAddress.Derive(string.Format(
                CultureInfo.InvariantCulture,
                CombinationSlotState.DeriveFormat,
                0
            ));

            var context = new ActionContext();
            _initialState = new MockStateDelta()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize())
                .SetState(slotAddress, new CombinationSlotState(slotAddress, 0).Serialize())
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .MintAsset(context, GoldCurrencyState.Address, gold.Currency * 100_000_000_000)
                .TransferAsset(
                    context,
                    Addresses.GoldCurrency,
                    _agentAddress,
                    gold.Currency * 3_000_000
                );

            Assert.Equal(
                gold.Currency * 99_997_000_000,
                _initialState.GetBalance(Addresses.GoldCurrency, gold.Currency)
            );
            Assert.Equal(
                gold.Currency * 3_000_000,
                _initialState.GetBalance(_agentAddress, gold.Currency)
            );

            foreach (var (key, value) in sheets)
            {
                _initialState =
                    _initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        // from 0 to 1 using one level 0 material
        [InlineData(0, 1, 0, 25, 0, 1)]
        // from 0 to N using multiple level 0 materials
        [InlineData(0, 2, 0, 87, 0, 3)]
        [InlineData(0, 4, 20, 837, 0, 15)]
        // from K to K with material(s). Check requiredBlock == 0
        [InlineData(10, 10, 0, 0, 0, 1)]
        // from K to N using one level X material
        [InlineData(5, 6, 40, 1300, 6, 1)]
        // from K to N using multiple materials
        [InlineData(5, 7, 120, 3800, 4, 6)]
        [InlineData(5, 9, 600, 10275, 7, 5)]
        // from 20 to 21 (just to reach level 21 exp)
        [InlineData(20, 21, 1310720, 7500, 20, 1)]
        // from 20 to 21 (over level 21)
        [InlineData(20, 21, 1310720, 7500, 20, 2)]
        // from 21 to 21 (no level up)
        [InlineData(21, 21, 0, 0, 1, 1)]
        [InlineData(21, 21, 0, 0, 21, 1)]
        // Test: change of exp, change of level, required block, NCG price
        public void Execute(
            int startLevel,
            int expectedLevel,
            int expectedCost,
            int expectedBlockIndex,
            int materialLevel,
            int materialCount)
        {
            var row = _tableSheets.EquipmentItemSheet.Values.First(r => r.Grade == 1 && r.Exp > 0);
            var equipment = (Equipment)ItemFactory.CreateItemUsable(row, default, 0, startLevel);
            if (startLevel == 0)
            {
                equipment.Exp = (long)row.Exp!;
            }
            else
            {
                equipment.Exp = _tableSheets.EnhancementCostSheetV3.Values.First(r =>
                    r.Grade == equipment.Grade && r.ItemSubType == equipment.ItemSubType &&
                    r.Level == equipment.level).Exp;
            }

            var startExp = equipment.Exp;
            _avatarState.inventory.AddItem(equipment, count: 1);

            var expectedExpIncrement = 0L;
            var materialIds = new List<Guid>();
            for (var i = 0; i < materialCount; i++)
            {
                var materialId = Guid.NewGuid();
                materialIds.Add(materialId);
                var material =
                    (Equipment)ItemFactory.CreateItemUsable(row, materialId, 0, materialLevel);
                if (materialLevel == 0)
                {
                    material.Exp = (long)row.Exp!;
                }
                else
                {
                    material.Exp = _tableSheets.EnhancementCostSheetV3.Values.First(r =>
                        r.Grade == material.Grade && r.ItemSubType == material.ItemSubType &&
                        r.Level == material.level).Exp;
                }

                expectedExpIncrement += material.Exp;
                _avatarState.inventory.AddItem(material, count: 1);
            }

            var result = new CombinationConsumable5.ResultModel()
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
            };
            var preItemUsable = new Equipment((Dictionary)equipment.Serialize());

            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                _avatarState.Update(mail);
            }

            _avatarState.worldInformation.ClearStage(
                1,
                1,
                1,
                _tableSheets.WorldSheet,
                _tableSheets.WorldUnlockSheet
            );

            var slotAddress =
                _avatarAddress.Derive(string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    0
                ));

            Assert.Equal(startLevel, equipment.level);

            _initialState = _initialState
                .SetState(
                    _avatarAddress.Derive(LegacyInventoryKey),
                    _avatarState.inventory.Serialize()
                )
                .SetState(
                    _avatarAddress.Derive(LegacyWorldInformationKey),
                    _avatarState.worldInformation.Serialize()
                )
                .SetState(
                    _avatarAddress.Derive(LegacyQuestListKey),
                    _avatarState.questList.Serialize()
                )
                .SetState(_avatarAddress, _avatarState.SerializeV2());

            var action = new ItemEnhancement()
            {
                itemId = default,
                materialIds = materialIds,
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            var nextState = action.Execute(new ActionContext()
            {
                PreviousState = _initialState,
                Signer = _agentAddress,
                BlockIndex = 1,
                Random = new TestRandom(),
            });

            var slotState = nextState.GetCombinationSlotState(_avatarAddress, 0);
            var resultEquipment = (Equipment)slotState.Result.itemUsable;
            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.Equal(default, resultEquipment.ItemId);
            Assert.Equal(expectedLevel, resultEquipment.level);
            Assert.Equal(startExp + expectedExpIncrement, resultEquipment.Exp);
            Assert.Equal(
                (3_000_000 - expectedCost) * _currency,
                nextState.GetBalance(_agentAddress, _currency)
            );

            var arenaSheet = _tableSheets.ArenaSheet;
            var arenaData = arenaSheet.GetRoundByBlockIndex(1);
            var feeStoreAddress =
                Addresses.GetBlacksmithFeeAddress(arenaData.ChampionshipId, arenaData.Round);
            Assert.Equal(
                expectedCost * _currency,
                nextState.GetBalance(feeStoreAddress, _currency)
            );
            Assert.Equal(30, nextAvatarState.mailBox.Count);

            EnhancementCostSheetV3.Row startRow;

            if (startLevel != 0)
            {
                startRow = _tableSheets.EnhancementCostSheetV3.OrderedList
                    .First(x => x.Grade == 1 && x.Level == startLevel);
            }
            else
            {
                startRow = new EnhancementCostSheetV3.Row();
            }

            var targetRow = _tableSheets.EnhancementCostSheetV3.OrderedList
                .First(x => x.Grade == 1 && x.Level == expectedLevel);
            var stateDict = (Dictionary)nextState.GetState(slotAddress);
            var slot = new CombinationSlotState(stateDict);
            var slotResult = (ItemEnhancement.ResultModel)slot.Result;
            if (startLevel != expectedLevel)
            {
                var baseMinAtk = (decimal)preItemUsable.StatsMap.BaseATK;
                var baseMaxAtk = (decimal)preItemUsable.StatsMap.BaseATK;
                var extraMinAtk = (decimal)preItemUsable.StatsMap.AdditionalATK;
                var extraMaxAtk = (decimal)preItemUsable.StatsMap.AdditionalATK;

                for (var i = startLevel + 1; i <= expectedLevel; i++)
                {
                    var currentRow = _tableSheets.EnhancementCostSheetV3.OrderedList
                        .First(x =>
                            x.Grade == 1 && x.ItemSubType == equipment.ItemSubType && x.Level == i);

                    baseMinAtk *= currentRow.BaseStatGrowthMin.NormalizeFromTenThousandths() + 1;
                    baseMaxAtk *= currentRow.BaseStatGrowthMax.NormalizeFromTenThousandths() + 1;
                    extraMinAtk *= currentRow.ExtraStatGrowthMin.NormalizeFromTenThousandths() + 1;
                    extraMaxAtk *= currentRow.ExtraStatGrowthMax.NormalizeFromTenThousandths() + 1;
                }

                Assert.InRange(
                    resultEquipment.StatsMap.ATK,
                    baseMinAtk + extraMinAtk,
                    baseMaxAtk + extraMaxAtk + 1
                );
            }

            Assert.Equal(
                expectedBlockIndex + 1, // +1 for execution
                resultEquipment.RequiredBlockIndex
            );
            Assert.Equal(preItemUsable.TradableId, slotResult.preItemUsable.TradableId);
            Assert.Equal(preItemUsable.TradableId, resultEquipment.TradableId);
        }
    }
}
