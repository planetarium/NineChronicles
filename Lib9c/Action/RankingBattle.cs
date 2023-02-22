using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c.Action;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Model;
using Nekoyume.Extensions;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/941
    /// Updated at https://github.com/planetarium/lib9c/pull/1135
    /// </summary>
    [Serializable]
    [ActionType("ranking_battle12")]
    public class RankingBattle : GameAction, IRankingBattleV2
    {
        public const int StageId = 999999;

        public Address avatarAddress;
        public Address enemyAddress;
        public Address weeklyArenaAddress;
        public List<Guid> costumeIds;
        public List<Guid> equipmentIds;
        public EnemyPlayerDigest PreviousEnemyPlayerDigest;
        public ArenaInfo PreviousArenaInfo;
        public ArenaInfo PreviousEnemyArenaInfo;

        Address IRankingBattleV2.AvatarAddress => avatarAddress;
        Address IRankingBattleV2.EnemyAddress => enemyAddress;
        Address IRankingBattleV2.WeeklyArenaAddress => weeklyArenaAddress;
        IEnumerable<Guid> IRankingBattleV2.CostumeIds => costumeIds;
        IEnumerable<Guid> IRankingBattleV2.EquipmentIds => equipmentIds;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var ctx = context;
            var states = ctx.PreviousStates;
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            if (ctx.Rehearsal)
            {
                return states
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(weeklyArenaAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress, enemyAddress);

            var arenaSheetAddress = Addresses.GetSheetAddress<ArenaSheet>();
            var arenaSheetState = states.GetState(arenaSheetAddress);
            if (arenaSheetState != null)
            {
                // exception handling for v100240.
                if (context.BlockIndex > 4374126 && context.BlockIndex < 4374162)
                {
                }
                else
                {
                    throw new ActionObsoletedException(nameof(RankingBattle));
                }
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug(
                "{AddressesHex}RankingBattle exec started. costume: ({CostumeIds}), equipment: ({EquipmentIds})",
                addressesHex,
                string.Join(",", costumeIds),
                string.Join(",", equipmentIds)
            );

            if (avatarAddress.Equals(enemyAddress))
            {
                throw new InvalidAddressException(
                    $"{addressesHex}Aborted as the signer tried to battle for themselves.");
            }

            if (!states.TryGetAvatarStateV2(ctx.Signer, avatarAddress, out var avatarState, out var migrationRequired))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            var sheets = states.GetSheetsV100291(
                containRankingSimulatorSheets: true,
                sheetTypes: new[]
                {
                    typeof(CharacterSheet),
                    typeof(CostumeStatSheet),
                });
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Get Sheets: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var equipments = avatarState.ValidateEquipmentsV2(equipmentIds, context.BlockIndex);
            var costumeItemIds = avatarState.ValidateCostume(costumeIds);

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Validate Equipments: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var items = equipmentIds.Concat(costumeIds);
            avatarState.EquipItems(items);
            avatarState.ValidateItemRequirement(
                costumeItemIds.ToList(),
                equipments,
                states.GetSheet<ItemRequirementSheet>(),
                states.GetSheet<EquipmentItemRecipeSheet>(),
                states.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                states.GetSheet<EquipmentItemOptionSheet>(),
                addressesHex);

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Equip Equipments: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (!avatarState.worldInformation.TryGetUnlockedWorldByStageClearedBlockIndex(out var world) ||
                world.StageClearedId < GameConfig.RequireClearedStageLevel.ActionsInRankingBoard)
            {
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                    world.StageClearedId);
            }

            AvatarState enemyAvatarState;
            try
            {
                enemyAvatarState = states.GetAvatarStateV2(enemyAddress);
            }
            // BackWard compatible.
            catch (FailedLoadStateException)
            {
                enemyAvatarState = states.GetAvatarState(enemyAddress);
            }

            if (enemyAvatarState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the opponent ({enemyAddress}) was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Get Enemy AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var costumeStatSheet = sheets.GetSheet<CostumeStatSheet>();
            if (!states.TryGetState(weeklyArenaAddress, out Dictionary rawWeeklyArenaState))
            {
                return states;
            }

            sw.Stop();
            Log.Verbose(
                "{AddressesHex}RankingBattle Get WeeklyArenaState ({Address}): {Elapsed}",
                addressesHex,
                weeklyArenaAddress,
                sw.Elapsed);
            sw.Restart();

            var arenaEnded = rawWeeklyArenaState["ended"].ToBoolean();
            if (arenaEnded)
            {
                throw new WeeklyArenaStateAlreadyEndedException();
            }

            // Run updated model
            var (arenaInfoAddress, previousArenaInfo, isNewArenaInfo) = states.GetArenaInfo(
                weeklyArenaAddress,
                avatarState,
                sheets.GetSheet<CharacterSheet>(),
                sheets.GetSheet<CostumeStatSheet>());
            PreviousArenaInfo = previousArenaInfo;
            var arenaInfo = PreviousArenaInfo.Clone();
            if (arenaInfo.DailyChallengeCount <= 0)
            {
                throw new NotEnoughWeeklyArenaChallengeCountException(
                    addressesHex + NotEnoughWeeklyArenaChallengeCountException.BaseMessage);
            }

            var rankingSheets = sheets.GetRankingSimulatorSheetsV100291();
            var player = new Player(avatarState, rankingSheets);
            PreviousEnemyPlayerDigest = new EnemyPlayerDigest(enemyAvatarState);
            var simulator = new RankingSimulator(
                ctx.Random,
                player,
                PreviousEnemyPlayerDigest,
                new List<Guid>(),
                rankingSheets,
                StageId,
                costumeStatSheet);
            simulator.Simulate();
            var (enemyArenaInfoAddress, previousEnemyArenaInfo, isNewEnemyArenaInfo) = states.GetArenaInfo(
                weeklyArenaAddress,
                enemyAvatarState,
                sheets.GetSheet<CharacterSheet>(),
                sheets.GetSheet<CostumeStatSheet>());
            PreviousEnemyArenaInfo = previousEnemyArenaInfo;
            var enemyArenaInfo = PreviousEnemyArenaInfo.Clone();
            var challengerScoreDelta = arenaInfo.Update(
                enemyArenaInfo,
                simulator.Result,
                ArenaScoreHelper.GetScoreV4);
            var rewards = RewardSelector.Select(
                ctx.Random,
                sheets.GetSheet<WeeklyArenaRewardSheet>(),
                sheets.GetSheet<MaterialItemSheet>(),
                player.Level,
                arenaInfo.GetRewardCount());
            simulator.PostSimulate(rewards, challengerScoreDelta, arenaInfo.Score);

            sw.Stop();
            Log.Verbose(
                "{AddressesHex}RankingBattle Simulate() with equipment:({Equipment}), costume:({Costume}): {Elapsed}",
                addressesHex,
                string.Join(",", simulator.Player.Equipments.Select(r => r.ItemId)),
                string.Join(",", simulator.Player.Costumes.Select(r => r.ItemId)),
                sw.Elapsed
            );

            Log.Verbose(
                "{AddressesHex}Execute RankingBattle({AvatarAddress}); result: {Result} event count: {EventCount}",
                addressesHex,
                avatarAddress,
                simulator.Log.result,
                simulator.Log.Count
            );
            sw.Restart();

            foreach (var itemBase in simulator.Reward.OrderBy(i => i.Id))
            {
                Log.Verbose(
                    "{AddressesHex}RankingBattle Add Reward Item({ItemBaseId}): {Elapsed}",
                    addressesHex,
                    itemBase.Id,
                    sw.Elapsed);
                avatarState.inventory.AddItem(itemBase);
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Serialize WeeklyArenaState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            states = states
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(arenaInfoAddress, arenaInfo.Serialize())
                .SetState(enemyArenaInfoAddress, enemyArenaInfo.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize());

            if (migrationRequired)
            {
                states = states
                    .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                    .SetState(avatarAddress, avatarState.SerializeV2());
            }

            if (isNewArenaInfo || isNewEnemyArenaInfo)
            {
                var addressListAddress = weeklyArenaAddress.Derive("address_list");
                var addressList = states.TryGetState(addressListAddress, out List rawAddressList)
                    ? rawAddressList.ToList(StateExtensions.ToAddress)
                    : new List<Address>();

                if (!addressList.Contains(avatarAddress))
                {
                    addressList.Add(avatarAddress);
                }

                if (!addressList.Contains(enemyAddress))
                {
                    addressList.Add(enemyAddress);
                }

                states = states.SetState(addressListAddress,
                    addressList.Aggregate(List.Empty,
                        (current, address) => current.Add(address.Serialize())));
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Serialize AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}RankingBattle Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["enemyAddress"] = enemyAddress.Serialize(),
                ["weeklyArenaAddress"] = weeklyArenaAddress.Serialize(),
                ["costume_ids"] = new List(costumeIds
                    .OrderBy(element => element)
                    .Select(e => e.Serialize())),
                ["equipment_ids"] = new List(equipmentIds
                    .OrderBy(element => element)
                    .Select(e => e.Serialize())),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            enemyAddress = plainValue["enemyAddress"].ToAddress();
            weeklyArenaAddress = plainValue["weeklyArenaAddress"].ToAddress();
            costumeIds = ((List)plainValue["costume_ids"])
                .Select(e => e.ToGuid())
                .ToList();
            equipmentIds = ((List)plainValue["equipment_ids"])
                .Select(e => e.ToGuid())
                .ToList();
        }
    }
}
