#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c.DevExtensions;
using Lib9c.DevExtensions.Action;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;
using Nekoyume.TableData.Rune;

namespace BalanceTool
{
    public static partial class HackAndSlashCalculator
    {
        public static async UniTask<IEnumerable<PlayData>> CalculateAsync(
            IWorld prevStates,
            int? randomSeed,
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
                    typeof(DeBuffLimitSheet),
                });

            return await playDataList.Select(pd => ExecuteHackAndSlashAsync(
                states,
                randomSeed,
                blockIndex,
                agentAddr,
                avatarIndex,
                pd,
                sheets));
        }

        private static async UniTask<PlayData> ExecuteHackAndSlashAsync(
            IWorld states,
            int? randomSeed,
            long blockIndex,
            Address agentAddr,
            int avatarIndex,
            PlayData playData,
            Dictionary<Type, (Address address, ISheet sheet)> sheets)
        {
            randomSeed ??= new RandomImpl(DateTime.Now.Millisecond).Next(0, int.MaxValue);
            var random = new RandomImpl(randomSeed.Value);
            // Create and save AvatarState to states by playData.
            var cra = new CreateOrReplaceAvatar(
                avatarIndex: avatarIndex,
                level: playData.AvatarLevel,
                equipments: playData.Equipments,
                foods: playData.Foods,
                costumeIds: playData.CostumeIds,
                runes: playData.Runes.Select(tuple => (tuple.runeId, tuple.level)).ToArray(),
                crystalRandomBuff: playData.CrystalRandomBuffId == 0
                    ? null
                    : (playData.StageId, new[] { playData.CrystalRandomBuffId }));
            states = await UniTask.RunOnThreadPool(() => cra.Execute(
                states,
                new RandomImpl(random.Next()),
                blockIndex,
                agentAddr));

            // Execute HackAndSlash and apply to playData.Result.
            var avatarAddr = Addresses.GetAvatarAddress(agentAddr, avatarIndex);
            var avatarState = states.GetAvatarState(avatarAddr);
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
                StageBuffId = playData.CrystalRandomBuffId == 0
                    ? null
                    : playData.CrystalRandomBuffId,
                ApStoneCount = 0, // Fix to 0.
                TotalPlayCount = 1, // Fix to 1.
            };

            var runeSlotStateAddress = RuneSlotState.DeriveAddress(avatarAddr, BattleType.Adventure);
            var runeSlotState = states.TryGetLegacyState(runeSlotStateAddress, out List rawRuneSlotState)
                ? new RuneSlotState(rawRuneSlotState)
                : new RuneSlotState(BattleType.Adventure);
            runeSlotState.UpdateSlot(has.RuneInfos, sheets.GetSheet<RuneListSheet>());

            var allRuneState = states.GetRuneState(avatarAddr, out _);
            var collectionState = states.GetCollectionState(avatarAddr);
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
            var result = new PlayResult(null);
            for (var i = 0; i < playData.PlayCount; i++)
            {
                var hasRandomSeed = random.Next();
                states = has.Execute(
                    states,
                    agentAddr,
                    blockIndex,
                    new RandomImpl(hasRandomSeed));
                var simulator = CreateSimulatedSimulator(
                    new RandomImpl(hasRandomSeed),
                    blockIndex,
                    has,
                    avatarState,
                    allRuneState,
                    runeSlotState,
                    skillsOnWaveStart,
                    sheets.GetSheet<StageSheet>()[has.StageId],
                    sheets.GetSheet<StageWaveSheet>()[has.StageId],
                    sheets.GetStageSimulatorSheets(),
                    sheets.GetSheet<EnemySkillSheet>(),
                    sheets.GetSheet<CostumeStatSheet>(),
                    sheets.GetSheet<MaterialItemSheet>(),
                    collectionState,
                    sheets.GetSheet<CollectionSheet>(),
                    sheets.GetSheet<WorldSheet>(),
                    sheets.GetSheet<WorldUnlockSheet>(),
                    sheets.GetSheet<DeBuffLimitSheet>(),
                    sheets.GetSheet<BuffLinkSheet>(),
                    exp);
                result = ApplyToPlayResult(
                    exp,
                    simulator,
                    result);
            }

            return playData.WithResult(result);
        }

        private static StageSimulator CreateSimulatedSimulator(
            IRandom random,
            long blockIndex,
            HackAndSlash has,
            AvatarState avatarState,
            AllRuneState allRuneState,
            RuneSlotState runeSlotState,
            List<Nekoyume.Model.Skill.Skill> skillsOnWaveStart,
            StageSheet.Row stageRow,
            StageWaveSheet.Row stageWaveRow,
            StageSimulatorSheets stageSimulatorSheets,
            EnemySkillSheet enemySkillSheet,
            CostumeStatSheet costumeStatSheet,
            MaterialItemSheet materialItemSheet,
            CollectionState collectionState,
            CollectionSheet collectionSheet,
            WorldSheet worldSheet,
            WorldUnlockSheet worldUnlockSheet,
            DeBuffLimitSheet deBuffLimitSheet,
            BuffLinkSheet buffLinkSheet,
            int exp)
        {
            var simulator = new StageSimulator(
                random,
                avatarState,
                has.Foods,
                allRuneState,
                runeSlotState,
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
                StageSimulator.GetWaveRewards(
                    random,
                    stageRow,
                    materialItemSheet),
                collectionState.GetEffects(collectionSheet),
                deBuffLimitSheet,
                buffLinkSheet,
                logEvent: true,
                States.Instance.GameConfigState.ShatterStrikeMaxDamage
                );
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
            if (clearedWaveNumber >= 2)
            {
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
