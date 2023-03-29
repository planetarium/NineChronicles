using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.DevExtensions;
using Lib9c.DevExtensions.Action;
using Libplanet;
using Libplanet.Action;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;

namespace BalanceTool.Runtime
{
    public static partial class HackAndSlashCalculator
    {
        public static IEnumerable<PlayData> Calculate(
            IAccountStateDelta prevStates,
            IRandom random,
            long blockIndex,
            Address agentAddr,
            int avatarIndex,
            IEnumerable<PlayData> playDataList)
        {
            var states = prevStates;
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

            return playDataList.Select(pd => ExecuteHackAndSlash(
                states,
                random,
                blockIndex,
                agentAddr,
                avatarIndex,
                pd,
                sheets));
        }

        private static PlayData ExecuteHackAndSlash(
            IAccountStateDelta states,
            IRandom random,
            long blockIndex,
            Address agentAddr,
            int avatarIndex,
            PlayData playData,
            Dictionary<Type, (Address address, ISheet sheet)> sheets)
        {
            // Create and save AvatarState to states by playData.
            var cra = new CreateOrReplaceAvatar(
                avatarIndex: avatarIndex,
                level: playData.AvatarLevel,
                equipments: playData.Equipments,
                foods: playData.Foods,
                costumeIds: playData.CostumeIds,
                runes: playData.Runes.Select(tuple => (tuple.runeId, tuple.level)).ToArray());
            states = cra.Execute(states, random, blockIndex, agentAddr);

            // Execute HackAndSlash and apply to playData.Result.
            var randomForHas = new RandomImpl(random.Next());
            var avatarAddr = Addresses.GetAvatarAddress(agentAddr, avatarIndex);
            var avatarState = states.GetAvatarStateV2(avatarAddr);
            var inventory = avatarState.inventory;
            var has = new HackAndSlash
            {
                AvatarAddress = avatarAddr,
                WorldId = playData.WorldId,
                StageId = playData.StageId,
                Equipments = inventory.Equipments.Select(e => e.NonFungibleId).ToList(),
                Foods = inventory.Consumables.Select(e => e.NonFungibleId).ToList(),
                Costumes = inventory.Costumes.Select(e => e.NonFungibleId).ToList(),
                RuneInfos = playData.Runes
                    .Select(e => new RuneSlotInfo(e.runeSlotIndex, e.runeId))
                    .ToList(),
                StageBuffId = null, // Fix to null. But it will be set by playData.StageBuffId.
                ApStoneCount = 0, // Fix to 0.
                TotalPlayCount = 1, // Fix to 1.
            };
            var runeStates = playData.Runes
                .Select(tuple => RuneState.DeriveAddress(avatarAddr, tuple.runeId))
                .Select(addr => new RuneState((List)states.GetState(addr)!))
                .ToList();
            var skillsOnWaveStart = new List<Nekoyume.Model.Skill.Skill>();
            if (has.StageBuffId.HasValue)
            {
                var skill = CrystalRandomSkillState.GetSkill(
                    has.StageBuffId.Value,
                    sheets.GetSheet<CrystalRandomBuffSheet>(),
                    sheets.GetSheet<SkillSheet>());
                skillsOnWaveStart.Add(skill);
            }

            var prevLevel = avatarState.level;
            var exp = StageRewardExpHelper.GetExp(prevLevel, has.StageId);
            var simulator = RecreateSimulator(
                randomForHas,
                blockIndex,
                has,
                avatarState,
                runeStates,
                skillsOnWaveStart,
                sheets.GetSheet<StageSheet>()[has.StageId],
                sheets.GetSheet<StageWaveSheet>()[has.StageId],
                sheets.GetStageSimulatorSheets(),
                sheets.GetSheet<EnemySkillSheet>(),
                sheets.GetSheet<CostumeStatSheet>(),
                sheets.GetSheet<MaterialItemSheet>(),
                sheets.GetSheet<WorldSheet>(),
                sheets.GetSheet<WorldUnlockSheet>(),
                exp);
            var result = new PlayResult(null);
            for (var i = 0; i < playData.PlayCount; i++)
            {
                states = has.Execute(
                    states,
                    agentAddr,
                    blockIndex,
                    randomForHas);
                result = ApplyToPlayResult(
                    exp,
                    simulator,
                    result);
            }

            return playData.WithResult(result);
        }

        private static StageSimulator RecreateSimulator(
            IRandom random,
            long blockIndex,
            HackAndSlash has,
            AvatarState avatarState,
            List<RuneState> runeStates,
            List<Nekoyume.Model.Skill.Skill> skillsOnWaveStart,
            StageSheet.Row stageRow,
            StageWaveSheet.Row stageWaveRow,
            StageSimulatorSheets stageSimulatorSheets,
            EnemySkillSheet enemySkillSheet,
            CostumeStatSheet costumeStatSheet,
            MaterialItemSheet materialItemSheet,
            WorldSheet worldSheet,
            WorldUnlockSheet worldUnlockSheet,
            int exp)
        {
            var simulator = new StageSimulator(
                random,
                avatarState,
                has.Foods,
                runeStates,
                skillsOnWaveStart,
                has.WorldId,
                has.StageId,
                stageRow,
                stageWaveRow,
                avatarState.worldInformation.IsStageCleared(has.StageId),
                exp,
                stageSimulatorSheets,
                enemySkillSheet,
                costumeStatSheet,
                StageSimulatorV2.GetWaveRewards(
                    random,
                    stageRow,
                    materialItemSheet));
            simulator.Simulate();

            if (simulator.Log.IsClear)
            {
                simulator.Player.worldInformation.ClearStage(
                    has.WorldId,
                    has.StageId,
                    blockIndex,
                    worldSheet,
                    worldUnlockSheet
                );
            }

            return simulator;
        }

        private static PlayResult ApplyToPlayResult(
            int exp,
            ISimulator simulator,
            PlayResult result)
        {
            var clearedWaves = new Dictionary<int, int>(result.ClearedWaves);
            var clearedWaveNumber = simulator.Log.clearedWaveNumber;
            for (var i = 1; i <= clearedWaveNumber; i++)
            {
                if (clearedWaves.ContainsKey(i))
                {
                    clearedWaves[i]++;
                }
                else
                {
                    clearedWaves[i] = 1;
                }
            }

            var totalRewards = new Dictionary<int, int>(result.TotalRewards);
            foreach (var itemBase in simulator.Reward)
            {
                if (totalRewards.ContainsKey(itemBase.Id))
                {
                    totalRewards[itemBase.Id] += 1;
                }
                else
                {
                    totalRewards[itemBase.Id] = 1;
                }
            }

            return new PlayResult(
                clearedWaves,
                totalRewards,
                clearedWaveNumber > 0
                    ? result.TotalExp + exp
                    : result.TotalExp);
        }
    }
}
