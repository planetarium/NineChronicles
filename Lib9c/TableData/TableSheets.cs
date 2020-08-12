using System;
using System.Text;
using Libplanet.Action;
using LruCacheNet;
using Nekoyume.Model.State;

namespace Nekoyume.TableData
{
    /// <summary>
    /// 어드레서블어셋에 새로운 테이블을 추가하면 AddressableAssetsContainer.asset에도 해당 csv파일을 추가해줘야합니다.
    /// </summary>
    [Serializable]
    public class TableSheets
    {
        private static readonly LruCache<TableSheetsState, TableSheets> Cache =
            new LruCache<TableSheetsState, TableSheets>();

        #region sheets
        public WorldSheet WorldSheet { get; private set; }
        public StageWaveSheet StageWaveSheet { get; private set; }
        public StageSheet StageSheet { get; private set; }
        public CharacterSheet CharacterSheet { get; private set; }
        public CharacterLevelSheet CharacterLevelSheet { get; private set; }
        public SkillSheet SkillSheet { get; private set; }
        public BuffSheet BuffSheet { get; private set; }
        public ItemSheet ItemSheet { get; private set; }
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
        public EquipmentItemOptionSheet EquipmentItemOptionSheet { get; private set; }
        public GameConfigSheet GameConfigSheet { get; private set; }
        public RedeemRewardSheet RedeemRewardSheet { get; private set; }
        public RedeemCodeListSheet RedeemCodeListSheet { get; private set; }
        public CombinationEquipmentQuestSheet CombinationEquipmentQuestSheet { get; private set; }
        public EnhancementCostSheet EnhancementCostSheet { get; private set; }
        public WeeklyArenaRewardSheet WeeklyArenaRewardSheet { get; private set; }

        #endregion

        public void SetToSheet(string name, string csv)
        {
            var sheetPropertyInfo = GetType().GetProperty(name);
            if (sheetPropertyInfo is null)
            {
                var sb = new StringBuilder($"[{nameof(TableSheets)}]");
                sb.Append($" / {nameof(SetToSheet)}({name}, csv)");
                sb.Append(" / failed to get property");
                throw new Exception(sb.ToString());
            }

            var sheetObject = Activator.CreateInstance(sheetPropertyInfo.PropertyType);
            var iSheet = (ISheet) sheetObject;
            if (iSheet is null)
            {
                var sb = new StringBuilder($"[{nameof(TableSheets)}]");
                sb.Append($" / {nameof(SetToSheet)}({name}, csv)");
                sb.Append($" / failed to cast to {nameof(ISheet)}");
                throw new Exception(sb.ToString());
            }

            iSheet.Set(csv);
            sheetPropertyInfo.SetValue(this, sheetObject);
        }

        /// <summary>
        /// TableSheetsState를 기준으로 초기화합니다.
        /// </summary>
        /// <param name="tableSheetsState">기준으로 삼을 상태입니다.</param>
        public void InitializeWithTableSheetsState(TableSheetsState tableSheetsState)
        {
            foreach (var pair in tableSheetsState.TableSheets)
            {
                SetToSheet(pair.Key, pair.Value);
            }

            ItemSheetInitialize();
            QuestSheetInitialize();
        }

        public static TableSheets FromTableSheetsState(TableSheetsState tableSheetsState)
        {
            if (Cache.TryGetValue(tableSheetsState, out var cached))
            {
                return cached;
            }

            var tableSheets = new TableSheets();
            tableSheets.InitializeWithTableSheetsState(tableSheetsState);

            Cache.Add(tableSheetsState, tableSheets);
            return tableSheets;
        }

        public static TableSheets FromActionContext(IActionContext ctx)
        {
            var tableSheetsState = TableSheetsState.FromActionContext(ctx);
            return FromTableSheetsState(tableSheetsState);
        }


        public void ItemSheetInitialize()
        {
            ItemSheet = new ItemSheet();
            ItemSheet.Set(ConsumableItemSheet, false);
            ItemSheet.Set(CostumeItemSheet, false);
            ItemSheet.Set(EquipmentItemSheet, false);
            ItemSheet.Set(MaterialItemSheet);
        }

        public void QuestSheetInitialize()
        {
            QuestSheet = new QuestSheet();
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
    }
}
