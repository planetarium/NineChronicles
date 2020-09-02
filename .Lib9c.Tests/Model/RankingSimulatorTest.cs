namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume;
    using Nekoyume.Battle;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class RankingSimulatorTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly IRandom _random;

        public RankingSimulatorTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _random = new ItemEnhancementTest.TestRandom();
        }

        [Theory]
        [InlineData(1, 1, true)]
        [InlineData(2, 1, true)]
        [InlineData(1, 2, false)]
        public void Simulate(int level, int requiredLevel, bool expected)
        {
            var sheets = TableSheetsImporter.ImportSheets();
            var worldSheet = new WorldSheet();
            worldSheet.Set(sheets[nameof(WorldSheet)]);
            var questRewardSheet = new QuestRewardSheet();
            questRewardSheet.Set(sheets[nameof(QuestRewardSheet)]);
            var questItemRewardSheet = new QuestItemRewardSheet();
            questItemRewardSheet.Set(sheets[nameof(QuestItemRewardSheet)]);
            var equipmentItemRecipeSheet = new EquipmentItemRecipeSheet();
            equipmentItemRecipeSheet.Set(sheets[nameof(EquipmentItemRecipeSheet)]);
            var equipmentItemSubRecipeSheet = new EquipmentItemSubRecipeSheet();
            equipmentItemSubRecipeSheet.Set(sheets[nameof(EquipmentItemSubRecipeSheet)]);
            var questSheet = new QuestSheet();
            questSheet.Set(sheets[nameof(GeneralQuestSheet)]);
            var worldUnlockSheet = new WorldUnlockSheet();
            worldUnlockSheet.Set(sheets[nameof(WorldUnlockSheet)]);
            var rewardSheet = new WeeklyArenaRewardSheet();
            rewardSheet.Set($"id,item_id,ratio,min,max,required_level\n1,302000,0.1,1,1,{requiredLevel}");
            var avatarState = new AvatarState(
                default,
                default,
                0,
                worldSheet,
                questSheet,
                questRewardSheet,
                questItemRewardSheet,
                equipmentItemRecipeSheet,
                equipmentItemSubRecipeSheet,
                new GameConfigState()
            )
            {
                level = level,
            };
            avatarState.worldInformation.ClearStage(
                1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                1,
                worldSheet,
                worldUnlockSheet
            );

            var materialItemSheet = new MaterialItemSheet();
            materialItemSheet.Set(_sheets[nameof(MaterialItemSheet)]);
            var skillSheet = new SkillSheet();
            skillSheet.Set(_sheets[nameof(SkillSheet)]);
            var skillBuffSheet = new SkillBuffSheet();
            skillBuffSheet.Set(_sheets[nameof(SkillBuffSheet)]);
            var buffSheet = new BuffSheet();
            buffSheet.Set(_sheets[nameof(BuffSheet)]);
            var characterSheet = new CharacterSheet();
            characterSheet.Set(_sheets[nameof(CharacterSheet)]);
            var levelSheet = new CharacterLevelSheet();
            levelSheet.Set(_sheets[nameof(CharacterLevelSheet)]);
            var setEffectSheet = new EquipmentItemSetEffectSheet();
            setEffectSheet.Set(_sheets[nameof(EquipmentItemSetEffectSheet)]);

            var simulator = new RankingSimulator(
                _random,
                avatarState,
                avatarState,
                new List<Guid>(),
                materialItemSheet,
                skillSheet,
                skillBuffSheet,
                buffSheet,
                characterSheet,
                levelSheet,
                setEffectSheet,
                rewardSheet,
                1,
                new ArenaInfo(avatarState, characterSheet, false),
                new ArenaInfo(avatarState, characterSheet, false)
            );
            simulator.Simulate();

            Assert.Equal(expected, simulator.Reward.Any());
        }
    }
}
