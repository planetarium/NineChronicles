using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
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
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1225
    /// Updated at https://github.com/planetarium/lib9c/pull/1225
    /// </summary>
    [Serializable]
    [ActionType("hack_and_slash16")]
    public class HackAndSlash : GameAction
    {
        internal class HackAndSlashRandom : Random, IRandom
        {
            public HackAndSlashRandom(int seed) : base(seed)
            {
                Seed = seed;
            }

            public int Seed { get; }
        }

        public List<Guid> Costumes;
        public List<Guid> Equipments;
        public List<Guid> Foods;
        public int WorldId;
        public int StageId;
        public int? StageBuffId;
        public Address AvatarAddress;
        public int PlayCount = 1;

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
            var sheets = states.GetSheets(
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
            Validator.ValidateForHackAndSlash(avatarState,
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
            CrystalRandomSkillState skillState = null;
            var isNotClearedStage = !avatarState.worldInformation.IsStageCleared(StageId);
            var skillsOnWaveStart = new List<Skill>();
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
            // First simulating will use Foods.
            var simulator = new StageSimulator(
                random,
                avatarState,
                Foods,
                skillsOnWaveStart,
                WorldId,
                StageId,
                sheets.GetStageSimulatorSheets(),
                sheets.GetSheet<CostumeStatSheet>(),
                StageSimulator.ConstructorVersionV100080);
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Initialize Simulator: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            simulator.Simulate(1);
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

            sw.Stop();
            Log.Verbose(
                "{AddressesHex}Execute HackAndSlash({AvatarAddress}); worldId: {WorldId}, stageId: {StageId}, result: {Result}, " +
                "clearWave: {ClearWave}, totalWave: {TotalWave}",
                addressesHex,
                AvatarAddress,
                WorldId,
                StageId,
                simulator.Log.result,
                simulator.Log.clearedWaveNumber,
                simulator.Log.waveCount
            );

            avatarState.Update(simulator);
            var sumOfStars = simulator.Log.clearedWaveNumber;

            if (PlayCount > 1)
            {
                sw.Restart();
                // if PlayCount > 1, it is MultiHAS.
                for (var i = 1; i < PlayCount; i++)
                {
                    // Remainder simulating will not use Foods.
                    simulator = new StageSimulator(
                        new HackAndSlashRandom(random.Next()),
                        avatarState,
                        new List<Guid>(),
                        new List<Skill>(),
                        WorldId,
                        StageId,
                        sheets.GetStageSimulatorSheets(),
                        sheets.GetSheet<CostumeStatSheet>(),
                        StageSimulator.ConstructorVersionV100080);
                    simulator.Simulate(1);
                    if (simulator.Log.IsClear)
                    {
                        simulator.Player.worldInformation.ClearStage(
                            WorldId,
                            StageId,
                            blockIndex,
                            worldSheet,
                            worldUnlockSheet
                        );
                    }

                    avatarState.Update(simulator);
                    sumOfStars += simulator.Log.clearedWaveNumber;
                }
                sw.Stop();
                Log.Verbose("{AddressesHex}HAS loop Simulate: {Elapsed}, Count: {PlayCount}",
                    addressesHex, sw.Elapsed, PlayCount - 1);
            }

            if (isNotClearedStage)
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var lastClearedStageId);
                if (lastClearedStageId >= StageId)
                {
                    // Make new CrystalRandomSkillState by next stage Id.
                    skillState = new CrystalRandomSkillState(skillStateAddress, StageId + 1);
                    states = states.SetState(skillStateAddress, skillState.Serialize());
                }
                else
                {
                    // Update CrystalRandomSkillState.Stars by sum of clearedWaveNumber. (add)
                    skillState.Update(sumOfStars, sheets.GetSheet<CrystalStageBuffGachaSheet>());
                    states = states.SetState(skillStateAddress, skillState.Serialize());
                }
            }

            sw.Restart();
            avatarState.UpdateQuestRewards(sheets.GetSheet<MaterialItemSheet>());
            avatarState.updatedAt = blockIndex;
            avatarState.mailBox.CleanUp();
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Update AvatarState: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            if (isNotClearedStage && skillsOnWaveStart.Any())
            {
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
