using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
#if UNITY_EDITOR || UNITY_STANDALONE
using TentuPlay.Api;
# endif

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("hack_and_slash")]
    public class HackAndSlash : GameAction
    {
        public List<Equipment> equipments;
        public List<Consumable> foods;
        public int worldId;
        public int stageId;
        public Address avatarAddress;
        public Address WeeklyArenaAddress;
        public BattleLog Result { get; private set; }
        public List<int> completedQuestIds;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["equipments"] = new Bencodex.Types.List(equipments.Select(e => e.Serialize())),
                ["foods"] = new Bencodex.Types.List(foods.Select(e => e.Serialize())),
                ["worldId"] = (Integer) worldId,
                ["stageId"] = (Integer) stageId,
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["weeklyArenaAddress"] = WeeklyArenaAddress.Serialize(),
            }.ToImmutableDictionary();


        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            equipments = ((Bencodex.Types.List) plainValue["equipments"]).Select(
                e => (Equipment) ItemFactory.Deserialize((Bencodex.Types.Dictionary) e)
            ).ToList();
            foods = ((Bencodex.Types.List) plainValue["foods"]).Select(
                e => (Consumable) ItemFactory.Deserialize((Bencodex.Types.Dictionary) e)
            ).ToList();
            worldId = (int) ((Integer) plainValue["worldId"]).Value;
            stageId = (int) ((Integer) plainValue["stageId"]).Value;
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
                return states;
            }
            sw.Stop();
            Log.Debug($"HAS Get AgentAvatarStates: {sw.Elapsed}");

            var worldInformation = avatarState.worldInformation;

            if (!worldInformation.TryGetWorld(worldId, out var world))
            {
                throw new ArgumentException(
                    $"{nameof(worldId)}({worldId}) not unlocked. If you want to clear, unlock first.");
            }

            if (world.IsStageCleared && stageId > world.StageClearedId + 1 ||
                !world.IsStageCleared && stageId != world.StageBegin)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"{nameof(worldId)}({worldId})-{nameof(stageId)}({stageId}) is too big.");
                sb.AppendLine($"Cleared {nameof(stageId)} is ({world.StageClearedId}).");
                throw new ArgumentException(sb.ToString());
            }

            sw.Restart();
            
            // 장비가 유효한지 검사한다.
            {
                var level = avatarState.level;
                var ringCount = 0;
                var failed = false;
                foreach (var equipment in equipments)
                {
                    if (equipment.RequiredBlockIndex > context.BlockIndex)
                    {
                        failed = true;
                        break;
                    }
                        
                    switch (equipment.Data.ItemSubType)
                    {
                        case ItemSubType.Weapon:
                            failed = level < GameConfig.RequireCharacterLevel.CharacterEquipmentSlotWeapon;
                            break;
                        case ItemSubType.Armor:
                            failed = level < GameConfig.RequireCharacterLevel.CharacterEquipmentSlotArmor;
                            break;
                        case ItemSubType.Belt:
                            failed = level < GameConfig.RequireCharacterLevel.CharacterEquipmentSlotBelt;
                            break;
                        case ItemSubType.Necklace:
                            failed = level < GameConfig.RequireCharacterLevel.CharacterEquipmentSlotNecklace;
                            break;
                        case ItemSubType.Ring:
                            ringCount++;
                            var requireLevel = ringCount == 1
                                ? GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing1
                                : ringCount == 2
                                    ? GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing2
                                    : int.MaxValue;
                            failed = level < requireLevel;
                            break;
                        default:
                            failed = true;
                            break;
                    }

                    if (failed)
                        break;
                }

                if (failed)
                {
                    // 장비가 유효하지 않은 에러.
                    return states;
                }
            }

            var tableSheetState = TableSheetsState.FromActionContext(ctx);

            sw.Stop();
            Log.Debug($"HAS Get TableSheetsState: {sw.Elapsed}");
            sw.Restart();

            var tableSheets = TableSheets.FromTableSheetsState(tableSheetState);

            sw.Stop();
            Log.Debug($"HAS Initialize TableSheets: {sw.Elapsed}");
            sw.Restart();

            var stage = tableSheets.StageSheet.Values.First(i => i.Id == stageId);
            if (avatarState.actionPoint < stage.CostAP)
            {
                return states;
            }

            avatarState.actionPoint -= stage.CostAP;

            var inventoryEquipments = avatarState.inventory.Items
                .Select(i => i.item)
                .OfType<Equipment>()
                .Where(i => i.equipped)
                .ToImmutableHashSet();
            foreach (var equipment in inventoryEquipments)
            {
                equipment.Unequip();
            }

            sw.Stop();
            Log.Debug($"HAS Unequip items: {sw.Elapsed}");
            sw.Restart();

            foreach (var equipment in equipments)
            {
                if (!avatarState.inventory.TryGetNonFungibleItem(equipment, out ItemUsable outNonFungibleItem))
                {
                    continue;
                }

                ((Equipment) outNonFungibleItem).Equip();
            }

            var simulator = new StageSimulator(
                ctx.Random, 
                avatarState, 
                foods, 
                worldId, 
                stageId,
                tableSheets
            );

            sw.Stop();
            Log.Debug($"HAS Initialize Simulator: {sw.Elapsed}");
            sw.Restart();

            simulator.Simulate();

            sw.Stop();
            Log.Debug($"HAS Simulator.Simulate(): {sw.Elapsed}");
            sw.Restart();

            Log.Debug($"Execute HackAndSlash({avatarAddress}). " +
                      $"worldId: {worldId}, " +
                      $"stageId: {stageId}, " +
                      $"result: {simulator.Log?.result}, " +
                      $"clearWave: {simulator.Log?.clearedWaveNumber}," +
                      $"totalWave: {simulator.Log?.waveCount}");

            if (simulator.Result == BattleLog.Result.Win && simulator.Log?.clearedWaveNumber == simulator.Log?.waveCount)
            {
                simulator.Player.worldInformation.ClearStage(
                    worldId,
                    stageId,
                    ctx.BlockIndex,
                    tableSheets.WorldUnlockSheet
                );
            }

            sw.Stop();
            Log.Debug($"HAS ClearStage: {sw.Elapsed}");
            sw.Restart();

            avatarState.Update(simulator);

            completedQuestIds = avatarState.UpdateQuestRewards(ctx);

            avatarState.updatedAt = DateTimeOffset.UtcNow;
            states = states.SetState(avatarAddress, avatarState.Serialize());

            sw.Stop();
            Log.Debug($"HAS Set AvatarState: {sw.Elapsed}");
            sw.Restart();
            if (states.TryGetState(RankingState.Address, out Dictionary d) && simulator.Result == BattleLog.Result.Win)
            {
                var ranking = new RankingState(d);
                ranking.Update(avatarState);

                sw.Stop();
                Log.Debug($"HAS Update RankingState: {sw.Elapsed}");
                sw.Restart();

                var serialized = ranking.Serialize();

                sw.Stop();
                Log.Debug($"HAS Serialize RankingState: {sw.Elapsed}");
                sw.Restart();
                states = states.SetState(RankingState.Address, serialized);
            }
            sw.Stop();
            Log.Debug($"HAS Set RankingState : {sw.Elapsed}");
            sw.Restart();

            if (states.TryGetState(WeeklyArenaAddress, out Dictionary weeklyDict))
            {
                var weekly = new WeeklyArenaState(weeklyDict);
                if (weekly.ContainsKey(avatarAddress))
                {
                    var info = weekly[avatarAddress];
                    info.Update(avatarState);
                    weekly.Update(info);
                }
                else
                {
                    weekly.Set(avatarState);
                }
                sw.Stop();
                Log.Debug($"HAS Update WeeklyArenaState: {sw.Elapsed}");
                sw.Restart();

                var weeklySerialized = weekly.Serialize();
                sw.Stop();
                Log.Debug($"HAS Serialize RankingState: {sw.Elapsed}");
                states = states.SetState(weekly.address, weekly.Serialize());
            }

            Result = simulator.Log;

            var ended = DateTimeOffset.UtcNow;
            Log.Debug($"HAS Total Executed Time: {ended - started}");

            return states.SetState(ctx.Signer, agentState.Serialize());
        }
    }
}
