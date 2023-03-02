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
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1222
    /// Updated at https://github.com/planetarium/lib9c/pull/1225
    /// </summary>
    [Serializable]
    [ActionType("hack_and_slash15")]
    public class HackAndSlash15 : GameAction, IHackAndSlashV7
    {
        public List<Guid> costumes;
        public List<Guid> equipments;
        public List<Guid> foods;
        public int worldId;
        public int stageId;
        public int? stageBuffId;
        public Address avatarAddress;

        IEnumerable<Guid> IHackAndSlashV7.Costumes => costumes;
        IEnumerable<Guid> IHackAndSlashV7.Equipments => equipments;
        IEnumerable<Guid> IHackAndSlashV7.Foods => foods;
        int IHackAndSlashV7.WorldId => worldId;
        int IHackAndSlashV7.StageId => stageId;
        int? IHackAndSlashV7.StageBuffId => stageBuffId;
        Address IHackAndSlashV7.AvatarAddress => avatarAddress;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var dict = new Dictionary<string, IValue>
                {
                    ["costumes"] = new List(costumes.OrderBy(i => i).Select(e => e.Serialize())),
                    ["equipments"] =
                        new List(equipments.OrderBy(i => i).Select(e => e.Serialize())),
                    ["foods"] = new List(foods.OrderBy(i => i).Select(e => e.Serialize())),
                    ["worldId"] = worldId.Serialize(),
                    ["stageId"] = stageId.Serialize(),
                    ["avatarAddress"] = avatarAddress.Serialize(),
                };
                if (stageBuffId.HasValue)
                {
                    dict["stageBuffId"] = stageBuffId.Serialize();
                }
                return dict.ToImmutableDictionary();
            }
        }

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            costumes = ((List)plainValue["costumes"]).Select(e => e.ToGuid()).ToList();
            equipments = ((List)plainValue["equipments"]).Select(e => e.ToGuid()).ToList();
            foods = ((List)plainValue["foods"]).Select(e => e.ToGuid()).ToList();
            worldId = plainValue["worldId"].ToInteger();
            stageId = plainValue["stageId"].ToInteger();
            if (plainValue.ContainsKey("stageBuffId"))
            {
                stageBuffId = plainValue["stageBuffId"].ToNullableInteger();
            }
            avatarAddress = plainValue["avatarAddress"].ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            return Execute(context.PreviousStates,
                context.Signer,
                context.BlockIndex,
                context.Random);
        }

        public IAccountStateDelta Execute(IAccountStateDelta states,
            Address signer,
            long blockIndex,
            IRandom random)
        {
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);

            var addressesHex = $"[{signer.ToHex()}, {avatarAddress.ToHex()}]";
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}HAS exec started", addressesHex);

            states.ValidateWorldId(avatarAddress, worldId);

            var sw = new Stopwatch();
            sw.Start();
            if (!states.TryGetAvatarStateV2(signer, avatarAddress, out AvatarState avatarState, out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Get AvatarState: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            var sheets = states.GetSheetsV100291(
                containQuestSheet: true,
                containStageSimulatorSheets: true,
                sheetTypes: new[]
                {
                    typeof(WorldSheet),
                    typeof(StageSheet),
                    typeof(SkillSheet),
                    typeof(QuestRewardSheet),
                    typeof(QuestItemRewardSheet),
                    typeof(EquipmentItemRecipeSheet),
                    typeof(CostumeStatSheet),
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
                worldId,
                stageId,
                equipments,
                costumes,
                foods,
                sw,
                blockIndex,
                addressesHex);

            var items = equipments.Concat(costumes);
            avatarState.EquipItems(items);
            avatarState.actionPoint -= sheets.GetSheet<StageSheet>()[stageId].CostAP;
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

            var skillStateAddress = Addresses.GetSkillStateAddressFromAvatarAddress(avatarAddress);
            CrystalRandomSkillState skillState = null;
            var isNotClearedStage = !avatarState.worldInformation.IsStageCleared(stageId);
            var skillsOnWaveStart = new List<Model.Skill.Skill>();
            if (isNotClearedStage)
            {
                // It has state, get CrystalRandomSkillState. If not, newly make.
                skillState = states.TryGetState<List>(skillStateAddress, out var serialized)
                    ? new CrystalRandomSkillState(skillStateAddress, serialized)
                    : new CrystalRandomSkillState(skillStateAddress, stageId);

                if (skillState.SkillIds.Any())
                {
                    var crystalRandomBuffSheet = sheets.GetSheet<CrystalRandomBuffSheet>();
                    var skillSheet = sheets.GetSheet<SkillSheet>();
                    int selectedId;
                    if (stageBuffId.HasValue && skillState.SkillIds.Contains(stageBuffId.Value))
                    {
                        selectedId = stageBuffId.Value;
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
            var simulator = new StageSimulatorV1(
                random,
                avatarState,
                foods,
                skillsOnWaveStart,
                worldId,
                stageId,
                sheets.GetStageSimulatorSheetsV100291(),
                sheets.GetSheet<CostumeStatSheet>(),
                StageSimulatorV1.ConstructorVersionV100080);

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Initialize Simulator: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            simulator.Simulate(1);
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Simulator.Simulate(): {Elapsed}", addressesHex, sw.Elapsed);

            Log.Verbose(
                "{AddressesHex}Execute HackAndSlash({AvatarAddress}); worldId: {WorldId}, stageId: {StageId}, result: {Result}, " +
                "clearWave: {ClearWave}, totalWave: {TotalWave}",
                addressesHex,
                avatarAddress,
                worldId,
                stageId,
                simulator.Log.result,
                simulator.Log.clearedWaveNumber,
                simulator.Log.waveCount
            );

            if (simulator.Log.IsClear)
            {
                sw.Restart();
                simulator.Player.worldInformation.ClearStage(
                    worldId,
                    stageId,
                    blockIndex,
                    sheets.GetSheet<WorldSheet>(),
                    sheets.GetSheet<WorldUnlockSheet>()
                );
                sw.Stop();
                Log.Verbose("{AddressesHex}HAS ClearStage: {Elapsed}", addressesHex, sw.Elapsed);

                if (isNotClearedStage)
                {
                    // Make new CrystalRandomSkillState by next stage Id.
                    var nextStageSkillState = new CrystalRandomSkillState(skillStateAddress, stageId + 1);
                    states = states.SetState(skillStateAddress, nextStageSkillState.Serialize());
                }
            }
            else
            {
                if (isNotClearedStage)
                {
                    // Update CrystalRandomSkillState.Stars by clearedWaveNumber. (add)
                    skillState!.Update(simulator.Log.clearedWaveNumber,
                        sheets.GetSheet<CrystalStageBuffGachaSheet>());
                    // clear current skill id.
                    skillState!.Update(new List<int>());
                    states = states.SetState(skillStateAddress, skillState!.Serialize());
                }
            }

            sw.Restart();
            avatarState.Update(simulator);
            avatarState.UpdateQuestRewards(sheets.GetSheet<MaterialItemSheet>());
            avatarState.updatedAt = blockIndex;
            avatarState.mailBox.CleanUp();
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Update AvatarState: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            states = states
                .SetState(avatarAddress, avatarState.SerializeV2())
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
