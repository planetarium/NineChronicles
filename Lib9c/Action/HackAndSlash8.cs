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
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100081ObsoleteIndex)]
    [ActionType("hack_and_slash8")]
    public class HackAndSlash8 : GameAction, IHackAndSlashV3
    {
        public List<Guid> costumes;
        public List<Guid> equipments;
        public List<Guid> foods;
        public int worldId;
        public int stageId;
        public Address avatarAddress;
        public Address rankingMapAddress;

        IEnumerable<Guid> IHackAndSlashV3.Costumes => costumes;
        IEnumerable<Guid> IHackAndSlashV3.Equipments => equipments;
        IEnumerable<Guid> IHackAndSlashV3.Foods => foods;
        int IHackAndSlashV3.WorldId => worldId;
        int IHackAndSlashV3.StageId => stageId;
        Address IHackAndSlashV3.AvatarAddress => avatarAddress;
        Address IHackAndSlashV3.RankingMapAddress => rankingMapAddress;

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


        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
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

            CheckObsolete(ActionObsoleteConfig.V100081ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}HAS exec started", addressesHex);

            if (!states.TryGetAvatarStateV2(ctx.Signer, avatarAddress, out AvatarState avatarState, out _))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
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

            var worldInformation = avatarState.worldInformation;
            if (!worldInformation.TryGetWorld(worldId, out var world))
            {
                // NOTE: Add new World from WorldSheet
                worldInformation.AddAndUnlockNewWorld(worldRow, ctx.BlockIndex, worldSheet);
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

            if (worldId == GameConfig.MimisbrunnrWorldId)
            {
                throw new InvalidWorldException($"{addressesHex}{worldId} can't execute HackAndSlash action.");
            }

            avatarState.ValidateEquipmentsV2(equipments, context.BlockIndex);
            avatarState.ValidateConsumable(foods, context.BlockIndex);
            avatarState.ValidateCostume(costumes);

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Validate: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var costumeStatSheet = states.GetSheet<CostumeStatSheet>();
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS get CostumeStatSheet: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (avatarState.actionPoint < stageRow.CostAP)
            {
                throw new NotEnoughActionPointException(
                    $"{addressesHex}Aborted due to insufficient action point: " +
                    $"{avatarState.actionPoint} < {stageRow.CostAP}"
                );
            }

            avatarState.actionPoint -= stageRow.CostAP;

            var items = equipments.Concat(costumes);
            avatarState.EquipItems(items);
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Unequip items: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            // Update QuestList only when QuestSheet.Count is greater than QuestList.Count
            var questList = avatarState.questList;
            var questSheet = states.GetQuestSheet();
            if (questList.Count() < questSheet.Count)
            {
                questList.UpdateListV1(
                    2,
                    questSheet,
                    states.GetSheet<QuestRewardSheet>(),
                    states.GetSheet<QuestItemRewardSheet>(),
                    states.GetSheet<EquipmentItemRecipeSheet>());
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Update QuestList: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var characterSheet = states.GetSheet<CharacterSheet>();
            var simulator = new StageSimulatorV1(
                ctx.Random,
                avatarState,
                foods,
                worldId,
                stageId,
                states.GetStageSimulatorSheetsV1(),
                costumeStatSheet,
                StageSimulatorV1.ConstructorVersionV100025);

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Initialize Simulator: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            simulator.SimulateV4();
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Simulator.SimulateV2(): {Elapsed}", addressesHex, sw.Elapsed);

            Log.Verbose(
                "{AddressesHex}Execute HackAndSlash({AvatarAddress}); worldId: {WorldId}, stageId: {StageId}, result: {Result}, " +
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
                var worldUnlockSheet = states.GetSheet<WorldUnlockSheet>();
                simulator.Player.worldInformation.ClearStage(
                    worldId,
                    stageId,
                    ctx.BlockIndex,
                    worldSheet,
                    worldUnlockSheet
                );
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS ClearStage: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            avatarState.Update(simulator);

            var materialSheet = states.GetSheet<MaterialItemSheet>();
            avatarState.UpdateQuestRewards(materialSheet);

            avatarState.updatedAt = ctx.BlockIndex;
            avatarState.mailBox.CleanUp();
            states = states
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize());

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (simulator.Log.IsClear && states.TryGetState(rankingMapAddress, out Dictionary d))
            {
                var ranking = new RankingMapState(d);
                ranking.Update(avatarState);

                sw.Stop();
                Log.Verbose("{AddressesHex}HAS Update RankingState: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();

                var serialized = ranking.Serialize();

                sw.Stop();
                Log.Verbose("{AddressesHex}HAS Serialize RankingState: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();
                states = states.SetState(rankingMapAddress, serialized);
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Set RankingState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            TimeSpan totalElapsed = DateTimeOffset.UtcNow - started;
            Log.Verbose("{AddressesHex}HAS Total Executed Time: {Elapsed}", addressesHex, totalElapsed);
            return states;
        }
    }
}
