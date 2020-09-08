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
                ["costumes"] = new List(costumes.Select(e => e.Serialize())),
                ["equipments"] = new List(equipments.Select(e => e.Serialize())),
                ["foods"] = new List(foods.Select(e => e.Serialize())),
                ["worldId"] = worldId.Serialize(),
                ["stageId"] = stageId.Serialize(),
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["weeklyArenaAddress"] = WeeklyArenaAddress.Serialize(),
            }.ToImmutableDictionary();


        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            costumes = ((List) plainValue["costumes"]).Select(
                e => e.ToInteger()
            ).ToList();
            equipments = ((List) plainValue["equipments"]).Select(
                e => e.ToGuid()
            ).ToList();
            foods = ((List) plainValue["foods"]).Select(
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
                // FIXME we should create dedicated type for this exception.
                throw new InvalidOperationException(
                    "Aborted as the avatar state of the signer was failed to load."
                );
            }

            sw.Stop();
            Log.Debug("HAS Get AgentAvatarStates: {Elapsed}", sw.Elapsed);

            sw.Restart();

            // worldId와 stageId가 유효한지 확인합니다.
            var worldSheet = states.GetSheet<WorldSheet>();

            if (!worldSheet.TryGetValue(worldId, out var worldRow, false))
            {
                // FIXME we should create dedicated type for this exception.
                throw new InvalidOperationException(
                    $"Not fount {worldId} in TableSheets.WorldSheet."
                );
            }

            if (stageId < worldRow.StageBegin ||
                stageId > worldRow.StageEnd)
            {
                // FIXME we should create dedicated type for this exception.
                throw new InvalidOperationException(
                    $"{worldId} world is not contains {worldRow.Id} stage: " +
                    $"{worldRow.StageBegin}-{worldRow.StageEnd}"
                );
            }

            var stageSheet = states.GetSheet<StageSheet>();
            if (!stageSheet.TryGetValue(stageId, out var stageRow))
            {
                // FIXME we should create dedicated type for this exception.
                throw new InvalidOperationException(
                    $"Not fount stage id in TableSheets.StageSheet: {stageId}"
                );
            }

            var worldInformation = avatarState.worldInformation;
            if (!worldInformation.TryGetWorld(worldId, out var world))
            {
                // NOTE: 이 경우는 아바타 생성 시에는 WorldSheet에 없던 worldId가 새로 추가된 경우로 볼 수 있습니다.
                if (!worldInformation.TryAddWorld(worldRow, out world))
                {
                    // FIXME we should create dedicated type for this exception.
                    throw new InvalidOperationException(
                        $"Failed to add {worldId} world to WorldInformation."
                    );
                }
            }

            if (!world.IsUnlocked)
            {
                // FIXME we should create dedicated type for this exception.
                throw new InvalidOperationException(
                    $"Aborted as the world {worldId} is locked."
                );
            }

            if (world.StageBegin != worldRow.StageBegin ||
                world.StageEnd != worldRow.StageEnd)
            {
                // NOTE: 이 경우는 아바타 생성 이후에 worldId가 포함하는 stageId의 범위가 바뀐 경우로 볼 수 있습니다.
                if (!worldInformation.TryUpdateWorld(worldRow, out world))
                {
                    // FIXME we should create dedicated type for this exception.
                    throw new InvalidOperationException(
                        $"Failed to update {worldId} world in WorldInformation."
                    );
                }

                if (world.StageBegin != worldRow.StageBegin ||
                    world.StageEnd != worldRow.StageEnd)
                {
                    // FIXME we should create dedicated type for this exception.
                    throw new InvalidOperationException(
                        $"Failed to update {worldId} world in WorldInformation."
                    );
                }
            }

            if (world.IsStageCleared && stageId > world.StageClearedId + 1 ||
                !world.IsStageCleared && stageId != world.StageBegin)
            {
                // FIXME we should create dedicated type for this exception.
                throw new InvalidOperationException(
                    $"Aborted as the stage ({worldId}/{stageId}) is not cleared; " +
                    $"cleared stage: {world.StageClearedId}"
                );
            }

            // 장비가 유효한지 검사한다.
            if (!avatarState.ValidateEquipments(equipments, context.BlockIndex))
            {
                // FIXME we should create dedicated type for this exception.
                throw new InvalidOperationException("Aborted as the equipment is invalid.");
            }

            sw.Restart();
            if (avatarState.actionPoint < stageRow.CostAP)
            {
                // FIXME we should create dedicated type for this exception.
                throw new InvalidOperationException(
                    $"Aborted due to insufficient action point: " +
                    $"{avatarState.actionPoint} < {stageRow.CostAP}"
                );
            }

            avatarState.actionPoint -= stageRow.CostAP;

            avatarState.EquipCostumes(costumes);

            avatarState.EquipEquipments(equipments);
            sw.Stop();
            Log.Debug("HAS Unequip items: {Elapsed}", sw.Elapsed);

            sw.Restart();
            var characterSheet = states.GetSheet<CharacterSheet>();
            var simulator = new StageSimulator(
                ctx.Random,
                avatarState,
                foods,
                worldId,
                stageId,
                states.GetStageSimulatorSheets()
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
            Log.Debug("HAS ClearStage: {Elapsed}", sw.Elapsed);

            sw.Restart();
            avatarState.Update(simulator);

            var materialSheet = states.GetSheet<MaterialItemSheet>();
            avatarState.UpdateQuestRewards(materialSheet);

            avatarState.updatedAt = DateTimeOffset.UtcNow;
            states = states.SetState(avatarAddress, avatarState.Serialize());

            sw.Stop();
            Log.Debug("HAS Set AvatarState: {Elapsed}", sw.Elapsed);

            sw.Restart();
            if (simulator.Log.IsClear &&
                states.TryGetState(RankingState.Address, out Dictionary d))
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
            if (simulator.Log.stageId >= GameConfig.RequireClearedStageLevel.ActionsInRankingBoard &&
                simulator.Log.IsClear &&
                states.TryGetState(WeeklyArenaAddress, out Dictionary weeklyDict))
            {
                var weekly = new WeeklyArenaState(weeklyDict);
                if (!weekly.Ended)
                {
                    if (weekly.ContainsKey(avatarAddress))
                    {
                        var info = weekly[avatarAddress];
                        info.Update(avatarState, characterSheet);
                        weekly.Update(info);
                    }
                    else
                    {
                        weekly.Set(avatarState, characterSheet);
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
