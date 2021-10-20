using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(BlockChain.Policy.BlockPolicySource.V100082ObsoleteIndex)]
    [ActionType("mimisbrunnr_battle5")]
    public class MimisbrunnrBattle5 : GameAction
    {
        public List<Guid> costumes;
        public List<Guid> equipments;
        public List<Guid> foods;
        public int worldId;
        public int stageId;
        public Address avatarAddress;
        public Address rankingMapAddress;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["costumes"] = new List(costumes.OrderBy(i => i).Select(e => e.Serialize())),
                ["equipments"] = new List(equipments.OrderBy(i => i).Select(e => e.Serialize())),
                ["foods"] = new List(foods.OrderBy(i => i).Select(e => e.Serialize())),
                ["worldId"] = worldId.Serialize(),
                ["stageId"] = stageId.Serialize(),
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["rankingMapAddress"] = rankingMapAddress.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            costumes =  ((List) plainValue["costumes"]).Select(e => e.ToGuid()).ToList();
            equipments = ((List) plainValue["equipments"]).Select(e => e.ToGuid()).ToList();
            foods = ((List) plainValue["foods"]).Select(e => e.ToGuid()).ToList();
            worldId = plainValue["worldId"].ToInteger();
            stageId = plainValue["stageId"].ToInteger();
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            rankingMapAddress = plainValue["rankingMapAddress"].ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            if (ctx.Rehearsal)
            {
                states = states.SetState(rankingMapAddress, MarkChanged);
                states = states.SetState(avatarAddress, MarkChanged);
                states = states
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }

            CheckObsolete(BlockChain.Policy.BlockPolicySource.V100082ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Mimisbrunnr exec started", addressesHex);

            if (!states.TryGetAvatarStateV2(ctx.Signer, avatarAddress, out AvatarState avatarState))
            {
                throw new FailedLoadStateException("Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}Mimisbrunnr Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (avatarState.RankingMapAddress != rankingMapAddress)
            {
                throw new InvalidAddressException($"{addressesHex}Invalid ranking map address");
            }

            var worldSheet = states.GetSheet<WorldSheet>();
            if (!worldSheet.TryGetValue(worldId, out var worldRow, false))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(WorldSheet), worldId);
            }

            if (stageId < worldRow.StageBegin ||
                stageId > worldRow.StageEnd)
            {
                throw new SheetRowColumnException(
                    $"{addressesHex}{worldId} world is not contains {worldRow.Id} stage: " +
                    $"{worldRow.StageBegin}-{worldRow.StageEnd}");
            }

            var stageSheet = states.GetSheet<StageSheet>();
            if (!stageSheet.TryGetValue(stageId, out var stageRow))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(StageSheet), stageId);
            }

            var worldUnlockSheet = states.GetSheet<WorldUnlockSheet>();
            var worldInformation = avatarState.worldInformation;
            if (!worldInformation.TryGetWorld(worldId, out var world))
            {
                // NOTE: Add new World from WorldSheet
                worldInformation.AddAndUnlockMimisbrunnrWorld(worldRow, ctx.BlockIndex, worldSheet, worldUnlockSheet);
                if (!worldInformation.TryGetWorld(worldId, out world))
                {
                    // Do nothing.
                }
            }

            if (!world.IsUnlocked)
            {
                var worldUnlockSheetRow = worldUnlockSheet.OrderedList.FirstOrDefault(row => row.WorldIdToUnlock == worldId);
                if (!(worldUnlockSheetRow is null) &&
                    worldInformation.IsWorldUnlocked(worldUnlockSheetRow.WorldId) &&
                    worldInformation.IsStageCleared(worldUnlockSheetRow.StageId))
                {
                    worldInformation.UnlockWorld(worldId, ctx.BlockIndex, worldSheet);
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
                    $"{addressesHex}Aborted as the stage ({worldId}/{stageId}) is not cleared; " +
                    $"cleared stage: {world.StageClearedId}"
                );
            }

            sw.Restart();
            var mimisbrunnrSheet = states.GetSheet<MimisbrunnrSheet>();
            if (!mimisbrunnrSheet.TryGetValue(stageId, out var mimisbrunnrSheetRow))
            {
                throw new SheetRowNotFoundException("MimisbrunnrSheet", addressesHex, stageId);
            }

            foreach (var equipmentId in equipments)
            {
                if (avatarState.inventory.TryGetNonFungibleItem(equipmentId, out ItemUsable itemUsable))
                {
                    var elementalType = ((Equipment) itemUsable).ElementalType;
                    if (!mimisbrunnrSheetRow.ElementalTypes.Exists(x => x == elementalType))
                    {
                        throw new InvalidElementalException(
                            $"{addressesHex}ElementalType of {equipmentId} does not match.");
                    }
                }
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}Mimisbrunnr Check Equipments ElementalType: {Elapsed}", addressesHex, sw.Elapsed);

            avatarState.ValidateEquipmentsV2(equipments, context.BlockIndex);
            avatarState.ValidateConsumable(foods, context.BlockIndex);
            avatarState.ValidateCostume(costumes);

            sw.Restart();
            if (avatarState.actionPoint < stageRow.CostAP)
            {
                throw new NotEnoughActionPointException(
                    $"{addressesHex}Aborted due to insufficient action point: " +
                    $"{avatarState.actionPoint} < {stageRow.CostAP}"
                );
            }
            avatarState.actionPoint -= stageRow.CostAP;
            var equippableItem = new List<Guid>();
            equippableItem.AddRange(costumes);
            equippableItem.AddRange(equipments);
            avatarState.EquipItems(equippableItem);
            sw.Stop();
            Log.Verbose("{AddressesHex}Mimisbrunnr Unequip items: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            var costumeStatSheet = states.GetSheet<CostumeStatSheet>();
            var simulator = new StageSimulator(
                ctx.Random,
                avatarState,
                foods,
                worldId,
                stageId,
                states.GetStageSimulatorSheets(),
                costumeStatSheet,
                StageSimulator.ConstructorVersionV100025);
            sw.Stop();
            Log.Verbose("{AddressesHex}Mimisbrunnr Initialize Simulator: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            simulator.Simulate5();
            sw.Stop();
            Log.Verbose("{AddressesHex}Mimisbrunnr Simulator.Simulate(): {Elapsed}", addressesHex, sw.Elapsed);

            Log.Verbose(
                "{AddressesHex}Execute Mimisbrunnr({AvatarAddress}); worldId: {WorldId}, stageId: {StageId}, result: {Result}, " +
                "clearWave: {ClearWave}, totalWave: {TotalWave}",
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
                    ctx.BlockIndex,
                    worldSheet,
                    worldUnlockSheet
                );
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}Mimisbrunnr ClearStage: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            avatarState.Update(simulator);

            var materialSheet = states.GetSheet<MaterialItemSheet>();
            avatarState.UpdateQuestRewards(materialSheet);

            avatarState.updatedAt = ctx.BlockIndex;
            avatarState.mailBox.CleanUp();
            states = states
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2());

            sw.Stop();
            Log.Verbose("{AddressesHex}Mimisbrunnr Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            if (simulator.Log.IsClear && states.TryGetState(rankingMapAddress, out Dictionary d))
            {
                var ranking = new RankingMapState(d);
                ranking.Update(avatarState);

                sw.Stop();
                Log.Verbose("{AddressesHex}Mimisbrunnr Update RankingState: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();

                var serialized = ranking.Serialize();

                sw.Stop();
                Log.Verbose("{AddressesHex}Mimisbrunnr Serialize RankingState: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();
                states = states.SetState(rankingMapAddress, serialized);
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}Mimisbrunnr Set RankingState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Mimisbrunnr Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states;
        }
    }
}
