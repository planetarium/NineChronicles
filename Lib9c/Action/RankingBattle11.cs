using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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
    /// Updated at https://github.com/planetarium/lib9c/pull/1176
    /// </summary>
    [Serializable]
    [ActionType("ranking_battle11")]
    public class RankingBattle11 : GameAction, IRankingBattleV2
    {
        public const int StageId = 999999;
        public static readonly BigInteger EntranceFee = 100;
        // BlockIndex for ArenaInfo separate from WeeklyArenaState.Map.
        // https://github.com/planetarium/lib9c/issues/883
        public const long UpdateTargetBlockIndex = 3_808_000L;
        // WeeklyArenaIndex for ArenaInfo separate from WeeklyArenaState.Map.
        public const int UpdateTargetWeeklyArenaIndex = 68;

        public Address avatarAddress;
        public Address enemyAddress;
        public Address weeklyArenaAddress;
        public List<Guid> costumeIds;
        public List<Guid> equipmentIds;
        public EnemyPlayerDigest EnemyPlayerDigest;
        public ArenaInfo ArenaInfo;
        public ArenaInfo EnemyArenaInfo;

        Address IRankingBattleV2.AvatarAddress => avatarAddress;
        Address IRankingBattleV2.EnemyAddress => enemyAddress;
        Address IRankingBattleV2.WeeklyArenaAddress => weeklyArenaAddress;
        IEnumerable<Guid> IRankingBattleV2.CostumeIds => costumeIds;
        IEnumerable<Guid> IRankingBattleV2.EquipmentIds => equipmentIds;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
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

            // Avoid InvalidBlockStateRootHashException
            if (ctx.BlockIndex == 680341 && Id.Equals(new Guid("df37dbd8-5703-4dff-918b-ad22ee4c34c6")))
            {
                return states;
            }

            var arenaSheetAddress = Addresses.GetSheetAddress<ArenaSheet>();
            var arenaSheetState = states.GetState(arenaSheetAddress);
            if (arenaSheetState != null)
            {
                throw new ActionObsoletedException(nameof(RankingBattle11));
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress, enemyAddress);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose(
                "{AddressesHex}RankingBattle exec started. costume: ({CostumeIds}), equipment: ({EquipmentIds})",
                addressesHex,
                string.Join(",", costumeIds),
                string.Join(",", equipmentIds)
            );

            if (avatarAddress.Equals(enemyAddress))
            {
                throw new InvalidAddressException($"{addressesHex}Aborted as the signer tried to battle for themselves.");
            }

            if (!states.TryGetAvatarStateV2(ctx.Signer, avatarAddress, out var avatarState, out bool migrationRequired))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            var sheets = states.GetSheetsV100291(
                containRankingSimulatorSheets:true,
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
            if (context.BlockIndex > 3806324)
            {
                avatarState.ValidateItemRequirement(
                    costumeItemIds.ToList(),
                    equipments,
                    states.GetSheet<ItemRequirementSheet>(),
                    states.GetSheet<EquipmentItemRecipeSheet>(),
                    states.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                    states.GetSheet<EquipmentItemOptionSheet>(),
                    addressesHex);
            }

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
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the opponent ({enemyAddress}) was failed to load.");
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
            Log.Verbose("{AddressesHex}RankingBattle Get WeeklyArenaState ({Address}): {Elapsed}", addressesHex, weeklyArenaAddress, sw.Elapsed);
            sw.Restart();

            bool arenaEnded = rawWeeklyArenaState["ended"].ToBoolean();
            if (arenaEnded)
            {
                throw new WeeklyArenaStateAlreadyEndedException();
            }

            if (context.BlockIndex >= UpdateTargetBlockIndex)
            {
                // Run updated model
                var arenaInfoAddress = weeklyArenaAddress.Derive(avatarAddress.ToByteArray());
                ArenaInfo arenaInfo;
                var characterSheet = sheets.GetSheet<CharacterSheet>();
                var addressListAddress = weeklyArenaAddress.Derive("address_list");
                bool listCheck = false;
                if (!states.TryGetState(arenaInfoAddress, out Dictionary rawArenaInfo))
                {
                    arenaInfo = new ArenaInfo(avatarState, characterSheet, costumeStatSheet, true);
                    listCheck = true;
                    rawArenaInfo = (Dictionary) arenaInfo.Serialize();
                }
                else
                {
                    arenaInfo = new ArenaInfo(rawArenaInfo);
                }

                var enemyInfoAddress = weeklyArenaAddress.Derive(enemyAddress.ToByteArray());
                ArenaInfo enemyInfo;
                if (!states.TryGetState(enemyInfoAddress, out Dictionary rawEnemyInfo))
                {
                    enemyInfo = new ArenaInfo(enemyAvatarState, characterSheet, costumeStatSheet,
                        true);
                    listCheck = true;
                    rawEnemyInfo = (Dictionary) enemyInfo.Serialize();
                }
                else
                {
                    enemyInfo = new ArenaInfo(rawEnemyInfo);
                }

                if (arenaInfo.DailyChallengeCount <= 0)
                {
                    throw new NotEnoughWeeklyArenaChallengeCountException(
                        addressesHex + NotEnoughWeeklyArenaChallengeCountException.BaseMessage);
                }

                ArenaInfo = new ArenaInfo(rawArenaInfo);
                EnemyArenaInfo = new ArenaInfo(rawEnemyInfo);
                var rankingSheets = sheets.GetRankingSimulatorSheetsV100291();
                var player = new Player(avatarState, rankingSheets);
                var enemyPlayerDigest = new EnemyPlayerDigest(enemyAvatarState);
                var simulator = new RankingSimulatorV1(
                    ctx.Random,
                    player,
                    enemyPlayerDigest,
                    new List<Guid>(),
                    rankingSheets,
                    StageId,
                    arenaInfo,
                    enemyInfo,
                    costumeStatSheet);

                simulator.Simulate();

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
                    .SetState(enemyInfoAddress, enemyInfo.Serialize())
                    .SetState(questListAddress, avatarState.questList.Serialize());

                if (migrationRequired)
                {
                    states = states
                        .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                        .SetState(avatarAddress, avatarState.SerializeV2());
                }

                if (listCheck)
                {
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
                Log.Verbose("{AddressesHex}RankingBattle Total Executed Time: {Elapsed}", addressesHex, ended - started);
                EnemyPlayerDigest = enemyPlayerDigest;
                return states;
            }
            // Run Backward compatible
            return BackwardCompatibleExecute(rawWeeklyArenaState, sheets, avatarState, costumeStatSheet, sw, addressesHex, enemyAvatarState, ctx, states, inventoryAddress, questListAddress, migrationRequired, worldInformationAddress, started);
        }

        private IAccountStateDelta BackwardCompatibleExecute(Dictionary rawWeeklyArenaState, Dictionary<Type, (Address address, ISheet sheet)> sheets,
            AvatarState avatarState, CostumeStatSheet costumeStatSheet, Stopwatch sw, string addressesHex,
            AvatarState enemyAvatarState, IActionContext ctx, IAccountStateDelta states, Address inventoryAddress,
            Address questListAddress, bool migrationRequired, Address worldInformationAddress, DateTimeOffset started)
        {
            Dictionary weeklyArenaMap = (Dictionary) rawWeeklyArenaState["map"];

            IKey arenaKey = (IKey) avatarAddress.Serialize();
            if (!weeklyArenaMap.ContainsKey(arenaKey))
            {
                var characterSheet = sheets.GetSheet<CharacterSheet>();
                var newInfo = new ArenaInfo(avatarState, characterSheet, costumeStatSheet, false);
                weeklyArenaMap =
                    (Dictionary) weeklyArenaMap.Add(arenaKey, newInfo.Serialize());
                sw.Stop();
                Log.Verbose("{AddressesHex}RankingBattle Set AvatarInfo: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();
            }

            var arenaInfo = new ArenaInfo((Dictionary) weeklyArenaMap[arenaKey]);

            if (arenaInfo.DailyChallengeCount <= 0)
            {
                throw new NotEnoughWeeklyArenaChallengeCountException(
                    addressesHex + NotEnoughWeeklyArenaChallengeCountException.BaseMessage);
            }

            if (!arenaInfo.Active)
            {
                arenaInfo.Activate();
            }

            IKey enemyKey = (IKey) enemyAddress.Serialize();
            if (!weeklyArenaMap.ContainsKey(enemyKey))
            {
                throw new WeeklyArenaStateNotContainsAvatarAddressException(addressesHex, enemyAddress);
            }

            var enemyArenaInfo = new ArenaInfo((Dictionary) weeklyArenaMap[enemyKey]);
            if (!enemyArenaInfo.Active)
            {
                enemyArenaInfo.Activate();
            }

            Log.Verbose("{WeeklyArenaStateAddress}", weeklyArenaAddress.ToHex());

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Validate ArenaInfo: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            ArenaInfo = new ArenaInfo((Dictionary) weeklyArenaMap[arenaKey]);
            EnemyArenaInfo = new ArenaInfo((Dictionary) weeklyArenaMap[enemyKey]);
            var rankingSheets = sheets.GetRankingSimulatorSheetsV100291();
            var player = new Player(avatarState, rankingSheets);
            var enemyPlayerDigest = new EnemyPlayerDigest(enemyAvatarState);
            var simulator = new RankingSimulatorV1(
                ctx.Random,
                player,
                enemyPlayerDigest,
                new List<Guid>(),
                rankingSheets,
                StageId,
                arenaInfo,
                enemyArenaInfo,
                costumeStatSheet);

            simulator.Simulate();

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

            var arenaMapDict = new Dictionary<IKey, IValue>();
            foreach (var kv in weeklyArenaMap)
            {
                var key = kv.Key;
                var value = kv.Value;
                if (key.Equals(arenaKey))
                {
                    value = arenaInfo.Serialize();
                }

                if (key.Equals(enemyKey))
                {
                    value = enemyArenaInfo.Serialize();
                }

                arenaMapDict[key] = value;
            }

            var weeklyArenaDict = new Dictionary<IKey, IValue>();
            foreach (var kv in rawWeeklyArenaState)
            {
                weeklyArenaDict[kv.Key] = kv.Key.Equals((Text) "map")
                    ? new Dictionary(arenaMapDict)
                    : kv.Value;
            }

            states = states.SetState(weeklyArenaAddress, new Dictionary(weeklyArenaDict));

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Serialize WeeklyArenaState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            states = states
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize());

            if (migrationRequired)
            {
                states = states
                    .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                    .SetState(avatarAddress, avatarState.SerializeV2());
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Serialize AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}RankingBattle Total Executed Time: {Elapsed}", addressesHex, ended - started);
            EnemyPlayerDigest = enemyPlayerDigest;
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
            costumeIds = ((List) plainValue["costume_ids"])
                .Select(e => e.ToGuid())
                .ToList();
            equipmentIds = ((List) plainValue["equipment_ids"])
                .Select(e => e.ToGuid())
                .ToList();
        }
    }
}
