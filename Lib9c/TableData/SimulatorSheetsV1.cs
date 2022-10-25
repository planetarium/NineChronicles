namespace Nekoyume.TableData
{
    public class SimulatorSheetsV1
    {
        public readonly MaterialItemSheet MaterialItemSheet;
        public readonly SkillSheet SkillSheet;
        public readonly SkillBuffSheet SkillBuffSheet;
        public readonly SkillActionBuffSheet SkillActionBuffSheet;
        public readonly ActionBuffSheet ActionBuffSheet;
        public readonly StatBuffSheet StatBuffSheet;
        public readonly CharacterSheet CharacterSheet;
        public readonly CharacterLevelSheet CharacterLevelSheet;
        public readonly EquipmentItemSetEffectSheet EquipmentItemSetEffectSheet;

        public SimulatorSheetsV1(
            MaterialItemSheet materialItemSheet,
            SkillSheet skillSheet,
            SkillBuffSheet skillBuffSheet,
            StatBuffSheet statBuffSheet,
            SkillActionBuffSheet skillActionBuffSheet,
            ActionBuffSheet actionBuffSheet,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet
        )
        {
            MaterialItemSheet = materialItemSheet;
            SkillSheet = skillSheet;
            SkillBuffSheet = skillBuffSheet;
            StatBuffSheet = statBuffSheet;
            SkillActionBuffSheet = skillActionBuffSheet;
            ActionBuffSheet = actionBuffSheet;
            CharacterSheet = characterSheet;
            CharacterLevelSheet = characterLevelSheet;
            EquipmentItemSetEffectSheet = equipmentItemSetEffectSheet;
        }
    }

    public class StageSimulatorSheetsV1 : SimulatorSheetsV1
    {
        public readonly StageSheet StageSheet;
        public readonly StageWaveSheet StageWaveSheet;
        public readonly EnemySkillSheet EnemySkillSheet;

        public StageSimulatorSheetsV1(
            MaterialItemSheet materialItemSheet,
            SkillSheet skillSheet,
            SkillBuffSheet skillBuffSheet,
            StatBuffSheet statBuffSheet,
            SkillActionBuffSheet skillActionBuffSheet,
            ActionBuffSheet actionBuffSheet,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            StageSheet stageSheet,
            StageWaveSheet stageWaveSheet,
            EnemySkillSheet enemySkillSheet
        ) : base(
            materialItemSheet,
            skillSheet,
            skillBuffSheet,
            statBuffSheet,
            skillActionBuffSheet,
            actionBuffSheet,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet
        )
        {
            StageSheet = stageSheet;
            StageWaveSheet = stageWaveSheet;
            EnemySkillSheet = enemySkillSheet;
        }
    }

    public class RankingSimulatorSheetsV1 : SimulatorSheetsV1
    {
        public readonly WeeklyArenaRewardSheet WeeklyArenaRewardSheet;

        public RankingSimulatorSheetsV1(
            MaterialItemSheet materialItemSheet,
            SkillSheet skillSheet,
            SkillBuffSheet skillBuffSheet,
            StatBuffSheet statBuffSheet,
            SkillActionBuffSheet skillActionBuffSheet,
            ActionBuffSheet actionBuffSheet,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            WeeklyArenaRewardSheet weeklyArenaRewardSheet
        ) : base(
            materialItemSheet,
            skillSheet,
            skillBuffSheet,
            statBuffSheet,
            skillActionBuffSheet,
            actionBuffSheet,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet
        )
        {
            WeeklyArenaRewardSheet = weeklyArenaRewardSheet;
        }
    }

    public class ArenaSimulatorSheetsV1 : SimulatorSheetsV1
    {
        public CostumeStatSheet CostumeStatSheet { get; }
        public WeeklyArenaRewardSheet WeeklyArenaRewardSheet { get; }

        public ArenaSimulatorSheetsV1(
            MaterialItemSheet materialItemSheet,
            SkillSheet skillSheet,
            SkillBuffSheet skillBuffSheet,
            StatBuffSheet statBuffSheet,
            SkillActionBuffSheet skillActionBuffSheet,
            ActionBuffSheet actionBuffSheet,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            CostumeStatSheet costumeStatSheet,
            WeeklyArenaRewardSheet weeklyArenaRewardSheet
        ) : base(materialItemSheet,
            skillSheet,
            skillBuffSheet,
            statBuffSheet,
            skillActionBuffSheet,
            actionBuffSheet,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet)
        {
            CostumeStatSheet = costumeStatSheet;
            WeeklyArenaRewardSheet = weeklyArenaRewardSheet;

        }
    }

    public class RaidSimulatorSheetsV1 : SimulatorSheetsV1
    {
        public WorldBossCharacterSheet WorldBossCharacterSheet { get; }
        public WorldBossActionPatternSheet WorldBossActionPatternSheet { get; }
        public WorldBossBattleRewardSheet WorldBossBattleRewardSheet { get; }
        public RuneWeightSheet RuneWeightSheet { get; }
        public RuneSheet RuneSheet { get; }

        public RaidSimulatorSheetsV1(
            MaterialItemSheet materialItemSheet,
            SkillSheet skillSheet,
            SkillBuffSheet skillBuffSheet,
            StatBuffSheet statBuffSheet,
            SkillActionBuffSheet skillActionBuffSheet,
            ActionBuffSheet actionBuffSheet,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            WorldBossCharacterSheet worldBossCharacterSheet,
            WorldBossActionPatternSheet worldBossActionPatternSheet,
            WorldBossBattleRewardSheet worldBossBattleRewardSheet,
            RuneWeightSheet runeWeightSheet,
            RuneSheet runeSheet
        ) : base(materialItemSheet,
            skillSheet,
            skillBuffSheet,
            statBuffSheet,
            skillActionBuffSheet,
            actionBuffSheet,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet)
        {
            WorldBossCharacterSheet = worldBossCharacterSheet;
            WorldBossActionPatternSheet = worldBossActionPatternSheet;
            WorldBossBattleRewardSheet = worldBossBattleRewardSheet;
            RuneWeightSheet = runeWeightSheet;
            RuneSheet = runeSheet;
        }
    }
}
