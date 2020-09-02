namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class PlayerTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;

        public PlayerTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _random = new ItemEnhancementTest.TestRandom();
            var worldSheet = new WorldSheet();
            worldSheet.Set(_sheets[nameof(WorldSheet)]);
            var questRewardSheet = new QuestRewardSheet();
            questRewardSheet.Set(_sheets[nameof(QuestRewardSheet)]);
            var questItemRewardSheet = new QuestItemRewardSheet();
            questItemRewardSheet.Set(_sheets[nameof(QuestItemRewardSheet)]);
            var equipmentItemRecipeSheet = new EquipmentItemRecipeSheet();
            equipmentItemRecipeSheet.Set(_sheets[nameof(EquipmentItemRecipeSheet)]);
            var equipmentItemSubRecipeSheet = new EquipmentItemSubRecipeSheet();
            equipmentItemSubRecipeSheet.Set(_sheets[nameof(EquipmentItemSubRecipeSheet)]);
            var questSheet = new QuestSheet();
            questSheet.Set(_sheets[nameof(GeneralQuestSheet)]);

            _avatarState = new AvatarState(
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
            );
        }

        [Fact]
        public void TickAlive()
        {
            var materialItemSheet = new MaterialItemSheet();
            materialItemSheet.Set(_sheets[nameof(MaterialItemSheet)]);
            var worldUnlockSheet = new WorldUnlockSheet();
            worldUnlockSheet.Set(_sheets[nameof(WorldUnlockSheet)]);
            var equipmentItemSheet = new EquipmentItemSheet();
            equipmentItemSheet.Set(_sheets[nameof(EquipmentItemSheet)]);
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
            var stageSheet = new StageSheet();
            stageSheet.Set(_sheets[nameof(StageSheet)]);
            var stageWaveSheet = new StageWaveSheet();
            stageWaveSheet.Set(_sheets[nameof(StageWaveSheet)]);
            var enemySkillSheet = new EnemySkillSheet();
            enemySkillSheet.Set(_sheets[nameof(EnemySkillSheet)]);

            var simulator = new StageSimulator(
                _random,
                _avatarState,
                new List<Guid>(),
                1,
                1,
                materialItemSheet,
                skillSheet,
                skillBuffSheet,
                buffSheet,
                characterSheet,
                levelSheet,
                setEffectSheet,
                stageSheet,
                stageWaveSheet,
                enemySkillSheet
            );
            var player = simulator.Player;
            var enemy = new Enemy(player, characterSheet.Values.First(), 1);
            player.Targets.Add(enemy);
            player.InitAI();
            player.Tick();

            Assert.NotEmpty(simulator.Log);
            Assert.Equal(nameof(WaveTurnEnd), simulator.Log.Last().GetType().Name);
        }

        [Fact]
        public void TickDead()
        {
            var materialItemSheet = new MaterialItemSheet();
            materialItemSheet.Set(_sheets[nameof(MaterialItemSheet)]);
            var worldUnlockSheet = new WorldUnlockSheet();
            worldUnlockSheet.Set(_sheets[nameof(WorldUnlockSheet)]);
            var equipmentItemSheet = new EquipmentItemSheet();
            equipmentItemSheet.Set(_sheets[nameof(EquipmentItemSheet)]);
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
            var stageSheet = new StageSheet();
            stageSheet.Set(_sheets[nameof(StageSheet)]);
            var stageWaveSheet = new StageWaveSheet();
            stageWaveSheet.Set(_sheets[nameof(StageWaveSheet)]);
            var enemySkillSheet = new EnemySkillSheet();
            enemySkillSheet.Set(_sheets[nameof(EnemySkillSheet)]);

            var simulator = new StageSimulator(
                _random,
                _avatarState,
                new List<Guid>(),
                1,
                1,
                materialItemSheet,
                skillSheet,
                skillBuffSheet,
                buffSheet,
                characterSheet,
                levelSheet,
                setEffectSheet,
                stageSheet,
                stageWaveSheet,
                enemySkillSheet
            );
            var player = simulator.Player;
            var enemy = new Enemy(player, characterSheet.Values.First(), 1);
            player.Targets.Add(enemy);
            player.InitAI();
            player.CurrentHP = -1;

            Assert.True(player.IsDead);

            player.Tick();

            Assert.NotEmpty(simulator.Log);
            Assert.Equal(nameof(WaveTurnEnd), simulator.Log.Last().GetType().Name);
        }
    }
}
