
namespace Nekoyume.TableData
{
    public class SimulatorSheets : SimulatorSheetsV1
    {
        public readonly RuneOptionSheet RuneOptionSheet;

        public SimulatorSheets(
            MaterialItemSheet materialItemSheet,
            SkillSheet skillSheet,
            SkillBuffSheet skillBuffSheet,
            StatBuffSheet statBuffSheet,
            SkillActionBuffSheet skillActionBuffSheet,
            ActionBuffSheet actionBuffSheet,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            RuneOptionSheet runeOptionSheet
        ) : base(
            materialItemSheet,
            skillSheet,
            skillBuffSheet,
            statBuffSheet,
            skillActionBuffSheet,
            actionBuffSheet,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet)
        {
            RuneOptionSheet = runeOptionSheet;
        }
    }

    public class StageSimulatorSheets : SimulatorSheets
    {
        public readonly StageSheet StageSheet;
        public readonly StageWaveSheet StageWaveSheet;
        public readonly EnemySkillSheet EnemySkillSheet;

        public StageSimulatorSheets(
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
            EnemySkillSheet enemySkillSheet,
            RuneOptionSheet runeOptionSheet
        ) : base(
            materialItemSheet,
            skillSheet,
            skillBuffSheet,
            statBuffSheet,
            skillActionBuffSheet,
            actionBuffSheet,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet,
            runeOptionSheet
        )
        {
            StageSheet = stageSheet;
            StageWaveSheet = stageWaveSheet;
            EnemySkillSheet = enemySkillSheet;
        }
    }

    public class RankingSimulatorSheets : SimulatorSheets
    {
        public readonly WeeklyArenaRewardSheet WeeklyArenaRewardSheet;

        public RankingSimulatorSheets(
            MaterialItemSheet materialItemSheet,
            SkillSheet skillSheet,
            SkillBuffSheet skillBuffSheet,
            StatBuffSheet statBuffSheet,
            SkillActionBuffSheet skillActionBuffSheet,
            ActionBuffSheet actionBuffSheet,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            WeeklyArenaRewardSheet weeklyArenaRewardSheet,
            RuneOptionSheet runeOptionSheet
        ) : base(
            materialItemSheet,
            skillSheet,
            skillBuffSheet,
            statBuffSheet,
            skillActionBuffSheet,
            actionBuffSheet,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet,
            runeOptionSheet
        )
        {
            WeeklyArenaRewardSheet = weeklyArenaRewardSheet;
        }
    }

    public class ArenaSimulatorSheets : SimulatorSheets
    {
        public CostumeStatSheet CostumeStatSheet { get; }
        public WeeklyArenaRewardSheet WeeklyArenaRewardSheet { get; }

        public ArenaSimulatorSheets(
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
            WeeklyArenaRewardSheet weeklyArenaRewardSheet,
            RuneOptionSheet runeOptionSheet
        ) : base(materialItemSheet,
            skillSheet,
            skillBuffSheet,
            statBuffSheet,
            skillActionBuffSheet,
            actionBuffSheet,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet,
            runeOptionSheet)
        {
            CostumeStatSheet = costumeStatSheet;
            WeeklyArenaRewardSheet = weeklyArenaRewardSheet;

        }
    }

    public class RaidSimulatorSheets : SimulatorSheets
    {
        public WorldBossCharacterSheet WorldBossCharacterSheet { get; }
        public WorldBossActionPatternSheet WorldBossActionPatternSheet { get; }
        public WorldBossBattleRewardSheet WorldBossBattleRewardSheet { get; }
        public RuneWeightSheet RuneWeightSheet { get; }
        public RuneSheet RuneSheet { get; }

        public RaidSimulatorSheets(
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
            RuneSheet runeSheet,
            RuneOptionSheet runeOptionSheet
        ) : base(materialItemSheet,
            skillSheet,
            skillBuffSheet,
            statBuffSheet,
            skillActionBuffSheet,
            actionBuffSheet,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet,
            runeOptionSheet)
        {
            WorldBossCharacterSheet = worldBossCharacterSheet;
            WorldBossActionPatternSheet = worldBossActionPatternSheet;
            WorldBossBattleRewardSheet = worldBossBattleRewardSheet;
            RuneWeightSheet = runeWeightSheet;
            RuneSheet = runeSheet;
        }
    }
}
