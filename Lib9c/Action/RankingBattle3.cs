using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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
    [ActionType("ranking_battle3")]
    public class RankingBattle3 : GameAction
    {
        public const int StageId = 999999;
        public static readonly BigInteger EntranceFee = 100;

        public Address AvatarAddress;
        public Address EnemyAddress;
        public Address WeeklyArenaAddress;
        public List<Guid> costumeIds;
        public List<Guid> equipmentIds;
        public List<Guid> consumableIds;
        public BattleLog Result { get; private set; }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.SetState(ctx.Signer, MarkChanged)
                    .SetState(AvatarAddress, MarkChanged)
                    .SetState(WeeklyArenaAddress, MarkChanged)
                    .SetState(ctx.Signer, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, ctx.Signer, WeeklyArenaAddress);
            }
            
            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug(
                "RankingBattle exec started. costume: ({CostumeIds}), equipment: ({EquipmentIds})",
                string.Join(",", costumeIds),
                string.Join(",", equipmentIds)
            );

            if (AvatarAddress.Equals(EnemyAddress))
            {
                throw new InvalidAddressException("Aborted as the signer tried to battle for themselves.");
            }

            if (!states.TryGetAvatarState(ctx.Signer, AvatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException("Aborted as the avatar state of the signer was failed to load.");
            }
            
            sw.Stop();
            Log.Debug("RankingBattle Get AgentAvatarStates: {Elapsed}", sw.Elapsed);
            sw.Restart();

            var items = equipmentIds.Concat(costumeIds);

            avatarState.ValidateEquipmentsV2(equipmentIds, context.BlockIndex);
            avatarState.ValidateConsumable(consumableIds, context.BlockIndex);
            avatarState.ValidateCostume(costumeIds);

            sw.Stop();
            Log.Debug("RankingBattle Validate Equipments: {Elapsed}", sw.Elapsed);
            sw.Restart();

            avatarState.EquipItems(items);

            sw.Stop();
            Log.Debug("RankingBattle Equip Equipments: {Elapsed}", sw.Elapsed);
            sw.Restart();

            if (!avatarState.worldInformation.TryGetUnlockedWorldByStageClearedBlockIndex(out var world) ||
                world.StageClearedId < GameConfig.RequireClearedStageLevel.ActionsInRankingBoard)
            {
                throw new NotEnoughClearedStageLevelException(
                    GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                    world.StageClearedId);
            }
            
            var enemyAvatarState = states.GetAvatarState(EnemyAddress);
            if (enemyAvatarState is null)
            {
                throw new FailedLoadStateException($"Aborted as the avatar state of the opponent ({EnemyAddress}) was failed to load.");
            }
            
            sw.Stop();
            Log.Debug("RankingBattle Get Enemy AvatarState: {Elapsed}", sw.Elapsed);
            sw.Restart();

            var weeklyArenaState = states.GetWeeklyArenaState(WeeklyArenaAddress);

            sw.Stop();
            Log.Debug("RankingBattle Get WeeklyArenaState ({Address}): {Elapsed}", WeeklyArenaAddress, sw.Elapsed);
            sw.Restart();

            if (weeklyArenaState.Ended)
            {
                throw new WeeklyArenaStateAlreadyEndedException();
            }

            if (!weeklyArenaState.ContainsKey(AvatarAddress))
            {
                throw new WeeklyArenaStateNotContainsAvatarAddressException(AvatarAddress);
            }

            var arenaInfo = weeklyArenaState[AvatarAddress];

            if (arenaInfo.DailyChallengeCount <= 0)
            {
                throw new NotEnoughWeeklyArenaChallengeCountException();
            }

            if (!arenaInfo.Active)
            {
                arenaInfo.Activate();
            }

            if (!weeklyArenaState.ContainsKey(EnemyAddress))
            {
                throw new WeeklyArenaStateNotContainsAvatarAddressException(EnemyAddress);
            }

            Log.Debug(weeklyArenaState.address.ToHex());
            
            sw.Stop();
            Log.Debug("RankingBattle Validate ArenaInfo: {Elapsed}", sw.Elapsed);
            sw.Restart();

            var costumeStatSheet = states.GetSheet<CostumeStatSheet>();

            sw.Stop();
            Log.Debug("RankingBattle Get CostumeStatSheet: {Elapsed}", sw.Elapsed);
            sw.Restart();

            var simulator = new RankingSimulator(
                ctx.Random,
                avatarState,
                enemyAvatarState,
                consumableIds,
                states.GetRankingSimulatorSheets(),
                StageId,
                arenaInfo,
                weeklyArenaState[EnemyAddress],
                costumeStatSheet);
            
            simulator.SimulateV2();

            sw.Stop();
            Log.Debug(
                "RankingBattle Simulate() with equipment:({Equipment}), costume:({Costume}): {Elapsed}",
                string.Join(",", simulator.Player.Equipments.Select(r => r.ItemId)),
                string.Join(",", simulator.Player.Costumes.Select(r => r.ItemId)),
                sw.Elapsed
            );

            Log.Debug(
                "Execute RankingBattle({AvatarAddress}); result: {Result} event count: {EventCount}",
                AvatarAddress,
                simulator.Log.result,
                simulator.Log.Count
            );
            sw.Restart();

            Result = simulator.Log;

            foreach (var itemBase in simulator.Reward.OrderBy(i => i.Id))
            {
                Log.Debug($"RankingBattle Add Reward Item({itemBase.Id}): {{Elapsed}}", sw.Elapsed);
                avatarState.inventory.AddItem(itemBase);
            }

            states = states.SetState(WeeklyArenaAddress, weeklyArenaState.Serialize());

            sw.Stop();
            Log.Debug("RankingBattle Serialize WeeklyArenaState: {Elapsed}", sw.Elapsed);
            sw.Restart();

            states = states.SetState(AvatarAddress, avatarState.Serialize());

            sw.Stop();
            Log.Debug("RankingBattle Serialize AvatarState: {Elapsed}", sw.Elapsed);
            sw.Restart();

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("RankingBattle Total Executed Time: {Elapsed}", ended - started);
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = AvatarAddress.Serialize(),
                ["enemyAddress"] = EnemyAddress.Serialize(),
                ["weeklyArenaAddress"] = WeeklyArenaAddress.Serialize(),
                ["costume_ids"] = new List(costumeIds
                    .OrderBy(element => element)
                    .Select(e => e.Serialize())),
                ["equipment_ids"] = new List(equipmentIds
                    .OrderBy(element => element)
                    .Select(e => e.Serialize())),
                ["consumable_ids"] = new List(consumableIds
                    .OrderBy(element => element)
                    .Select(e => e.Serialize())),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            EnemyAddress = plainValue["enemyAddress"].ToAddress();
            WeeklyArenaAddress = plainValue["weeklyArenaAddress"].ToAddress();
            costumeIds = ((List) plainValue["costume_ids"])
                .Select(e => e.ToGuid())
                .ToList();
            equipmentIds = ((List) plainValue["equipment_ids"])
                .Select(e => e.ToGuid())
                .ToList();
            consumableIds = ((List) plainValue["consumable_ids"])
                .Select(e => e.ToGuid())
                .ToList();
        }
    }
}
