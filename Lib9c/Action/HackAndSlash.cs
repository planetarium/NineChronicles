using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;
using Serilog;
using static Lib9c.SerializeKeys;
using Skill = Nekoyume.Model.Skill.Skill;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/2195
    /// </summary>
    [Serializable]
    [ActionType("hack_and_slash22")]
    public class HackAndSlash : GameAction, IHackAndSlashV10
    {
        public const int UsableApStoneCount = 10;

        public List<Guid> Costumes;
        public List<Guid> Equipments;
        public List<Guid> Foods;
        public List<RuneSlotInfo> RuneInfos;
        public int WorldId;
        public int StageId;
        public int? StageBuffId;
        public Address AvatarAddress;
        public int TotalPlayCount = 1;
        public int ApStoneCount = 0;

        IEnumerable<Guid> IHackAndSlashV10.Costumes => Costumes;
        IEnumerable<Guid> IHackAndSlashV10.Equipments => Equipments;
        IEnumerable<Guid> IHackAndSlashV10.Foods => Foods;
        IEnumerable<IValue> IHackAndSlashV10.RuneSlotInfos => RuneInfos.Select(x => x.Serialize());
        int IHackAndSlashV10.WorldId => WorldId;
        int IHackAndSlashV10.StageId => StageId;
        int IHackAndSlashV10.TotalPlayCount => TotalPlayCount;
        int IHackAndSlashV10.ApStoneCount => ApStoneCount;
        int? IHackAndSlashV10.StageBuffId => StageBuffId;
        Address IHackAndSlashV10.AvatarAddress => AvatarAddress;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var dict = new Dictionary<string, IValue>
                {
                    ["costumes"] = new List(Costumes.OrderBy(i => i).Select(e => e.Serialize())),
                    ["equipments"] =
                        new List(Equipments.OrderBy(i => i).Select(e => e.Serialize())),
                    ["r"] = RuneInfos.OrderBy(x => x.SlotIndex).Select(x=> x.Serialize()).Serialize(),
                    ["foods"] = new List(Foods.OrderBy(i => i).Select(e => e.Serialize())),
                    ["worldId"] = WorldId.Serialize(),
                    ["stageId"] = StageId.Serialize(),
                    ["avatarAddress"] = AvatarAddress.Serialize(),
                    ["totalPlayCount"] = TotalPlayCount.Serialize(),
                    ["apStoneCount"] = ApStoneCount.Serialize(),
                };
                if (StageBuffId.HasValue)
                {
                    dict["stageBuffId"] = StageBuffId.Serialize();
                }
                return dict.ToImmutableDictionary();
            }
        }

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            Costumes = ((List)plainValue["costumes"]).Select(e => e.ToGuid()).ToList();
            Equipments = ((List)plainValue["equipments"]).Select(e => e.ToGuid()).ToList();
            Foods = ((List)plainValue["foods"]).Select(e => e.ToGuid()).ToList();
            RuneInfos = plainValue["r"].ToList(x => new RuneSlotInfo((List)x));
            WorldId = plainValue["worldId"].ToInteger();
            StageId = plainValue["stageId"].ToInteger();
            if (plainValue.ContainsKey("stageBuffId"))
            {
                StageBuffId = plainValue["stageBuffId"].ToNullableInteger();
            }
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            TotalPlayCount = plainValue["totalPlayCount"].ToInteger();
            ApStoneCount = plainValue["apStoneCount"].ToInteger();
        }

        public override IAccount Execute(IActionContext context)
        {
            context.UseGas(1);
            var random = context.GetRandom();
            return Execute(
                context.PreviousState,
                context.Signer,
                context.BlockIndex,
                random);
        }

        public IAccount Execute(
            IAccount states,
            Address signer,
            long blockIndex,
            IRandom random)
        {
            var inventoryAddress = AvatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = AvatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = AvatarAddress.Derive(LegacyQuestListKey);

            var addressesHex = $"[{signer.ToHex()}, {AvatarAddress.ToHex()}]";
            var started = DateTimeOffset.UtcNow;
            const string source = "HackAndSlash";
            Log.Verbose("{AddressesHex} {Source} from #{BlockIndex} exec started",
                addressesHex, source, blockIndex);

            if (ApStoneCount > UsableApStoneCount)
            {
                throw new UsageLimitExceedException(
                    "Exceeded the amount of ap stones that can be used " +
                    $"apStoneCount : {ApStoneCount} > UsableApStoneCount : {UsableApStoneCount}");
            }

            if (ApStoneCount < 0)
            {
                throw new InvalidItemCountException(
                    "ApStone count must not be negative. " +
                    $"Ap stone count: {ApStoneCount}");
            }

            if (TotalPlayCount <= 0)
            {
                throw new PlayCountIsZeroException(
                    $"{addressesHex}playCount must not be zero or negative. " +
                    $"Total play count : {TotalPlayCount}");
            }

            states.ValidateWorldId(AvatarAddress, WorldId);

            var sw = new Stopwatch();
            sw.Start();
            if (!states.TryGetAvatarStateV2(signer, AvatarAddress, out AvatarState avatarState, out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex} {Source} HAS {Process} from #{BlockIndex}: {Elapsed}",
                addressesHex, source, "Get AvatarState", blockIndex, sw.Elapsed.TotalMilliseconds);

            sw.Restart();
            var sheets = states.GetSheets(
                    containQuestSheet: true,
                    containSimulatorSheets: true,
                    sheetTypes: new[]
                    {
                        typeof(WorldSheet),
                        typeof(StageSheet),
                        typeof(StageWaveSheet),
                        typeof(EnemySkillSheet),
                        typeof(CostumeStatSheet),
                        typeof(SkillSheet),
                        typeof(QuestRewardSheet),
                        typeof(QuestItemRewardSheet),
                        typeof(EquipmentItemRecipeSheet),
                        typeof(WorldUnlockSheet),
                        typeof(MaterialItemSheet),
                        typeof(ItemRequirementSheet),
                        typeof(EquipmentItemRecipeSheet),
                        typeof(EquipmentItemSubRecipeSheetV2),
                        typeof(EquipmentItemOptionSheet),
                        typeof(CrystalStageBuffGachaSheet),
                        typeof(CrystalRandomBuffSheet),
                        typeof(StakeActionPointCoefficientSheet),
                        typeof(RuneListSheet),
                    });
            sw.Stop();
            Log.Verbose("{AddressesHex} {Source} HAS {Process} from #{BlockIndex}: {Elapsed}",
                addressesHex, source, "Get Sheets", blockIndex, sw.Elapsed.TotalMilliseconds);

            sw.Restart();
            var stakingLevel = 0;
            StakeActionPointCoefficientSheet actionPointCoefficientSheet = null;

            var goldCurrency = states.GetGoldCurrency();
            var stakedAmount = states.GetStakedAmount(signer);
            if (stakedAmount > goldCurrency * 0 &&
                sheets.TryGetSheet(out actionPointCoefficientSheet))
            {
                stakingLevel = actionPointCoefficientSheet.FindLevelByStakedAmount(signer, stakedAmount);
            }

            sw.Stop();
            Log.Verbose("{AddressesHex} {Source} HAS {Process} from #{BlockIndex}: {Elapsed}",
                addressesHex, source, "Check StateState", blockIndex, sw.Elapsed.TotalMilliseconds);

            var worldSheet = sheets.GetSheet<WorldSheet>();
            if (!worldSheet.TryGetValue(WorldId, out var worldRow, false))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(WorldSheet), WorldId);
            }

            if (StageId < worldRow.StageBegin ||
                StageId > worldRow.StageEnd)
            {
                throw new SheetRowColumnException(
                    $"{addressesHex}{WorldId} world is not contains {worldRow.Id} stage: " +
                    $"{worldRow.StageBegin}-{worldRow.StageEnd}");
            }

            sw.Restart();
            if (!sheets.GetSheet<StageSheet>().TryGetValue(StageId, out var stageRow))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(StageSheet), StageId);
            }

            sw.Stop();
            Log.Verbose("{AddressesHex} {Source} HAS {Process} from #{BlockIndex}: {Elapsed}",
                addressesHex, source, "Get StageSheet", blockIndex, sw.Elapsed.TotalMilliseconds);

            sw.Restart();
            var worldInformation = avatarState.worldInformation;
            if (!worldInformation.TryGetWorld(WorldId, out var world))
            {
                // NOTE: Add new World from WorldSheet
                worldInformation.AddAndUnlockNewWorld(worldRow, blockIndex, worldSheet);
                worldInformation.TryGetWorld(WorldId, out world);
            }

            if (!world.IsUnlocked)
            {
                throw new InvalidWorldException($"{addressesHex}{WorldId} is locked.");
            }

            if (world.StageBegin != worldRow.StageBegin ||
                world.StageEnd != worldRow.StageEnd)
            {
                worldInformation.UpdateWorld(worldRow);
            }

            if (!world.IsStageCleared && StageId != world.StageBegin)
            {
                throw new InvalidStageException(
                    $"{addressesHex}Aborted as the stage ({WorldId}/{StageId - 1}) is not cleared; " +
                    $"clear the stage ({world.Id}/{world.StageBegin}) first"
                );
            }

            if (world.IsStageCleared && StageId - 1 > world.StageClearedId)
            {
                throw new InvalidStageException(
                    $"{addressesHex}Aborted as the stage ({WorldId}/{StageId - 1}) is not cleared; " +
                    $"cleared stage is ({world.Id}/{world.StageClearedId}), so you can play stage " +
                    $"({world.Id}/{world.StageClearedId + 1})"
                );
            }

            sw.Stop();
            Log.Verbose("{AddressesHex} {Source} HAS {Process} from #{BlockIndex}: {Elapsed}",
                addressesHex, source, "Validate World", blockIndex, sw.Elapsed.TotalMilliseconds);

            sw.Restart();
            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the game config state was failed to load.");
            }

            var equipmentList = avatarState.ValidateEquipmentsV3(
                Equipments, blockIndex, gameConfigState);
            var foodIds = avatarState.ValidateConsumableV2(Foods, blockIndex, gameConfigState);
            var costumeIds = avatarState.ValidateCostumeV2(Costumes, gameConfigState);
            sw.Stop();
            Log.Verbose("{AddressesHex} {Source} HAS {Process} from #{BlockIndex}: {Elapsed}",
                addressesHex, source, "Validate Items", blockIndex, sw.Elapsed.TotalMilliseconds);
            sw.Restart();
            var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();
            var apPlayCount = TotalPlayCount;
            var minimumCostAp = stageRow.CostAP;
            if (actionPointCoefficientSheet != null && stakingLevel > 0)
            {
                minimumCostAp = actionPointCoefficientSheet.GetActionPointByStaking(
                    minimumCostAp,
                    1,
                    stakingLevel);
            }

            if (ApStoneCount > 0)
            {
                // use apStone
                var row = materialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone);
                if (!avatarState.inventory.RemoveFungibleItem(row.ItemId, blockIndex,
                        count: ApStoneCount))
                {
                    throw new NotEnoughMaterialException(
                        $"{addressesHex}Aborted as the player has no enough material ({row.Id})");
                }

                var apStonePlayCount =
                    ApStoneCount * (gameConfigState.ActionPointMax / minimumCostAp);
                apPlayCount = TotalPlayCount - apStonePlayCount;
                if (apPlayCount < 0)
                {
                    throw new InvalidRepeatPlayException(
                        $"{addressesHex}Invalid TotalPlayCount({TotalPlayCount}) and ApStoneCount({ApStoneCount}). " +
                        $"TotalPlayCount must be at least calculated apStonePlayCount({apStonePlayCount}). " +
                        $"Calculated ap play count: {apPlayCount}");
                }

                Log.Verbose(
                    "{AddressesHex} {Source} TotalPlayCount: {TotalPlayCount}, " +
                    "ApStoneCount: {ApStoneCount}, PlayCount by Ap stone: {ApStonePlayCount}, " +
                    "Ap cost per 1 play: {MinimumCostAp}, " +
                    "BlockIndex: {BlockIndex}, " +
                    "PlayCount by action point: {ApPlayCount}, Used AP: {UsedAp}",
                    addressesHex,
                    source,
                    TotalPlayCount,
                    ApStoneCount,
                    apStonePlayCount,
                    minimumCostAp,
                    apPlayCount,
                    apPlayCount * minimumCostAp);
            }

            if (avatarState.actionPoint < minimumCostAp * apPlayCount)
            {
                throw new NotEnoughActionPointException(
                    $"{addressesHex}Aborted due to insufficient action point: " +
                    $"{avatarState.actionPoint} < cost({minimumCostAp * apPlayCount}))"
                );
            }

            avatarState.actionPoint -= minimumCostAp * apPlayCount;
            avatarState.ValidateItemRequirement(
                costumeIds.Concat(foodIds).ToList(),
                equipmentList,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                addressesHex);

            var items = Equipments.Concat(Costumes);
            avatarState.EquipItems(items);
            sw.Stop();
            Log.Verbose("{AddressesHex} {Source} HAS {Process} from #{BlockIndex}: {Elapsed}",
                addressesHex, source, "Unequip items", blockIndex, sw.Elapsed.TotalMilliseconds);

            sw.Restart();
            var questSheet = sheets.GetQuestSheet();
            sw.Stop();
            Log.Verbose("{AddressesHex} {Source} HAS {Process} from #{BlockIndex}: {Elapsed}",
                addressesHex, source, "Get QuestSheet", blockIndex, sw.Elapsed.TotalMilliseconds);

            // Update QuestList only when QuestSheet.Count is greater than QuestList.Count
            var questList = avatarState.questList;
            if (questList.Count() < questSheet.Count)
            {
                sw.Restart();
                questList.UpdateList(
                    questSheet,
                    sheets.GetSheet<QuestRewardSheet>(),
                    sheets.GetSheet<QuestItemRewardSheet>(),
                    sheets.GetSheet<EquipmentItemRecipeSheet>());
                sw.Stop();
                Log.Verbose("{AddressesHex} {Source} HAS {Process} from #{BlockIndex}: {Elapsed}",
                    addressesHex, source, "Update QuestList", blockIndex, sw.Elapsed.TotalMilliseconds);
            }

            sw.Restart();

            var skillStateAddress = Addresses.GetSkillStateAddressFromAvatarAddress(AvatarAddress);
            var isNotClearedStage = !avatarState.worldInformation.IsStageCleared(StageId);
            var skillsOnWaveStart = new List<Skill>();
            CrystalRandomSkillState skillState = null;
            if (isNotClearedStage)
            {
                // If state exists, get CrystalRandomSkillState. If not, create new state.
                skillState = states.TryGetState<List>(skillStateAddress, out var serialized)
                    ? new CrystalRandomSkillState(skillStateAddress, serialized)
                    : new CrystalRandomSkillState(skillStateAddress, StageId);

                if (skillState.SkillIds.Any())
                {
                    var crystalRandomBuffSheet = sheets.GetSheet<CrystalRandomBuffSheet>();
                    var skillSheet = sheets.GetSheet<SkillSheet>();
                    int selectedId;
                    if (StageBuffId.HasValue && skillState.SkillIds.Contains(StageBuffId.Value))
                    {
                        selectedId = StageBuffId.Value;
                    }
                    else
                    {
                        selectedId = skillState.GetHighestRankSkill(crystalRandomBuffSheet);
                    }

                    var skill = CrystalRandomSkillState.GetSkill(
                        selectedId,
                        crystalRandomBuffSheet,
                        skillSheet);
                    skillsOnWaveStart.Add(skill);
                }
            }

            sw.Stop();
            Log.Verbose("{AddressesHex} {Source} HAS {Process} from #{BlockIndex}: {Elapsed}",
                addressesHex, source, "Get skillState", blockIndex, sw.Elapsed.TotalMilliseconds);

            sw.Restart();
            var worldUnlockSheet = sheets.GetSheet<WorldUnlockSheet>();
            var crystalStageBuffSheet = sheets.GetSheet<CrystalStageBuffGachaSheet>();
            sw.Restart();
            // if PlayCount > 1, it is Multi-HAS.
            var simulatorSheets = sheets.GetSimulatorSheets();

            // update rune slot
            var runeSlotStateAddress = RuneSlotState.DeriveAddress(AvatarAddress, BattleType.Adventure);
            var runeSlotState = states.TryGetState(runeSlotStateAddress, out List rawRuneSlotState)
                ? new RuneSlotState(rawRuneSlotState)
                : new RuneSlotState(BattleType.Adventure);
            var runeListSheet = sheets.GetSheet<RuneListSheet>();
            runeSlotState.UpdateSlot(RuneInfos, runeListSheet);
            states = states.SetState(runeSlotStateAddress, runeSlotState.Serialize());

            // update item slot
            var itemSlotStateAddress = ItemSlotState.DeriveAddress(AvatarAddress, BattleType.Adventure);
            var itemSlotState = states.TryGetState(itemSlotStateAddress, out List rawItemSlotState)
                ? new ItemSlotState(rawItemSlotState)
                : new ItemSlotState(BattleType.Adventure);
            itemSlotState.UpdateEquipment(Equipments);
            itemSlotState.UpdateCostumes(Costumes);
            states = states.SetState(itemSlotStateAddress, itemSlotState.Serialize());

            var runeStates = new List<RuneState>();
            foreach (var address in RuneInfos.Select(info => RuneState.DeriveAddress(AvatarAddress, info.RuneId)))
            {
                if (states.TryGetState(address, out List rawRuneState))
                {
                    runeStates.Add(new RuneState(rawRuneState));
                }
            }
            sw.Stop();
            Log.Verbose("{AddressesHex} {Source} HAS {Process} from #{BlockIndex}: {Elapsed}",
                addressesHex, source, "Update slotState", blockIndex, sw.Elapsed.TotalMilliseconds);

            var stageWaveRow =  sheets.GetSheet<StageWaveSheet>()[StageId];
            var enemySkillSheet = sheets.GetSheet<EnemySkillSheet>();
            var costumeStatSheet = sheets.GetSheet<CostumeStatSheet>();
            var stageCleared = !isNotClearedStage;
            var starCount = 0;
            for (var i = 0; i < TotalPlayCount; i++)
            {
                var rewards = StageSimulator.GetWaveRewards(random, stageRow, materialItemSheet);
                sw.Restart();
                // First simulating will use Foods and Random Skills.
                // Remainder simulating will not use Foods.
                var simulator = new StageSimulator(
                    random,
                    avatarState,
                    i == 0 ? Foods : new List<Guid>(),
                    runeStates,
                    i == 0 ? skillsOnWaveStart : new List<Skill>(),
                    WorldId,
                    StageId,
                    stageRow,
                    stageWaveRow,
                    stageCleared,
                    StageRewardExpHelper.GetExp(avatarState.level, StageId),
                    simulatorSheets,
                    enemySkillSheet,
                    costumeStatSheet,
                    rewards,
                    false);
                sw.Stop();
                Log.Verbose("{AddressesHex} {Source} HAS {Process} from #{BlockIndex}: {Elapsed}",
                    addressesHex, source, "Initialize Simulator", blockIndex, sw.Elapsed.TotalMilliseconds);

                sw.Restart();
                simulator.Simulate();
                sw.Stop();
                Log.Verbose("{AddressesHex} {Source} HAS {Process} from #{BlockIndex}: {Elapsed}",
                    addressesHex, source, "Simulator.Simulate()", blockIndex, sw.Elapsed.TotalMilliseconds);

                sw.Restart();
                if (simulator.Log.IsClear)
                {
                    if (!stageCleared)
                    {
                        avatarState.worldInformation.ClearStage(
                            WorldId,
                            StageId,
                            blockIndex,
                            worldSheet,
                            worldUnlockSheet
                        );
                        stageCleared = true;
                    }
                    sw.Stop();
                    Log.Verbose("{AddressesHex} {Source} HAS {Process} from #{BlockIndex}: {Elapsed}",
                        addressesHex, source, "ClearStage", blockIndex, sw.Elapsed.TotalMilliseconds);
                }

                sw.Restart();

                // This conditional logic is same as written in the
                // MimisbrunnrBattle("mimisbrunnr_battle10") action.
                if (blockIndex < ActionObsoleteConfig.V100310ExecutedBlockIndex)
                {
                    var player = simulator.Player;
                    foreach (var key in player.monsterMapForBeforeV100310.Keys)
                    {
                        player.monsterMap.Add(key, player.monsterMapForBeforeV100310[key]);
                    }

                    player.monsterMapForBeforeV100310.Clear();

                    foreach (var key in player.eventMapForBeforeV100310.Keys)
                    {
                        player.eventMap.Add(key, player.eventMapForBeforeV100310[key]);
                    }

                    player.eventMapForBeforeV100310.Clear();
                }

                starCount += simulator.Log.clearedWaveNumber;
                avatarState.Update(simulator);

                sw.Stop();
                Log.Verbose(
                    "{AddressesHex} {Source} {Process} by simulator({AvatarAddress}); " +
                    "blockIndex: {BlockIndex} " +
                    "worldId: {WorldId}, stageId: {StageId}, result: {Result}, " +
                    "clearWave: {ClearWave}, totalWave: {TotalWave}",
                    addressesHex,
                    source,
                    "Update avatar",
                    AvatarAddress,
                    WorldId,
                    StageId,
                    simulator.Log.result,
                    simulator.Log.clearedWaveNumber,
                    simulator.Log.waveCount
                );
            }
            sw.Stop();
            Log.Debug("{AddressesHex} {Source} {Process} from #{BlockIndex}: {Elapsed}, Count: {PlayCount}",
                addressesHex, source, "loop Simulate", blockIndex, sw.Elapsed.TotalMilliseconds, TotalPlayCount);

            // Update CrystalRandomSkillState.Stars by clearedWaveNumber. (add)
            skillState?.Update(starCount, crystalStageBuffSheet);
            sw.Restart();
            avatarState.UpdateQuestRewards(materialItemSheet);
            avatarState.updatedAt = blockIndex;
            avatarState.mailBox.CleanUp();
            sw.Stop();
            Log.Verbose("{AddressesHex} {Source} HAS {Process} from #{BlockIndex}: {Elapsed}",
                addressesHex, source, "Update AvatarState", blockIndex, sw.Elapsed.TotalMilliseconds);

            sw.Restart();
            if (isNotClearedStage)
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var lastClearedStageId);
                if (lastClearedStageId >= StageId)
                {
                    // Make new CrystalRandomSkillState by next stage Id.
                    skillState = new CrystalRandomSkillState(skillStateAddress, StageId + 1);
                }

                skillState.Update(new List<int>());
                states = states.SetState(skillStateAddress, skillState.Serialize());
            }

            states = states
                .SetState(AvatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize());
            sw.Stop();
            Log.Verbose("{AddressesHex} {Source} HAS {Process} from #{BlockIndex}: {Elapsed}",
                addressesHex, source, "Set States", blockIndex, sw.Elapsed.TotalMilliseconds);

            var totalElapsed = DateTimeOffset.UtcNow - started;
            Log.Verbose("{AddressesHex} {Source} HAS {Process}: {Elapsed}, blockIndex: {BlockIndex}", addressesHex, source, "Total Executed Time", totalElapsed.TotalMilliseconds, blockIndex);
            return states;
        }

    }
}
