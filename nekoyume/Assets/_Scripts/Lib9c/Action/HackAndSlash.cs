using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("hack_and_slash")]
    public class HackAndSlash : GameAction
    {
        public List<int> costumes;
        public List<Guid> equipments;
        public List<Guid> foods;
        public int worldId;
        public int stageId;
        public Address avatarAddress;
        public Address WeeklyArenaAddress;
        public BattleLog Result { get; private set; }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["costumes"] = new Bencodex.Types.List(costumes.Select(e => e.Serialize())),
                ["equipments"] = new Bencodex.Types.List(equipments.Select(e => e.Serialize())),
                ["foods"] = new Bencodex.Types.List(foods.Select(e => e.Serialize())),
                ["worldId"] = worldId.Serialize(),
                ["stageId"] = stageId.Serialize(),
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["weeklyArenaAddress"] = WeeklyArenaAddress.Serialize(),
            }.ToImmutableDictionary();


        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            costumes = ((Bencodex.Types.List) plainValue["costumes"]).Select(
                e => e.ToInteger()
            ).ToList();
            equipments = ((Bencodex.Types.List) plainValue["equipments"]).Select(
                e => e.ToGuid()
            ).ToList();
            foods = ((Bencodex.Types.List) plainValue["foods"]).Select(
                e => e.ToGuid()
            ).ToList();
            worldId = plainValue["worldId"].ToInteger();
            stageId = plainValue["stageId"].ToInteger();
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            WeeklyArenaAddress = plainValue["weeklyArenaAddress"].ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(RankingState.Address, MarkChanged);
                states = states.SetState(avatarAddress, MarkChanged);
                states = states.SetState(WeeklyArenaAddress, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("HAS exec started.");

            if (!states.TryGetAgentAvatarStates(
                ctx.Signer,
                avatarAddress,
                out AgentState agentState,
                out AvatarState avatarState))
            {
                return LogError(
                    context,
                    "Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Debug("HAS Get AgentAvatarStates: {Elapsed}", sw.Elapsed);

            sw.Restart();
            var tableSheetState = TableSheetsState.FromActionContext(ctx);
            sw.Stop();
            Log.Debug("HAS Get TableSheetsState: {Elapsed}", sw.Elapsed);

            sw.Restart();
            var tableSheets = TableSheets.FromTableSheetsState(tableSheetState);
            sw.Stop();
            Log.Debug("HAS Initialize TableSheets: {Elapsed}", sw.Elapsed);

            // worldId와 stageId가 유효한지 확인합니다.

            if (!tableSheets.WorldSheet.TryGetValue(worldId, out var worldRow))
            {
                return LogError(
                    context,
                    "Not fount {WorldId} in TableSheets.WorldSheet.",
                    worldId
                );
            }

            if (stageId < worldRow.StageBegin ||
                stageId > worldRow.StageEnd)
            {
                return LogError(
                    context,
                    "{WorldId} world is not contains {StageId} stage: {StageBegin}-{StageEnd}",
                    stageId,
                    worldRow.Id,
                    worldRow.StageBegin,
                    worldRow.StageEnd
                );
            }

            if (!tableSheets.StageSheet.TryGetValue(stageId, out var stageRow))
            {
                return LogError(
                    context,
                    "Not fount stage id in TableSheets.StageSheet: {StageId}",
                    stageId
                );
            }

            var worldInformation = avatarState.worldInformation;
            if (!worldInformation.TryGetWorld(worldId, out var world))
            {
                // NOTE: 이 경우는 아바타 생성 시에는 WorldSheet에 없던 worldId가 새로 추가된 경우로 볼 수 있습니다.
                if (!worldInformation.TryAddWorld(worldRow, out world))
                {
                    return LogError(context, "Failed to add {WorldId} world to WorldInformation.", worldId);
                }
            }

            if (!world.IsUnlocked)
            {
                return LogError(context, "Aborted as the world {WorldId} is locked.", worldId);
            }

            if (world.StageBegin != worldRow.StageBegin ||
                world.StageEnd != worldRow.StageEnd)
            {
                // NOTE: 이 경우는 아바타 생성 이후에 worldId가 포함하는 stageId의 범위가 바뀐 경우로 볼 수 있습니다.
                if (!worldInformation.TryUpdateWorld(worldRow, out world))
                {
                    return LogError(context, "Failed to update {WorldId} world in WorldInformation.", worldId);
                }

                if (world.StageBegin != worldRow.StageBegin ||
                    world.StageEnd != worldRow.StageEnd)
                {
                    return LogError(context, "Failed to update {WorldId} world in WorldInformation.", worldId);
                }
            }

            if (world.IsStageCleared && stageId > world.StageClearedId + 1 ||
                !world.IsStageCleared && stageId != world.StageBegin)
            {
                return LogError(
                    context,
                    "Aborted as the stage ({WorldId}/{StageId}) is not cleared; cleared stage: {StageClearedId}",
                    worldId,
                    stageId,
                    world.StageClearedId
                );
            }

            // 장비가 유효한지 검사한다.
            if (!avatarState.ValidateEquipments(equipments, context.BlockIndex))
            {
                // 장비가 유효하지 않은 에러.
                return LogError(context, "Aborted as the equipment is invalid.");
            }

            sw.Restart();
            if (avatarState.actionPoint < stageRow.CostAP)
            {
                return LogError(
                    context,
                    "Aborted due to insufficient action point: {ActionPointBalance} < {ActionCost}",
                    avatarState.actionPoint,
                    stageRow.CostAP
                );
            }

            avatarState.actionPoint -= stageRow.CostAP;

            avatarState.EquipCostumes(costumes);

            avatarState.EquipEquipments(equipments);
            sw.Stop();
            Log.Debug("HAS Unequip items: {Elapsed}", sw.Elapsed);

            sw.Restart();
            var simulator = new StageSimulator(
                ctx.Random,
                avatarState,
                foods,
                worldId,
                stageId,
                tableSheets
            );

            sw.Stop();
            Log.Debug("HAS Initialize Simulator: {Elapsed}", sw.Elapsed);

            sw.Restart();
            simulator.Simulate();
            sw.Stop();
            Log.Debug("HAS Simulator.Simulate(): {Elapsed}", sw.Elapsed);

            Log.Debug(
                "Execute HackAndSlash({AvatarAddress}); worldId: {WorldId}, stageId: {StageId}, result: {Result}, " +
                "clearWave: {ClearWave}, totalWave: {TotalWave}",
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
                try
                {
                    simulator.Player.worldInformation.ClearStage(
                        worldId,
                        stageId,
                        ctx.BlockIndex,
                        tableSheets.WorldSheet,
                        tableSheets.WorldUnlockSheet
                    );
                }
                catch (FailedToUnlockWorldException e)
                {
                    return LogError(context, e.Message);
                }
            }

            sw.Stop();
            Log.Debug("HAS ClearStage: {Elapsed}", sw.Elapsed);

            sw.Restart();
            avatarState.Update(simulator);

            avatarState.UpdateQuestRewards(ctx);

            avatarState.updatedAt = DateTimeOffset.UtcNow;
            states = states.SetState(avatarAddress, avatarState.Serialize());

            sw.Stop();
            Log.Debug("HAS Set AvatarState: {Elapsed}", sw.Elapsed);

            sw.Restart();
            if (states.TryGetState(RankingState.Address, out Dictionary d) && simulator.Log.IsClear)
            {
                var ranking = new RankingState(d);
                ranking.Update(avatarState);

                sw.Stop();
                Log.Debug("HAS Update RankingState: {Elapsed}", sw.Elapsed);
                sw.Restart();

                var serialized = ranking.Serialize();

                sw.Stop();
                Log.Debug("HAS Serialize RankingState: {Elapsed}", sw.Elapsed);
                sw.Restart();
                states = states.SetState(RankingState.Address, serialized);
            }

            sw.Stop();
            Log.Debug("HAS Set RankingState: {Elapsed}", sw.Elapsed);

            sw.Restart();
            if (states.TryGetState(WeeklyArenaAddress, out Dictionary weeklyDict))
            {
                var weekly = new WeeklyArenaState(weeklyDict);
                if (!weekly.Ended)
                {
                    if (weekly.ContainsKey(avatarAddress))
                    {
                        var info = weekly[avatarAddress];
                        info.Update(avatarState, tableSheets.CharacterSheet);
                        weekly.Update(info);
                    }
                    else
                    {
                        weekly.Set(avatarState, tableSheets.CharacterSheet);
                    }

                    sw.Stop();
                    Log.Debug("HAS Update WeeklyArenaState: {Elapsed}", sw.Elapsed);

                    sw.Restart();
                    var weeklySerialized = weekly.Serialize();
                    sw.Stop();
                    Log.Debug("HAS Serialize RankingState: {Elapsed}", sw.Elapsed);

                    states = states.SetState(weekly.address, weeklySerialized);
                }
            }

            Result = simulator.Log;

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("HAS Total Executed Time: {Elapsed}", ended - started);
            return states.SetState(ctx.Signer, agentState.Serialize());
        }
    }
}
