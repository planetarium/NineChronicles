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
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using Skill = Nekoyume.Model.Skill.Skill;
using static Lib9c.SerializeKeys;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Rune;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1495
    /// </summary>
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100360ObsoleteIndex)]
    [ActionType("mimisbrunnr_battle11")]
    public class MimisbrunnrBattle11 : GameAction, IMimisbrunnrBattleV5
    {
        public List<Guid> Costumes;
        public List<Guid> Equipments;
        public List<Guid> Foods;
        public List<RuneSlotInfo> RuneInfos;
        public int WorldId;
        public int StageId;
        public int PlayCount = 1;
        public Address AvatarAddress;

        IEnumerable<Guid> IMimisbrunnrBattleV5.Costumes => Costumes;
        IEnumerable<Guid> IMimisbrunnrBattleV5.Equipments => Equipments;
        IEnumerable<Guid> IMimisbrunnrBattleV5.Foods => Foods;
        IEnumerable<IValue> IMimisbrunnrBattleV5.RuneSlotInfos =>
            RuneInfos.Select(x => x.Serialize());
        int IMimisbrunnrBattleV5.WorldId => WorldId;
        int IMimisbrunnrBattleV5.StageId => StageId;
        int IMimisbrunnrBattleV5.PlayCount => PlayCount;
        Address IMimisbrunnrBattleV5.AvatarAddress => AvatarAddress;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["costumes"] = new List(Costumes.OrderBy(i => i).Select(e => e.Serialize())),
                ["equipments"] = new List(Equipments.OrderBy(i => i).Select(e => e.Serialize())),
                ["foods"] = new List(Foods.OrderBy(i => i).Select(e => e.Serialize())),
                ["r"] = RuneInfos.OrderBy(x => x.SlotIndex).Select(x=> x.Serialize()).Serialize(),
                ["worldId"] = WorldId.Serialize(),
                ["stageId"] = StageId.Serialize(),
                ["playCount"] = PlayCount.Serialize(),
                ["avatarAddress"] = AvatarAddress.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            Costumes = ((List)plainValue["costumes"]).Select(e => e.ToGuid()).ToList();
            Equipments = ((List)plainValue["equipments"]).Select(e => e.ToGuid()).ToList();
            Foods = ((List)plainValue["foods"]).Select(e => e.ToGuid()).ToList();
            RuneInfos = plainValue["r"].ToList(x => new RuneSlotInfo((List)x));
            WorldId = plainValue["worldId"].ToInteger();
            StageId = plainValue["stageId"].ToInteger();
            PlayCount = plainValue["playCount"].ToInteger();
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var inventoryAddress = AvatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = AvatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = AvatarAddress.Derive(LegacyQuestListKey);
            if (context.Rehearsal)
            {
                return states;
            }

            CheckObsolete(ActionObsoleteConfig.V100360ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug(
                "{AddressesHex}Mimisbrunnr exec started",
                addressesHex);

            if (!states.TryGetAvatarStateV2(
                    context.Signer,
                    AvatarAddress,
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
            var sheets = states.GetSheets(
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
                        typeof(RuneListSheet),
                    });
            sw.Stop();
            Log.Verbose(
                "{AddressesHex}Get Sheets: {Elapsed}",
                addressesHex,
                sw.Elapsed);

            sw.Restart();
            var worldSheet = sheets.GetSheet<WorldSheet>();
            if (!worldSheet.TryGetValue(WorldId, out var worldRow, false))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(WorldSheet), WorldId);
            }

            if (StageId < worldRow.StageBegin ||
                StageId > worldRow.StageEnd)
            {
                throw new SheetRowColumnException(
                    $"{addressesHex}{WorldId} world is not contains {worldRow.Id} stage:" +
                    $" {worldRow.StageBegin}-{worldRow.StageEnd}");
            }

            if (!sheets.GetSheet<StageSheet>().TryGetValue(StageId, out var stageRow))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(StageSheet), StageId);
            }

            var worldUnlockSheet = sheets.GetSheet<WorldUnlockSheet>();
            var worldInformation = avatarState.worldInformation;
            if (!worldInformation.TryGetWorld(WorldId, out var world))
            {
                // NOTE: Add new World from WorldSheet
                worldInformation.AddAndUnlockMimisbrunnrWorld(
                    worldRow,
                    context.BlockIndex,
                    worldSheet,
                    worldUnlockSheet);
                if (!worldInformation.TryGetWorld(WorldId, out world))
                {
                    // Do nothing.
                }
            }

            if (!world.IsUnlocked)
            {
                var worldUnlockSheetRow = worldUnlockSheet.OrderedList
                    .FirstOrDefault(row => row.WorldIdToUnlock == WorldId);
                if (!(worldUnlockSheetRow is null) &&
                    worldInformation.IsWorldUnlocked(worldUnlockSheetRow.WorldId) &&
                    worldInformation.IsStageCleared(worldUnlockSheetRow.StageId))
                {
                    worldInformation.UnlockWorld(WorldId, context.BlockIndex, worldSheet);
                    if (!worldInformation.TryGetWorld(WorldId, out world))
                    {
                        // Do nothing.
                    }
                }
            }

            if (!world.IsUnlocked)
            {
                throw new InvalidWorldException($"{addressesHex}{WorldId} is locked.");
            }

            if (world.StageBegin != worldRow.StageBegin ||
                world.StageEnd != worldRow.StageEnd)
            {
                worldInformation.UpdateWorld(worldRow);
            }

            if (world.IsStageCleared && StageId > world.StageClearedId + 1 ||
                !world.IsStageCleared && StageId != world.StageBegin)
            {
                throw new InvalidStageException(
                    $"{addressesHex}Aborted as the stage ({WorldId}/{StageId}) is not" +
                    $" cleared; cleared stage: {world.StageClearedId}"
                );
            }

            sw.Restart();
            var mimisbrunnrSheet = sheets.GetSheet<MimisbrunnrSheet>();
            if (!mimisbrunnrSheet.TryGetValue(StageId, out var mimisbrunnrSheetRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    "MimisbrunnrSheet",
                    StageId);
            }

            foreach (var equipmentId in Equipments)
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

            var equipmentList = avatarState.ValidateEquipmentsV2(Equipments, context.BlockIndex);
            var foodIds = avatarState.ValidateConsumable(Foods, context.BlockIndex);
            var costumeIds = avatarState.ValidateCostume(Costumes);

            sw.Restart();

            if (PlayCount <= 0)
            {
                throw new PlayCountIsZeroException(
                    $"{addressesHex}playCount must be greater than 0." +
                    $" current playCount : {PlayCount}");
            }

            var totalCostActionPoint = stageRow.CostAP * PlayCount;
            if (avatarState.actionPoint < totalCostActionPoint)
            {
                throw new NotEnoughActionPointException(
                    $"{addressesHex}Aborted due to insufficient action point:" +
                    $" current({avatarState.actionPoint}) < required({totalCostActionPoint})"
                );
            }

            var equippableItem = Costumes.Concat(Equipments);
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
            var materialSheet = sheets.GetSheet<MaterialItemSheet>();

            // update rune slot
            if (RuneInfos is null)
            {
                throw new RuneInfosIsEmptyException(
                    $"[{nameof(MimisbrunnrBattle)}] my avatar address : {AvatarAddress}");
            }

            if (RuneInfos.GroupBy(x => x.SlotIndex).Count() != RuneInfos.Count)
            {
                throw new DuplicatedRuneSlotIndexException(
                    $"[{nameof(MimisbrunnrBattle)}] my avatar address : {AvatarAddress}");
            }

            var runeSlotStateAddress = RuneSlotState.DeriveAddress(AvatarAddress, BattleType.Adventure);
            var runeSlotState = states.TryGetState(runeSlotStateAddress, out List rawRuneSlotState)
                ? new RuneSlotState(rawRuneSlotState)
                : new RuneSlotState(BattleType.Adventure);

            if (RuneInfos.Exists(x => x.SlotIndex >= runeSlotState.GetRuneSlot().Count))
            {
                throw new SlotNotFoundException(
                    $"[{nameof(MimisbrunnrBattle)}] my avatar address : {AvatarAddress}");
            }

            var runeStates = new List<RuneState>();
            foreach (var address in RuneInfos.Select(info => RuneState.DeriveAddress(AvatarAddress, info.RuneId)))
            {
                if (states.TryGetState(address, out List rawRuneState))
                {
                    runeStates.Add(new RuneState(rawRuneState));
                }
            }
            var runeListSheet = sheets.GetSheet<RuneListSheet>();
            runeSlotState.UpdateSlotV2(RuneInfos, runeListSheet);
            states = states.SetState(runeSlotStateAddress, runeSlotState.Serialize());

            // update item slot
            var itemSlotStateAddress = ItemSlotState.DeriveAddress(AvatarAddress, BattleType.Adventure);
            var itemSlotState = states.TryGetState(itemSlotStateAddress, out List rawItemSlotState)
                ? new ItemSlotState(rawItemSlotState)
                : new ItemSlotState(BattleType.Adventure);
            itemSlotState.UpdateEquipment(Equipments);
            itemSlotState.UpdateCostumes(Costumes);
            states = states.SetState(itemSlotStateAddress, itemSlotState.Serialize());

            var simulator = new StageSimulator(
                context.Random,
                avatarState,
                Foods,
                runeStates,
                new List<Skill>(),
                WorldId,
                StageId,
                stageRow,
                sheets.GetSheet<StageWaveSheet>()[StageId],
                avatarState.worldInformation.IsStageCleared(StageId),
                0,
                sheets.GetSimulatorSheets(),
                sheets.GetSheet<EnemySkillSheet>(),
                sheets.GetSheet<CostumeStatSheet>(),
                StageSimulator.GetWaveRewards(context.Random, stageRow, materialSheet, PlayCount));
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
                AvatarAddress,
                WorldId,
                StageId,
                simulator.Log.result,
                simulator.Log.clearedWaveNumber,
                simulator.Log.waveCount
            );

            sw.Restart();
            if (simulator.Log.IsClear)
            {
                simulator.Player.worldInformation.ClearStage(
                    WorldId,
                    StageId,
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

            // This conditional logic is same as written in the
            // HackAndSlash("hack_and_slash18") action.
            if (context.BlockIndex < ActionObsoleteConfig.V100310ExecutedBlockIndex)
            {
                var player = simulator.Player;
                foreach (var key in player.monsterMapForBeforeV100310.Keys)
                {
                    player.monsterMap.Add(key, player.monsterMapForBeforeV100310[key]);
                }

                player.monsterMapForBeforeV100310.Clear();

                foreach (var key in player.eventMapForBeforeV100310.Keys)
                {
                    player.eventMap.Add(key, player.eventMapForBeforeV100310[key]);
                }

                player.eventMapForBeforeV100310.Clear();
            }

            avatarState.Update(simulator);
            avatarState.UpdateQuestRewards(materialSheet);

            avatarState.updatedAt = context.BlockIndex;
            avatarState.mailBox.CleanUp();
            states = states
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(AvatarAddress, avatarState.SerializeV2());

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
