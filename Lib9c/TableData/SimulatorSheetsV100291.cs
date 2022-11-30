using Bencodex.Types;

namespace Nekoyume.TableData
{
    public class SimulatorSheetsV100291
    {
        public readonly MaterialItemSheet MaterialItemSheet;
        public readonly SkillSheet SkillSheet;
        public readonly SkillBuffSheet SkillBuffSheet;
        public readonly BuffSheet BuffSheet;
        public readonly CharacterSheet CharacterSheet;
        public readonly CharacterLevelSheet CharacterLevelSheet;
        public readonly EquipmentItemSetEffectSheet EquipmentItemSetEffectSheet;

        public SimulatorSheetsV100291(
            MaterialItemSheet materialItemSheet,
            SkillSheet skillSheet,
            SkillBuffSheet skillBuffSheet,
            BuffSheet buffSheet,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet
        )
        {
            MaterialItemSheet = materialItemSheet;
            SkillSheet = skillSheet;
            SkillBuffSheet = skillBuffSheet;
            BuffSheet = buffSheet;
            CharacterSheet = characterSheet;
            CharacterLevelSheet = characterLevelSheet;
            EquipmentItemSetEffectSheet = equipmentItemSetEffectSheet;
        }

        public SimulatorSheetsV1 ToSimulatorSheetsV1()
        {
            var statBuffSheet = new StatBuffSheet();
            statBuffSheet.Set((Text)BuffSheet.Serialize());
            return new SimulatorSheetsV1(MaterialItemSheet, SkillSheet, SkillBuffSheet,
                statBuffSheet, new SkillActionBuffSheet(), new ActionBuffSheet(), CharacterSheet,
                CharacterLevelSheet, EquipmentItemSetEffectSheet);
        }
    }

    public class StageSimulatorSheetsV100291 : SimulatorSheetsV100291
    {
        public readonly StageSheet StageSheet;
        public readonly StageWaveSheet StageWaveSheet;
        public readonly EnemySkillSheet EnemySkillSheet;

        public StageSimulatorSheetsV100291(
            MaterialItemSheet materialItemSheet,
            SkillSheet skillSheet,
            SkillBuffSheet skillBuffSheet,
            BuffSheet buffSheet,
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
            buffSheet,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet
        )
        {
            StageSheet = stageSheet;
            StageWaveSheet = stageWaveSheet;
            EnemySkillSheet = enemySkillSheet;
        }

        public StageSimulatorSheetsV1 ToStageSimulatorSheetsV1()
        {
            var statBuffSheet = new StatBuffSheet();
            statBuffSheet.Set((Text)BuffSheet.Serialize());
            return new StageSimulatorSheetsV1(MaterialItemSheet, SkillSheet, SkillBuffSheet, statBuffSheet,
                new SkillActionBuffSheet(), new ActionBuffSheet(), CharacterSheet,
                CharacterLevelSheet, EquipmentItemSetEffectSheet, StageSheet, StageWaveSheet, EnemySkillSheet);
        }
    }

    public class RankingSimulatorSheetsV100291 : SimulatorSheetsV100291
    {
        public readonly WeeklyArenaRewardSheet WeeklyArenaRewardSheet;

        public RankingSimulatorSheetsV100291(
            MaterialItemSheet materialItemSheet,
            SkillSheet skillSheet,
            SkillBuffSheet skillBuffSheet,
            BuffSheet buffSheet,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            WeeklyArenaRewardSheet weeklyArenaRewardSheet
        ) : base(
            materialItemSheet,
            skillSheet,
            skillBuffSheet,
            buffSheet,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet
        )
        {
            WeeklyArenaRewardSheet = weeklyArenaRewardSheet;
        }

        public RankingSimulatorSheetsV1 ToRankingSimulatorSheetsV1()
        {
            var statBuffSheet = new StatBuffSheet();
            statBuffSheet.Set((Text)BuffSheet.Serialize());
            return new RankingSimulatorSheetsV1(MaterialItemSheet, SkillSheet, SkillBuffSheet,
                statBuffSheet,
                new SkillActionBuffSheet(), new ActionBuffSheet(), CharacterSheet,
                CharacterLevelSheet, EquipmentItemSetEffectSheet, WeeklyArenaRewardSheet);
        }
    }

    public class ArenaSimulatorSheetsV100291 : SimulatorSheetsV100291
    {
        public CostumeStatSheet CostumeStatSheet { get; }
        public WeeklyArenaRewardSheet WeeklyArenaRewardSheet { get; }

        public ArenaSimulatorSheetsV100291(
            MaterialItemSheet materialItemSheet,
            SkillSheet skillSheet,
            SkillBuffSheet skillBuffSheet,
            BuffSheet buffSheet,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            CostumeStatSheet costumeStatSheet,
            WeeklyArenaRewardSheet weeklyArenaRewardSheet
        ) : base(materialItemSheet,
            skillSheet,
            skillBuffSheet,
            buffSheet,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet)
        {
            CostumeStatSheet = costumeStatSheet;
            WeeklyArenaRewardSheet = weeklyArenaRewardSheet;

        }

        public ArenaSimulatorSheetsV1 ToArenaSimulatorSheetsV1()
        {
            var statBuffSheet = new StatBuffSheet();
            statBuffSheet.Set((Text)BuffSheet.Serialize());
            return new ArenaSimulatorSheetsV1(MaterialItemSheet, SkillSheet, SkillBuffSheet, statBuffSheet,
                new SkillActionBuffSheet(), new ActionBuffSheet(), CharacterSheet,
                CharacterLevelSheet, EquipmentItemSetEffectSheet, CostumeStatSheet,
                WeeklyArenaRewardSheet);
        }
    }
}
