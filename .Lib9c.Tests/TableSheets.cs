namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Crystal;
    using Nekoyume.TableData.Event;
    using Nekoyume.TableData.GrandFinale;

    public class TableSheets
    {
        public TableSheets(Dictionary<string, string> sheets)
        {
            foreach (var (key, value) in sheets)
            {
                var sheetPropertyInfo = GetType().GetProperty(key);
                if (sheetPropertyInfo is null)
                {
                    var sb = new StringBuilder($"[{nameof(TableSheets)}]");
                    sb.Append($" / ({key}, csv)");
                    sb.Append(" / failed to get property");
                    throw new Exception(sb.ToString());
                }

                var sheetObject = Activator.CreateInstance(sheetPropertyInfo.PropertyType);
                var iSheet = (ISheet)sheetObject;
                if (iSheet is null)
                {
                    var sb = new StringBuilder($"[{nameof(TableSheets)}]");
                    sb.Append($" / ({key}, csv)");
                    sb.Append($" / failed to cast to {nameof(ISheet)}");
                    throw new Exception(sb.ToString());
                }

                iSheet.Set(value);
                sheetPropertyInfo.SetValue(this, sheetObject);
            }

            ItemSheetInitialize();
            QuestSheetInitialize();
        }

        public WorldSheet WorldSheet { get; private set; }

        public StageWaveSheet StageWaveSheet { get; private set; }

        public StageSheet StageSheet { get; private set; }

        public MimisbrunnrSheet MimisbrunnrSheet { get; private set; }

        public CharacterSheet CharacterSheet { get; private set; }

        public CharacterLevelSheet CharacterLevelSheet { get; private set; }

        public SkillSheet SkillSheet { get; private set; }

        public StatBuffSheet StatBuffSheet { get; private set; }

        public ItemSheet ItemSheet { get; private set; }

        public ItemRequirementSheet ItemRequirementSheet { get; private set; }

        public ConsumableItemSheet ConsumableItemSheet { get; private set; }

        public CostumeItemSheet CostumeItemSheet { get; private set; }

        public EquipmentItemSheet EquipmentItemSheet { get; private set; }

        public MaterialItemSheet MaterialItemSheet { get; private set; }

        public QuestSheet QuestSheet { get; private set; }

        public WorldQuestSheet WorldQuestSheet { get; private set; }

        public CollectQuestSheet CollectQuestSheet { get; private set; }

        public ConsumableItemRecipeSheet ConsumableItemRecipeSheet { get; private set; }

        public CombinationQuestSheet CombinationQuestSheet { get; private set; }

        public TradeQuestSheet TradeQuestSheet { get; private set; }

        public ItemEnhancementQuestSheet ItemEnhancementQuestSheet { get; private set; }

        public GeneralQuestSheet GeneralQuestSheet { get; private set; }

        public SkillBuffSheet SkillBuffSheet { get; private set; }

        public MonsterQuestSheet MonsterQuestSheet { get; private set; }

        public ItemGradeQuestSheet ItemGradeQuestSheet { get; private set; }

        public ItemTypeCollectQuestSheet ItemTypeCollectQuestSheet { get; private set; }

        public GoldQuestSheet GoldQuestSheet { get; private set; }

        public EquipmentItemSetEffectSheet EquipmentItemSetEffectSheet { get; private set; }

        public EnemySkillSheet EnemySkillSheet { get; private set; }

        public ItemConfigForGradeSheet ItemConfigForGradeSheet { get; private set; }

        public QuestRewardSheet QuestRewardSheet { get; private set; }

        public QuestItemRewardSheet QuestItemRewardSheet { get; set; }

        public WorldUnlockSheet WorldUnlockSheet { get; set; }

        public StageDialogSheet StageDialogSheet { get; private set; }

        public EquipmentItemRecipeSheet EquipmentItemRecipeSheet { get; private set; }

        public EquipmentItemSubRecipeSheet EquipmentItemSubRecipeSheet { get; private set; }

        public EquipmentItemSubRecipeSheetV2 EquipmentItemSubRecipeSheetV2 { get; private set; }

        public EquipmentItemOptionSheet EquipmentItemOptionSheet { get; private set; }

        public GameConfigSheet GameConfigSheet { get; private set; }

        public RedeemRewardSheet RedeemRewardSheet { get; private set; }

        public RedeemCodeListSheet RedeemCodeListSheet { get; private set; }

        public CombinationEquipmentQuestSheet CombinationEquipmentQuestSheet { get; private set; }

        public EnhancementCostSheet EnhancementCostSheet { get; private set; }

        public EnhancementCostSheetV2 EnhancementCostSheetV2 { get; private set; }

        public WeeklyArenaRewardSheet WeeklyArenaRewardSheet { get; internal set; }

        public CostumeStatSheet CostumeStatSheet { get; private set; }

        public MonsterCollectionSheet MonsterCollectionSheet { get; private set; }

        public MonsterCollectionRewardSheet MonsterCollectionRewardSheet { get; private set; }

        public StakeRegularRewardSheet StakeRegularRewardSheet { get; private set; }

        public StakeRegularFixedRewardSheet StakeRegularFixedRewardSheet { get; private set; }

        public StakeAchievementRewardSheet StakeAchievementRewardSheet { get; private set; }

        public SweepRequiredCPSheet SweepRequiredCPSheet { get; private set; }

        public CrystalEquipmentGrindingSheet CrystalEquipmentGrindingSheet { get; private set; }

        public CrystalMonsterCollectionMultiplierSheet CrystalMonsterCollectionMultiplierSheet { get; private set; }

        public CrystalMaterialCostSheet CrystalMaterialCostSheet { get; private set; }

        public CrystalStageBuffGachaSheet CrystalStageBuffGachaSheet { get; private set; }

        public CrystalRandomBuffSheet CrystalRandomBuffSheet { get; private set; }

        public ArenaSheet ArenaSheet { get; private set; }

        public CrystalFluctuationSheet CrystalFluctuationSheet { get; private set; }

        public CrystalHammerPointSheet CrystalHammerPointSheet { get; private set; }

        public EventScheduleSheet EventScheduleSheet { get; private set; }

        public EventDungeonSheet EventDungeonSheet { get; private set; }

        public EventDungeonStageSheet EventDungeonStageSheet { get; private set; }

        public EventDungeonStageWaveSheet EventDungeonStageWaveSheet { get; private set; }

        public EventConsumableItemRecipeSheet EventConsumableItemRecipeSheet { get; private set; }

        public EventMaterialItemRecipeSheet EventMaterialItemRecipeSheet { get; private set; }

        public StakeActionPointCoefficientSheet StakeActionPointCoefficientSheet { get; private set; }

        public WorldBossListSheet WorldBossListSheet { get; private set; }

        public WorldBossRankRewardSheet WorldBossRankRewardSheet { get; private set; }

        public RuneWeightSheet RuneWeightSheet { get; private set; }

        public WorldBossGlobalHpSheet WorldBossGlobalHpSheet { get; private set; }

        public WorldBossCharacterSheet WorldBossCharacterSheet { get; private set; }

        public WorldBossActionPatternSheet WorldBossActionPatternSheet { get; private set; }

        public WorldBossBattleRewardSheet WorldBossBattleRewardSheet { get; private set; }

        public RuneSheet RuneSheet { get; private set; }

        public WorldBossKillRewardSheet WorldBossKillRewardSheet { get; private set; }

        public WorldBossRankingRewardSheet WorldBossRankingRewardSheet { get; private set; }

        public SkillActionBuffSheet SkillActionBuffSheet { get; private set; }

        public ActionBuffSheet ActionBuffSheet { get; private set; }

        public BuffSheet BuffSheet { get; private set; }

        public RuneListSheet RuneListSheet { get; private set; }

        public RuneCostSheet RuneCostSheet { get; private set; }

        public RuneOptionSheet RuneOptionSheet { get; private set; }

        public GrandFinaleScheduleSheet GrandFinaleScheduleSheet { get; private set; }

        public GrandFinaleParticipantsSheet GrandFinaleParticipantsSheet { get; private set; }

        public void ItemSheetInitialize()
        {
            ItemSheet ??= new ItemSheet();
            ItemSheet.Set(ConsumableItemSheet, false);
            ItemSheet.Set(CostumeItemSheet, false);
            ItemSheet.Set(EquipmentItemSheet, false);
            ItemSheet.Set(MaterialItemSheet);
        }

        public void QuestSheetInitialize()
        {
            QuestSheet ??= new QuestSheet();
            QuestSheet.Set(WorldQuestSheet, false);
            QuestSheet.Set(CollectQuestSheet, false);
            QuestSheet.Set(CombinationQuestSheet, false);
            QuestSheet.Set(TradeQuestSheet, false);
            QuestSheet.Set(MonsterQuestSheet, false);
            QuestSheet.Set(ItemEnhancementQuestSheet, false);
            QuestSheet.Set(GeneralQuestSheet, false);
            QuestSheet.Set(ItemGradeQuestSheet, false);
            QuestSheet.Set(ItemTypeCollectQuestSheet, false);
            QuestSheet.Set(GoldQuestSheet, false);
            QuestSheet.Set(CombinationEquipmentQuestSheet);
        }

        public SimulatorSheets GetSimulatorSheets()
        {
            return new SimulatorSheets(
                MaterialItemSheet,
                SkillSheet,
                SkillBuffSheet,
                StatBuffSheet,
                SkillActionBuffSheet,
                ActionBuffSheet,
                CharacterSheet,
                CharacterLevelSheet,
                EquipmentItemSetEffectSheet,
                RuneOptionSheet
            );
        }

        public SimulatorSheetsV1 GetSimulatorSheetsV1()
        {
            return new SimulatorSheetsV1(
                MaterialItemSheet,
                SkillSheet,
                SkillBuffSheet,
                StatBuffSheet,
                SkillActionBuffSheet,
                ActionBuffSheet,
                CharacterSheet,
                CharacterLevelSheet,
                EquipmentItemSetEffectSheet
            );
        }

        public StageSimulatorSheets GetStageSimulatorSheets()
        {
            return new StageSimulatorSheets(
                MaterialItemSheet,
                SkillSheet,
                SkillBuffSheet,
                StatBuffSheet,
                SkillActionBuffSheet,
                ActionBuffSheet,
                CharacterSheet,
                CharacterLevelSheet,
                EquipmentItemSetEffectSheet,
                StageSheet,
                StageWaveSheet,
                EnemySkillSheet,
                RuneOptionSheet
            );
        }

        public StageSimulatorSheetsV1 GetStageSimulatorSheetsV1()
        {
            return new StageSimulatorSheetsV1(
                MaterialItemSheet,
                SkillSheet,
                SkillBuffSheet,
                StatBuffSheet,
                SkillActionBuffSheet,
                ActionBuffSheet,
                CharacterSheet,
                CharacterLevelSheet,
                EquipmentItemSetEffectSheet,
                StageSheet,
                StageWaveSheet,
                EnemySkillSheet
            );
        }

        public RankingSimulatorSheetsV1 GetRankingSimulatorSheetsV1()
        {
            return new RankingSimulatorSheetsV1(
                MaterialItemSheet,
                SkillSheet,
                SkillBuffSheet,
                StatBuffSheet,
                SkillActionBuffSheet,
                ActionBuffSheet,
                CharacterSheet,
                CharacterLevelSheet,
                EquipmentItemSetEffectSheet,
                WeeklyArenaRewardSheet
            );
        }

        public RankingSimulatorSheets GetRankingSimulatorSheets()
        {
            return new RankingSimulatorSheets(
                MaterialItemSheet,
                SkillSheet,
                SkillBuffSheet,
                StatBuffSheet,
                SkillActionBuffSheet,
                ActionBuffSheet,
                CharacterSheet,
                CharacterLevelSheet,
                EquipmentItemSetEffectSheet,
                WeeklyArenaRewardSheet,
                RuneOptionSheet
            );
        }

        public ArenaSimulatorSheetsV1 GetArenaSimulatorSheetsV1()
        {
            return new ArenaSimulatorSheetsV1(
                MaterialItemSheet,
                SkillSheet,
                SkillBuffSheet,
                StatBuffSheet,
                SkillActionBuffSheet,
                ActionBuffSheet,
                CharacterSheet,
                CharacterLevelSheet,
                EquipmentItemSetEffectSheet,
                CostumeStatSheet,
                WeeklyArenaRewardSheet
            );
        }

        public ArenaSimulatorSheets GetArenaSimulatorSheets()
        {
            return new ArenaSimulatorSheets(
                MaterialItemSheet,
                SkillSheet,
                SkillBuffSheet,
                StatBuffSheet,
                SkillActionBuffSheet,
                ActionBuffSheet,
                CharacterSheet,
                CharacterLevelSheet,
                EquipmentItemSetEffectSheet,
                CostumeStatSheet,
                WeeklyArenaRewardSheet,
                RuneOptionSheet
            );
        }

        public RaidSimulatorSheetsV1 GetRaidSimulatorSheetsV1()
        {
            return new RaidSimulatorSheetsV1(
                MaterialItemSheet,
                SkillSheet,
                SkillBuffSheet,
                StatBuffSheet,
                SkillActionBuffSheet,
                ActionBuffSheet,
                CharacterSheet,
                CharacterLevelSheet,
                EquipmentItemSetEffectSheet,
                WorldBossCharacterSheet,
                WorldBossActionPatternSheet,
                WorldBossBattleRewardSheet,
                RuneWeightSheet,
                RuneSheet
            );
        }

        public RaidSimulatorSheets GetRaidSimulatorSheets()
        {
            return new RaidSimulatorSheets(
                MaterialItemSheet,
                SkillSheet,
                SkillBuffSheet,
                StatBuffSheet,
                SkillActionBuffSheet,
                ActionBuffSheet,
                CharacterSheet,
                CharacterLevelSheet,
                EquipmentItemSetEffectSheet,
                WorldBossCharacterSheet,
                WorldBossActionPatternSheet,
                WorldBossBattleRewardSheet,
                RuneWeightSheet,
                RuneSheet,
                RuneOptionSheet
            );
        }

        public AvatarSheets GetAvatarSheets()
        {
            return new AvatarSheets(
                WorldSheet,
                QuestSheet,
                QuestRewardSheet,
                QuestItemRewardSheet,
                EquipmentItemRecipeSheet,
                EquipmentItemSubRecipeSheet
            );
        }
    }
}
