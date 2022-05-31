using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    public static class AvatarStateExtensions
    {
        public static void UpdateMonsterMap(this AvatarState avatarState,
            StageWaveSheet stageWaveSheet, int stageId)
        {
            var monsterMap = new CollectionMap();
            if (stageWaveSheet.TryGetValue(stageId, out var stageWaveRow))
            {
                foreach (var monster in stageWaveRow.Waves.SelectMany(wave => wave.Monsters))
                {
                    monsterMap.Add(new KeyValuePair<int, int>(monster.CharacterId, monster.Count));
                }
            }

            avatarState.questList.UpdateMonsterQuest(monsterMap);
        }

        public static void UpdateInventory(this AvatarState avatarState, List<ItemBase> rewards)
        {
            var itemMap = new CollectionMap();
            foreach (var reward in rewards)
            {
                itemMap.Add(avatarState.inventory.AddItem(reward));
            }

            avatarState.questList.UpdateCollectQuest(itemMap);
        }

        public static void UpdateExp(this AvatarState avatarState, int level, long exp)
        {
            var levelUpCount = level - avatarState.level;
            var eventMap = new CollectionMap
                { new KeyValuePair<int, int>((int)QuestEventType.Level, levelUpCount) };
            avatarState.level = level;
            avatarState.exp = exp;
            avatarState.questList.UpdateCompletedQuest(eventMap);
        }

        public static (int, long) GetLevelAndExp(this AvatarState avatarState,
            CharacterLevelSheet characterLevelSheet, int stageId, int repeatCount)
        {
            var remainCount = repeatCount;
            var currentLevel = avatarState.level;
            var currentExp = avatarState.exp;
            while (remainCount > 0)
            {
                characterLevelSheet.TryGetValue(currentLevel, out var row, true);
                var maxExp = row.Exp + row.ExpNeed;
                var remainExp = maxExp - currentExp;
                var stageExp = StageRewardExpHelper.GetExp(currentLevel, stageId);
                if (stageExp == 0)
                {
                    break;
                }

                var requiredCount = (int)DecimalMath.DecimalEx.Ceiling(remainExp / (decimal)stageExp);
                if (remainCount - requiredCount >= 0) // level up
                {
                    currentExp += stageExp * requiredCount;
                    remainCount -= requiredCount;
                    currentLevel += 1;
                }
                else
                {
                    currentExp += stageExp * remainCount;
                    break;
                }
            }

            return (currentLevel, currentExp);
        }

        [Obsolete("Use GetLevelAndExp")]
        public static (int, long) GetLevelAndExpV1(this AvatarState avatarState,
            CharacterLevelSheet characterLevelSheet, int stageId, int repeatCount)
        {
            var remainCount = repeatCount;
            var currentLevel = avatarState.level;
            var currentExp = avatarState.exp;
            while (remainCount > 0)
            {
                characterLevelSheet.TryGetValue(currentLevel, out var row, true);
                var maxExp = row.Exp + row.ExpNeed;
                var remainExp = maxExp - currentExp;
                var stageExp = StageRewardExpHelper.GetExp(currentLevel, stageId);
                if (stageExp == 0)
                {
                    break;
                }

                var requiredCount = (int)DecimalMath.DecimalEx.Ceiling(remainExp / (decimal)stageExp);
                if (remainCount - requiredCount > 0) // level up
                {
                    currentExp += stageExp * requiredCount;
                    remainCount -= requiredCount;
                    currentLevel += 1;
                }
                else
                {
                    currentExp += stageExp * remainCount;
                    break;
                }
            }

            return (currentLevel, currentExp);
        }

        public static void ValidEquipmentAndCostume(this AvatarState avatarState,
            IEnumerable<Guid> costumeIds,
            List<Guid> equipmentIds,
            ItemRequirementSheet itemRequirementSheet,
            EquipmentItemRecipeSheet equipmentItemRecipeSheet,
            EquipmentItemSubRecipeSheetV2 equipmentItemSubRecipeSheetV2,
            EquipmentItemOptionSheet equipmentItemOptionSheet,
            long blockIndex,
            string addressesHex)
        {
            var equipments = avatarState.ValidateEquipmentsV2(equipmentIds, blockIndex);
            var costumeItemIds = avatarState.ValidateCostume(costumeIds);
            avatarState.ValidateItemRequirement(
                costumeItemIds.ToList(),
                equipments,
                itemRequirementSheet,
                equipmentItemRecipeSheet,
                equipmentItemSubRecipeSheetV2,
                equipmentItemOptionSheet,
                addressesHex);
        }
    }
}
