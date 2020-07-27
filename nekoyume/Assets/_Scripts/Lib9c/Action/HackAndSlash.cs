using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
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


        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
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

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out AgentState agentState,
                out AvatarState avatarState))
            {
                return LogError(context, "Aborted as the avatar state of the signer was failed to load.");
            }
            sw.Stop();
            Log.Debug("HAS Get AgentAvatarStates: {Elapsed}", sw.Elapsed);

            var worldInformation = avatarState.worldInformation;

            if (!worldInformation.TryGetWorld(worldId, out var world))
            {
                return LogError(context, "Aborted as the world {WorldId} is locked.", worldId);
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

            sw.Restart();

            // 장비가 유효한지 검사한다.
            if (!avatarState.ValidateEquipments(equipments, context.BlockIndex))
            {
                // 장비가 유효하지 않은 에러.
                return LogError(context, "Aborted as the equipment is invalid.");
            }

            var tableSheetState = TableSheetsState.FromActionContext(ctx);

            sw.Stop();
            Log.Debug("HAS Get TableSheetsState: {Elapsed}", sw.Elapsed);
            sw.Restart();

            var tableSheets = TableSheets.FromTableSheetsState(tableSheetState);

            sw.Stop();
            Log.Debug("HAS Initialize TableSheets: {Elapsed}", sw.Elapsed);
            sw.Restart();

            var stage = tableSheets.StageSheet.Values.First(i => i.Id == stageId);
            if (avatarState.actionPoint < stage.CostAP)
            {
                return LogError(
                    context,
                    "Aborted due to insufficient action point: {ActionPointBalance} < {ActionCost}",
                    avatarState.actionPoint,
                    stage.CostAP
                );
            }

            avatarState.actionPoint -= stage.CostAP;

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
            sw.Restart();

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

            if (simulator.Log.IsClear)
            {
                simulator.Player.worldInformation.ClearStage(
                    worldId,
                    stageId,
                    ctx.BlockIndex,
                    tableSheets.WorldUnlockSheet
                );
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
