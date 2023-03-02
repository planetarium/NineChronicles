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
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Skill;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1229
    /// Updated at https://github.com/planetarium/lib9c/pull/1241
    /// Updated at https://github.com/planetarium/lib9c/pull/1244
    /// </summary>
    [Serializable]
    [ActionType("hack_and_slash16")]
    public class HackAndSlash16 : GameAction, IHackAndSlashV8
    {
        public List<Guid> Costumes;
        public List<Guid> Equipments;
        public List<Guid> Foods;
        public int WorldId;
        public int StageId;
        public int? StageBuffId;
        public Address AvatarAddress;
        public int PlayCount = 1;

        IEnumerable<Guid> IHackAndSlashV8.Costumes => Costumes;
        IEnumerable<Guid> IHackAndSlashV8.Equipments => Equipments;
        IEnumerable<Guid> IHackAndSlashV8.Foods => Foods;
        int IHackAndSlashV8.WorldId => WorldId;
        int IHackAndSlashV8.StageId => StageId;
        int IHackAndSlashV8.PlayCount => PlayCount;
        int? IHackAndSlashV8.StageBuffId => StageBuffId;
        Address IHackAndSlashV8.AvatarAddress => AvatarAddress;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var dict = new Dictionary<string, IValue>
                {
                    ["costumes"] = new List(Costumes.OrderBy(i => i).Select(e => e.Serialize())),
                    ["equipments"] =
                        new List(Equipments.OrderBy(i => i).Select(e => e.Serialize())),
                    ["foods"] = new List(Foods.OrderBy(i => i).Select(e => e.Serialize())),
                    ["worldId"] = WorldId.Serialize(),
                    ["stageId"] = StageId.Serialize(),
                    ["avatarAddress"] = AvatarAddress.Serialize(),
                    ["playCount"] = PlayCount.Serialize(),
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
            WorldId = plainValue["worldId"].ToInteger();
            StageId = plainValue["stageId"].ToInteger();
            if (plainValue.ContainsKey("stageBuffId"))
            {
                StageBuffId = plainValue["stageBuffId"].ToNullableInteger();
            }
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            PlayCount = plainValue["playCount"].ToInteger();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            if (context.Rehearsal)
            {
                return context.PreviousStates;
            }

            return Execute(
                context.PreviousStates,
                context.Signer,
                context.BlockIndex,
                context.Random);
        }

        public IAccountStateDelta Execute(
            IAccountStateDelta states,
            Address signer,
            long blockIndex,
            IRandom random)
        {
            var inventoryAddress = AvatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = AvatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = AvatarAddress.Derive(LegacyQuestListKey);

            var addressesHex = $"[{signer.ToHex()}, {AvatarAddress.ToHex()}]";
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}HAS exec started", addressesHex);

            states.ValidateWorldId(AvatarAddress, WorldId);

            if (PlayCount <= 0)
            {
                throw new PlayCountIsZeroException(
                    $"{addressesHex}playCount must be greater than 0. " +
                    $"current playCount : {PlayCount}");
            }

            var sw = new Stopwatch();
            sw.Start();
            if (!states.TryGetAvatarStateV2(signer, AvatarAddress, out AvatarState avatarState, out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Get AvatarState: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            var sheets = states.GetSheetsV100291(
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
                });
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Get Sheets: {Elapsed}", addressesHex, sw.Elapsed);

            // Validate about avatar state.
            Validator.ValidateForHackAndSlashV1(avatarState,
                sheets,
                WorldId,
                StageId,
                Equipments,
                Costumes,
                Foods,
                sw,
                blockIndex,
                addressesHex,
                PlayCount);

            var items = Equipments.Concat(Costumes);
            avatarState.EquipItems(items);
            avatarState.actionPoint -= sheets.GetSheet<StageSheet>()[StageId].CostAP * PlayCount;
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Unequip items: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            var questSheet = sheets.GetQuestSheet();
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS GetQuestSheet: {Elapsed}", addressesHex, sw.Elapsed);

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
                Log.Verbose("{AddressesHex}HAS Update QuestList: {Elapsed}", addressesHex, sw.Elapsed);
            }

            sw.Restart();

            var skillStateAddress = Addresses.GetSkillStateAddressFromAvatarAddress(AvatarAddress);
            var isNotClearedStage = !avatarState.worldInformation.IsStageCleared(StageId);
            var skillsOnWaveStart = new List<Skill>();
            CrystalRandomSkillState skillState = null;
            if (isNotClearedStage)
            {
                // It has state, get CrystalRandomSkillState. If not, newly make.
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
                        selectedId = skillState.SkillIds
                            .OrderBy(id => crystalRandomBuffSheet[id].Rank)
                            .ThenBy(id => id)
                            .First();
                    }

                    var skill = CrystalRandomSkillState.GetSkill(
                        selectedId,
                        crystalRandomBuffSheet,
                        skillSheet);
                    skillsOnWaveStart.Add(skill);
                }
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Get skillState : {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            var worldSheet = sheets.GetSheet<WorldSheet>();
            var worldUnlockSheet = sheets.GetSheet<WorldUnlockSheet>();
            var crystalStageBuffSheet = sheets.GetSheet<CrystalStageBuffGachaSheet>();
            var stageRow = sheets.GetSheet<StageSheet>()[StageId];
            var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();
            sw.Restart();
            // if PlayCount > 1, it is Multi-HAS.
            for (var i = 0; i < PlayCount; i++)
            {
                sw.Restart();
                // First simulating will use Foods and Random Skills.
                // Remainder simulating will not use Foods.
                var simulator = new StageSimulatorV2(
                    random,
                    avatarState,
                    i == 0 ? Foods : new List<Guid>(),
                    i == 0 ? skillsOnWaveStart : new List<Skill>(),
                    WorldId,
                    StageId,
                    stageRow,
                    sheets.GetSheet<StageWaveSheet>()[StageId],
                    avatarState.worldInformation.IsStageCleared(StageId),
                    StageRewardExpHelper.GetExp(avatarState.level, StageId),
                    sheets.GetSimulatorSheetsV100291(),
                    sheets.GetSheet<EnemySkillSheet>(),
                    sheets.GetSheet<CostumeStatSheet>(),
                    StageSimulatorV2.GetWaveRewards(random, stageRow, materialItemSheet));
                sw.Stop();
                Log.Verbose("{AddressesHex}HAS Initialize Simulator: {Elapsed}", addressesHex, sw.Elapsed);

                sw.Restart();
                simulator.Simulate();
                sw.Stop();
                Log.Verbose("{AddressesHex}HAS Simulator.Simulate(): {Elapsed}", addressesHex, sw.Elapsed);

                sw.Restart();
                if (simulator.Log.IsClear)
                {
                    simulator.Player.worldInformation.ClearStage(
                        WorldId,
                        StageId,
                        blockIndex,
                        worldSheet,
                        worldUnlockSheet
                    );
                    sw.Stop();
                    Log.Verbose("{AddressesHex}HAS ClearStage: {Elapsed}", addressesHex, sw.Elapsed);
                }

                sw.Restart();
                avatarState.Update(simulator);
                // Update CrystalRandomSkillState.Stars by clearedWaveNumber. (add)
                skillState?.Update(simulator.Log.clearedWaveNumber, crystalStageBuffSheet);

                sw.Stop();
                Log.Verbose(
                    "{AddressesHex}Update avatar by simulator({AvatarAddress}); " +
                    "worldId: {WorldId}, stageId: {StageId}, result: {Result}, " +
                    "clearWave: {ClearWave}, totalWave: {TotalWave}",
                    addressesHex,
                    AvatarAddress,
                    WorldId,
                    StageId,
                    simulator.Log.result,
                    simulator.Log.clearedWaveNumber,
                    simulator.Log.waveCount
                );
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS loop Simulate: {Elapsed}, Count: {PlayCount}",
                addressesHex, sw.Elapsed, PlayCount);

            sw.Restart();
            avatarState.UpdateQuestRewards(materialItemSheet);
            avatarState.updatedAt = blockIndex;
            avatarState.mailBox.CleanUp();
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Update AvatarState: {Elapsed}", addressesHex, sw.Elapsed);

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
            Log.Verbose("{AddressesHex}HAS Set States: {Elapsed}", addressesHex, sw.Elapsed);

            var totalElapsed = DateTimeOffset.UtcNow - started;
            Log.Verbose("{AddressesHex}HAS Total Executed Time: {Elapsed}", addressesHex, totalElapsed);
            return states;
        }
    }
}
