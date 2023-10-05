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
    using Xunit;
    using static SerializeKeys;

    public class ItemEnhancement12Test
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;
        private readonly Currency _currency;
        private IAccount _initialState;

        public ItemEnhancement12Test()
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
        // from 0 to 0 using one level 0 material
        [InlineData(0, false, 0, 0, false, 1)]
        [InlineData(0, false, 0, 0, true, 1)]
        [InlineData(0, true, 0, 0, false, 1)]
        [InlineData(0, true, 0, 0, true, 1)]
        // from 0 to 1 using two level 0 material
        [InlineData(0, false, 1, 0, false, 3)]
        [InlineData(0, false, 1, 0, true, 3)]
        [InlineData(0, true, 1, 0, false, 3)]
        [InlineData(0, true, 1, 0, true, 3)]
        // from 0 to N using multiple level 0 materials
        [InlineData(0, false, 2, 0, false, 7)]
        [InlineData(0, false, 4, 0, false, 31)]
        [InlineData(0, false, 2, 0, true, 7)]
        [InlineData(0, false, 4, 0, true, 31)]
        [InlineData(0, true, 2, 0, false, 7)]
        [InlineData(0, true, 4, 0, false, 31)]
        [InlineData(0, true, 2, 0, true, 7)]
        [InlineData(0, true, 4, 0, true, 31)]
        // from K to K with material(s). Check requiredBlock == 0
        [InlineData(10, false, 10, 0, false, 1)]
        [InlineData(10, false, 10, 0, true, 1)]
        [InlineData(10, true, 10, 0, false, 1)]
        [InlineData(10, true, 10, 0, true, 1)]
        // from K to N using one level X material
        [InlineData(5, false, 6, 6, false, 1)]
        [InlineData(5, false, 6, 6, true, 1)]
        [InlineData(5, true, 6, 6, false, 1)]
        [InlineData(5, true, 6, 6, true, 1)]
        // from K to N using multiple materials
        [InlineData(5, false, 7, 4, false, 6)]
        [InlineData(5, false, 9, 7, false, 5)]
        [InlineData(5, false, 7, 4, true, 6)]
        [InlineData(5, false, 9, 7, true, 5)]
        [InlineData(5, true, 7, 4, false, 6)]
        [InlineData(5, true, 9, 7, false, 5)]
        [InlineData(5, true, 7, 4, true, 6)]
        [InlineData(5, true, 9, 7, true, 5)]
        // from 20 to 21 (just to reach level 21 exp)
        [InlineData(20, false, 21, 20, false, 1)]
        [InlineData(20, false, 21, 20, true, 1)]
        [InlineData(20, true, 21, 20, false, 1)]
        [InlineData(20, true, 21, 20, true, 1)]
        // from 20 to 21 (over level 21)
        [InlineData(20, false, 21, 20, false, 2)]
        [InlineData(20, false, 21, 20, true, 2)]
        [InlineData(20, true, 21, 20, false, 2)]
        [InlineData(20, true, 21, 20, true, 2)]
        // from 21 to 21 (no level up)
        [InlineData(21, false, 21, 1, false, 1)]
        [InlineData(21, false, 21, 21, false, 1)]
        [InlineData(21, false, 21, 1, true, 1)]
        [InlineData(21, false, 21, 21, true, 1)]
        [InlineData(21, true, 21, 1, false, 1)]
        [InlineData(21, true, 21, 21, false, 1)]
        [InlineData(21, true, 21, 1, true, 1)]
        [InlineData(21, true, 21, 21, true, 1)]
        // Test: change of exp, change of level, required block, NCG price
        public void Execute(
            int startLevel,
            bool oldStart,
            int expectedLevel,
            int materialLevel,
            bool oldMaterial,
            int materialCount)
        {
            var row = _tableSheets.EquipmentItemSheet.Values.First(r => r.Id == 10110000);
            var equipment = (Equipment)ItemFactory.CreateItemUsable(row, default, 0, startLevel);
            if (startLevel == 0)
            {
                equipment.Exp = (long)row.Exp!;
            }
            else
            {
                equipment.Exp = _tableSheets.EnhancementCostSheetV3.OrderedList.First(r =>
                    r.ItemSubType == equipment.ItemSubType && r.Grade == equipment.Grade &&
                    r.Level == equipment.level).Exp;
            }

            var startExp = equipment.Exp;
            if (oldStart)
            {
                equipment.Exp = 0L;
            }

            _avatarState.inventory.AddItem(equipment, count: 1);

            var expectedTargetRow = _tableSheets.EnhancementCostSheetV3.OrderedList.FirstOrDefault(
                r =>
                    r.Grade == equipment.Grade && r.ItemSubType == equipment.ItemSubType &&
                    r.Level == expectedLevel);
            var startRow = _tableSheets.EnhancementCostSheetV3.OrderedList.FirstOrDefault(r =>
                r.Grade == equipment.Grade && r.ItemSubType == equipment.ItemSubType &&
                r.Level == startLevel);
            var expectedCost = (expectedTargetRow?.Cost ?? 0) - (startRow?.Cost ?? 0);
            var expectedBlockIndex =
                (expectedTargetRow?.RequiredBlockIndex ?? 0) - (startRow?.RequiredBlockIndex ?? 0);

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
                    material.Exp = _tableSheets.EnhancementCostSheetV3.OrderedList.First(r =>
                        r.ItemSubType == material.ItemSubType && r.Grade == material.Grade &&
                        r.Level == material.level).Exp;
                }

                expectedExpIncrement += material.Exp;
                if (oldMaterial)
                {
                    material.Exp = 0L;
                }

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

            var action = new ItemEnhancement12()
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
                RandomSeed = 0,
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

            var stateDict = (Dictionary)nextState.GetState(slotAddress);
            var slot = new CombinationSlotState(stateDict);
            var slotResult = (ItemEnhancement12.ResultModel)slot.Result;
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
            Assert.Equal(preItemUsable.ItemId, slotResult.preItemUsable.ItemId);
            Assert.Equal(preItemUsable.ItemId, resultEquipment.ItemId);
        }
    }
}
