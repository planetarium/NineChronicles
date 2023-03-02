using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1241
    /// Updated at https://github.com/planetarium/lib9c/pull/1244
    /// </summary>
    [Serializable]
    [ActionType("mimisbrunnr_battle10")]
    public class MimisbrunnrBattle10 : GameAction, IMimisbrunnrBattleV4
    {
        public List<Guid> costumes;
        public List<Guid> equipments;
        public List<Guid> foods;
        public int worldId;
        public int stageId;
        public int playCount = 1;
        public Address avatarAddress;

        IEnumerable<Guid> IMimisbrunnrBattleV4.Costumes => costumes;
        IEnumerable<Guid> IMimisbrunnrBattleV4.Equipments => equipments;
        IEnumerable<Guid> IMimisbrunnrBattleV4.Foods => foods;
        int IMimisbrunnrBattleV4.WorldId => worldId;
        int IMimisbrunnrBattleV4.StageId => stageId;
        int IMimisbrunnrBattleV4.PlayCount => playCount;
        Address IMimisbrunnrBattleV4.AvatarAddress => avatarAddress;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["costumes"] = new List(costumes.OrderBy(i => i).Select(e => e.Serialize())),
                ["equipments"] = new List(equipments.OrderBy(i => i).Select(e => e.Serialize())),
                ["foods"] = new List(foods.OrderBy(i => i).Select(e => e.Serialize())),
                ["worldId"] = worldId.Serialize(),
                ["stageId"] = stageId.Serialize(),
                ["playCount"] = playCount.Serialize(),
                ["avatarAddress"] = avatarAddress.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            costumes = ((List)plainValue["costumes"]).Select(e => e.ToGuid()).ToList();
            equipments = ((List)plainValue["equipments"]).Select(e => e.ToGuid()).ToList();
            foods = ((List)plainValue["foods"]).Select(e => e.ToGuid()).ToList();
            worldId = plainValue["worldId"].ToInteger();
            stageId = plainValue["stageId"].ToInteger();
            playCount = plainValue["playCount"].ToInteger();
            avatarAddress = plainValue["avatarAddress"].ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            if (context.Rehearsal)
            {
                return states;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug(
                "{AddressesHex}Mimisbrunnr exec started",
                addressesHex);

            if (!states.TryGetAvatarStateV2(
                    context.Signer,
                    avatarAddress,
                    out var avatarState,
                    out _))
            {
                throw new FailedLoadStateException(
                    "Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Verbose(
                "{AddressesHex}Mimisbrunnr Get AgentAvatarStates: {Elapsed}",
                addressesHex,
                sw.Elapsed);

            sw.Restart();

            // FIXME Delete this check next hard fork.
            bool useV100291Sheets = UseV100291Sheets(context.BlockIndex);
            var sheets = useV100291Sheets
                ? states.GetSheetsV100291(
                    containSimulatorSheets: true,
                    sheetTypes: new[]
                    {
                        typeof(WorldSheet),
                        typeof(StageSheet),
                        typeof(StageWaveSheet),
                        typeof(EnemySkillSheet),
                        typeof(CostumeStatSheet),
                        typeof(WorldUnlockSheet),
                        typeof(MimisbrunnrSheet),
                        typeof(ItemRequirementSheet),
                        typeof(EquipmentItemRecipeSheet),
                        typeof(EquipmentItemSubRecipeSheetV2),
                        typeof(EquipmentItemOptionSheet),
                        typeof(MaterialItemSheet),
                    })
                : states.GetSheetsV1(
                    containSimulatorSheets: true,
                    sheetTypes: new[]
                    {
                        typeof(WorldSheet),
                        typeof(StageSheet),
                        typeof(StageWaveSheet),
                        typeof(EnemySkillSheet),
                        typeof(CostumeStatSheet),
                        typeof(WorldUnlockSheet),
                        typeof(MimisbrunnrSheet),
                        typeof(ItemRequirementSheet),
                        typeof(EquipmentItemRecipeSheet),
                        typeof(EquipmentItemSubRecipeSheetV2),
                        typeof(EquipmentItemOptionSheet),
                        typeof(MaterialItemSheet),
                    });
            sw.Stop();
            Log.Verbose(
                "{AddressesHex}Get Sheets: {Elapsed}",
                addressesHex,
                sw.Elapsed);

            sw.Restart();
            var worldSheet = sheets.GetSheet<WorldSheet>();
            if (!worldSheet.TryGetValue(worldId, out var worldRow, false))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(WorldSheet), worldId);
            }

            if (stageId < worldRow.StageBegin ||
                stageId > worldRow.StageEnd)
            {
                throw new SheetRowColumnException(
                    $"{addressesHex}{worldId} world is not contains {worldRow.Id} stage:" +
                    $" {worldRow.StageBegin}-{worldRow.StageEnd}");
            }

            if (!sheets.GetSheet<StageSheet>().TryGetValue(stageId, out var stageRow))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(StageSheet), stageId);
            }

            var worldUnlockSheet = sheets.GetSheet<WorldUnlockSheet>();
            var worldInformation = avatarState.worldInformation;
            if (!worldInformation.TryGetWorld(worldId, out var world))
            {
                // NOTE: Add new World from WorldSheet
                worldInformation.AddAndUnlockMimisbrunnrWorld(
                    worldRow,
                    context.BlockIndex,
                    worldSheet,
                    worldUnlockSheet);
                if (!worldInformation.TryGetWorld(worldId, out world))
                {
                    // Do nothing.
                }
            }

            if (!world.IsUnlocked)
            {
                var worldUnlockSheetRow = worldUnlockSheet.OrderedList
                    .FirstOrDefault(row => row.WorldIdToUnlock == worldId);
                if (!(worldUnlockSheetRow is null) &&
                    worldInformation.IsWorldUnlocked(worldUnlockSheetRow.WorldId) &&
                    worldInformation.IsStageCleared(worldUnlockSheetRow.StageId))
                {
                    worldInformation.UnlockWorld(worldId, context.BlockIndex, worldSheet);
                    if (!worldInformation.TryGetWorld(worldId, out world))
                    {
                        // Do nothing.
                    }
                }
            }

            if (!world.IsUnlocked)
            {
                throw new InvalidWorldException($"{addressesHex}{worldId} is locked.");
            }

            if (world.StageBegin != worldRow.StageBegin ||
                world.StageEnd != worldRow.StageEnd)
            {
                worldInformation.UpdateWorld(worldRow);
            }

            if (world.IsStageCleared && stageId > world.StageClearedId + 1 ||
                !world.IsStageCleared && stageId != world.StageBegin)
            {
                throw new InvalidStageException(
                    $"{addressesHex}Aborted as the stage ({worldId}/{stageId}) is not" +
                    $" cleared; cleared stage: {world.StageClearedId}"
                );
            }

            sw.Restart();
            var mimisbrunnrSheet = sheets.GetSheet<MimisbrunnrSheet>();
            if (!mimisbrunnrSheet.TryGetValue(stageId, out var mimisbrunnrSheetRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    "MimisbrunnrSheet",
                    stageId);
            }

            foreach (var equipmentId in equipments)
            {
                if (!avatarState.inventory.TryGetNonFungibleItem(
                        equipmentId,
                        out ItemUsable itemUsable))
                {
                    continue;
                }

                var elementalType = ((Equipment)itemUsable).ElementalType;
                if (!mimisbrunnrSheetRow.ElementalTypes.Exists(x =>
                        x == elementalType))
                {
                    throw new InvalidElementalException(
                        $"{addressesHex}ElementalType of {equipmentId} does not match.");
                }
            }

            sw.Stop();
            Log.Verbose(
                "{AddressesHex}Mimisbrunnr Check Equipments ElementalType: {Elapsed}",
                addressesHex,
                sw.Elapsed);

            var equipmentList = avatarState.ValidateEquipmentsV2(equipments, context.BlockIndex);
            var foodIds = avatarState.ValidateConsumable(foods, context.BlockIndex);
            var costumeIds = avatarState.ValidateCostume(costumes);

            sw.Restart();

            if (playCount <= 0)
            {
                throw new PlayCountIsZeroException(
                    $"{addressesHex}playCount must be greater than 0." +
                    $" current playCount : {playCount}");
            }

            var totalCostActionPoint = stageRow.CostAP * playCount;
            if (avatarState.actionPoint < totalCostActionPoint)
            {
                throw new NotEnoughActionPointException(
                    $"{addressesHex}Aborted due to insufficient action point:" +
                    $" current({avatarState.actionPoint}) < required({totalCostActionPoint})"
                );
            }

            var equippableItem = costumes.Concat(equipments);
            avatarState.EquipItems(equippableItem);
            var requirementSheet = sheets.GetSheet<ItemRequirementSheet>();
            avatarState.ValidateItemRequirement(
                costumeIds.Concat(foodIds).ToList(),
                equipmentList,
                requirementSheet,
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                addressesHex);

            avatarState.actionPoint -= totalCostActionPoint;
            sw.Stop();
            Log.Verbose(
                "{AddressesHex}Mimisbrunnr Unequip items: {Elapsed}",
                addressesHex,
                sw.Elapsed);

            sw.Restart();
            var simulatorSheets = useV100291Sheets
                ? sheets.GetSimulatorSheetsV100291()
                : sheets.GetSimulatorSheetsV1();
            var materialSheet = sheets.GetSheet<MaterialItemSheet>();
            var simulator = new StageSimulatorV2(
                context.Random,
                avatarState,
                foods,
                new List<Skill>(),
                worldId,
                stageId,
                stageRow,
                sheets.GetSheet<StageWaveSheet>()[stageId],
                avatarState.worldInformation.IsStageCleared(stageId),
                0,
                simulatorSheets,
                sheets.GetSheet<EnemySkillSheet>(),
                sheets.GetSheet<CostumeStatSheet>(),
                StageSimulatorV2.GetWaveRewards(context.Random, stageRow, materialSheet, playCount));
            sw.Stop();
            Log.Verbose(
                "{AddressesHex}Mimisbrunnr Initialize Simulator: {Elapsed}",
                addressesHex,
                sw.Elapsed);

            sw.Restart();
            simulator.Simulate();
            sw.Stop();
            Log.Verbose(
                "{AddressesHex}Mimisbrunnr Simulator.Simulate(): {Elapsed}",
                addressesHex,
                sw.Elapsed);

            Log.Verbose(
                "{AddressesHex}Execute Mimisbrunnr({AvatarAddress});" +
                " worldId: {WorldId}, stageId: {StageId}, result: {Result}," +
                " clearWave: {ClearWave}, totalWave: {TotalWave}",
                addressesHex,
                avatarAddress,
                worldId,
                stageId,
                simulator.Log.result,
                simulator.Log.clearedWaveNumber,
                simulator.Log.waveCount
            );

            sw.Restart();
            if (simulator.Log.IsClear)
            {
                simulator.Player.worldInformation.ClearStage(
                    worldId,
                    stageId,
                    context.BlockIndex,
                    worldSheet,
                    worldUnlockSheet
                );
            }

            sw.Stop();
            Log.Verbose(
                "{AddressesHex}Mimisbrunnr ClearStage: {Elapsed}",
                addressesHex,
                sw.Elapsed);
            sw.Restart();

            avatarState.Update(simulator);
            avatarState.UpdateQuestRewards(materialSheet);

            avatarState.updatedAt = context.BlockIndex;
            avatarState.mailBox.CleanUp();
            states = states
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2());

            sw.Stop();
            Log.Verbose(
                "{AddressesHex}Mimisbrunnr Set AvatarState: {Elapsed}",
                addressesHex,
                sw.Elapsed);
            sw.Restart();

            var ended = DateTimeOffset.UtcNow;
            Log.Debug(
                "{AddressesHex}Mimisbrunnr Total Executed Time: {Elapsed}",
                addressesHex,
                ended - started);
            return states;
        }
    }
}
