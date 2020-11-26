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
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("hack_and_slash3")]
    public class HackAndSlash3 : GameAction
    {
        public List<int> costumes;
        public List<Guid> equipments;
        public List<Guid> foods;
        public int worldId;
        public int stageId;
        public Address avatarAddress;
        public Address WeeklyArenaAddress;
        public Address RankingMapAddress;
        public BattleLog Result { get; private set; }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["costumes"] = new List(costumes.OrderBy(i => i).Select(e => e.Serialize())),
                ["equipments"] = new List(equipments.OrderBy(i => i).Select(e => e.Serialize())),
                ["foods"] = new List(foods.OrderBy(i => i).Select(e => e.Serialize())),
                ["worldId"] = worldId.Serialize(),
                ["stageId"] = stageId.Serialize(),
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["weeklyArenaAddress"] = WeeklyArenaAddress.Serialize(),
                ["rankingMapAddress"] = RankingMapAddress.Serialize(),
            }.ToImmutableDictionary();


        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            costumes =  ((List) plainValue["costumes"]).Select(e => e.ToInteger()).ToList();
            equipments = ((List) plainValue["equipments"]).Select(e => e.ToGuid()).ToList();
            foods = ((List) plainValue["foods"]).Select(e => e.ToGuid()).ToList();
            worldId = plainValue["worldId"].ToInteger();
            stageId = plainValue["stageId"].ToInteger();
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            WeeklyArenaAddress = plainValue["weeklyArenaAddress"].ToAddress();
            RankingMapAddress = plainValue["rankingMapAddress"].ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(RankingMapAddress, MarkChanged);
                states = states.SetState(avatarAddress, MarkChanged);
                states = states.SetState(WeeklyArenaAddress, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("HAS exec started.");

            if (!states.TryGetAvatarState(ctx.Signer, avatarAddress, out AvatarState avatarState))
            {
                throw new FailedLoadStateException("Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Debug("HAS Get AgentAvatarStates: {Elapsed}", sw.Elapsed);

            sw.Restart();

            if (avatarState.RankingMapAddress != RankingMapAddress)
            {
                throw new InvalidAddressException("Invalid ranking map address");
            }

            // worldId와 stageId가 유효한지 확인합니다.
            var worldSheet = states.GetSheet<WorldSheet>();

            if (!worldSheet.TryGetValue(worldId, out var worldRow, false))
            {
                throw new SheetRowNotFoundException(nameof(WorldSheet), worldId);
            }

            if (stageId < worldRow.StageBegin ||
                stageId > worldRow.StageEnd)
            {
                throw new SheetRowColumnException(
                    $"{worldId} world is not contains {worldRow.Id} stage: " +
                    $"{worldRow.StageBegin}-{worldRow.StageEnd}");
            }

            var stageSheet = states.GetSheet<StageSheet>();
            if (!stageSheet.TryGetValue(stageId, out var stageRow))
            {
                throw new SheetRowNotFoundException(nameof(StageSheet), stageId);
            }

            var worldInformation = avatarState.worldInformation;
            if (!worldInformation.TryGetWorld(worldId, out var world))
            {
                // NOTE: Add new World from WorldSheet
                worldInformation.AddAndUnlockNewWorld(worldRow, ctx.BlockIndex, worldSheet);
            }

            if (!world.IsUnlocked)
            {
                throw new InvalidWorldException($"{worldId} is locked.");
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
                    $"Aborted as the stage ({worldId}/{stageId}) is not cleared; " +
                    $"cleared stage: {world.StageClearedId}"
                );
            }

            avatarState.ValidateEquipments(equipments, context.BlockIndex);
            avatarState.ValidateConsumable(foods, context.BlockIndex);
            avatarState.ValidateCostume(new HashSet<int>(costumes));

            var costumeStatSheet = states.GetSheet<CostumeStatSheet>();

            sw.Restart();
            if (avatarState.actionPoint < stageRow.CostAP)
            {
                throw new NotEnoughActionPointException(
                    $"Aborted due to insufficient action point: " +
                    $"{avatarState.actionPoint} < {stageRow.CostAP}"
                );
            }

            avatarState.actionPoint -= stageRow.CostAP;

            avatarState.EquipCostumes(new HashSet<int>(costumes));

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
                states.GetStageSimulatorSheets(),
                costumeStatSheet
            );

            sw.Stop();
            Log.Debug("HAS Initialize Simulator: {Elapsed}", sw.Elapsed);

            sw.Restart();
            if (ctx.BlockIndex >= StageSimulator.StartBlockIndexToUseSimulatorV2)
            {
                simulator.SimulateV2();
            }
            else
            {
                simulator.Simulate();
            }
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

            avatarState.updatedAt = ctx.BlockIndex;
            avatarState.mailBox.CleanUp();
            states = states.SetState(avatarAddress, avatarState.Serialize());

            sw.Stop();
            Log.Debug("HAS Set AvatarState: {Elapsed}", sw.Elapsed);

            sw.Restart();
            if (states.TryGetState(RankingMapAddress, out Dictionary d) && simulator.Log.IsClear)
            {
                var ranking = new RankingMapState(d);
                ranking.Update(avatarState);

                sw.Stop();
                Log.Debug("HAS Update RankingState: {Elapsed}", sw.Elapsed);
                sw.Restart();

                var serialized = ranking.Serialize();

                sw.Stop();
                Log.Debug("HAS Serialize RankingState: {Elapsed}", sw.Elapsed);
                sw.Restart();
                states = states.SetState(RankingMapAddress, serialized);
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
                        info.Update(avatarState, characterSheet, costumeStatSheet);
                        weekly.Update(info);
                    }
                    else
                    {
                        weekly.SetV2(avatarState, characterSheet, costumeStatSheet);
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
            return states;
        }
    }
}
