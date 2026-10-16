using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Nekoyume.TableData;
using Nekoyume.TableData.AdventureBoss;
using Nekoyume.TableData.Crystal;
using Nekoyume.TableData.CustomEquipmentCraft;
using Nekoyume.TableData.Event;
using Nekoyume.TableData.Garages;
using Nekoyume.TableData.Pet;
using Nekoyume.TableData.Rune;
using Nekoyume.TableData.Stake;
using Nekoyume.TableData.Summon;

namespace Nekoyume.Game
{
    public class TableSheets
    {
        public static TableSheets Instance => Game.instance.TableSheets;

        private TableSheets()
        {
        }

        public static async UniTask<TableSheets> MakeTableSheetsAsync(IDictionary<string, string> sheets)
        {
            var previousContext = SynchronizationContext.Current;
            await UniTask.SwitchToThreadPool();

            var tableSheets = new TableSheets();
            tableSheets.Initialize(sheets);
            await UniTask.SwitchToSynchronizationContext(previousContext);

            return tableSheets;
        }

        public static TableSheets MakeTableSheets(IDictionary<string, string> sheets)
        {
            var tableSheets = new TableSheets();
            tableSheets.Initialize(sheets);
            return tableSheets;
        }

        private void Initialize(IDictionary<string, string> sheets)
        {
            var type = typeof(TableSheets);
            var started = DateTime.UtcNow;
            var sw = new Stopwatch();
            sw.Start();
            foreach (var pair in sheets)
            {
                var sheetPropertyInfo = type.GetProperty(pair.Key);
                if (sheetPropertyInfo is null)
                {
                    var sb = new StringBuilder($"[{nameof(TableSheets)}]");
                    sb.Append($" / ({pair.Key}, csv)");
                    sb.Append(" / failed to get property");
                    NcDebug.LogError(sb.ToString());
                    continue;
                }

                var sheetObject = Activator.CreateInstance(sheetPropertyInfo.PropertyType);
                var iSheet = (ISheet)sheetObject;
                if (iSheet is null)
                {
                    var sb = new StringBuilder($"[{nameof(TableSheets)}]");
                    sb.Append($" / ({pair.Key}, csv)");
                    sb.Append($" / failed to cast to {nameof(ISheet)}");
                    var errorMessage = sb.ToString();
                    NcDebug.LogError(errorMessage);
                    Game.QuitWithMessage("ERROR_INITIALIZE_FAILED", errorMessage);
                    throw new Exception(errorMessage);
                }

                if (!string.IsNullOrEmpty(pair.Value))
                {
                    try
                    {
                        iSheet.Set(pair.Value);
                    }
                    catch (Exception e)
                    {
                        var sb = new StringBuilder($"[{nameof(TableSheets)}]");
                        sb.Append($" / ({pair.Key}, csv)");
                        sb.Append($" / failed to set sheet data: {e.Message}");
                        var errorMessage = sb.ToString();
                        NcDebug.LogError(errorMessage);
                        Game.QuitWithMessage("ERROR_INITIALIZE_FAILED", errorMessage);
                        throw;
                    }
                }
                else
                {
                    var errorMessage = $"[{nameof(TableSheets)}] The value for {pair.Key} is empty.";
                    NcDebug.LogError(errorMessage);
                    Game.QuitWithMessage("ERROR_INITIALIZE_FAILED", errorMessage);
                }

                sheetPropertyInfo.SetValue(this, sheetObject);
            }

            ItemSheetInitialize();
            QuestSheetInitialize();
            sw.Stop();
            NcDebug.Log($"[{nameof(TableSheets)}] Initialize Total: {DateTime.UtcNow - started}");
        }

        public WorldSheet WorldSheet { get; private set; }

        public StageWaveSheet StageWaveSheet { get; private set; }

        public StageSheet StageSheet { get; private set; }

        public CharacterSheet CharacterSheet { get; private set; }

        public CharacterLevelSheet CharacterLevelSheet { get; private set; }

        public SkillSheet SkillSheet { get; private set; }

        public StatBuffSheet StatBuffSheet { get; private set; }

        public BuffSheet BuffSheet { get; private set; }

        public ItemSheet ItemSheet { get; private set; }

        public ItemRequirementSheet ItemRequirementSheet { get; private set; }

        public ConsumableItemSheet ConsumableItemSheet { get; private set; }

        public CostumeItemSheet CostumeItemSheet { get; private set; }

        public EquipmentItemSheet EquipmentItemSheet { get; private set; }

        public MaterialItemSheet MaterialItemSheet { get; private set; }

        public UnlockCombinationSlotCostSheet UnlockCombinationSlotCostSheet { get; private set; }

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

        public CombinationEquipmentQuestSheet CombinationEquipmentQuestSheet { get; private set; }

        public EnhancementCostSheet EnhancementCostSheet { get; private set; }

        public EnhancementCostSheetV2 EnhancementCostSheetV2 { get; private set; }

        public EnhancementCostSheetV3 EnhancementCostSheetV3 { get; private set; }

        public WeeklyArenaRewardSheet WeeklyArenaRewardSheet { get; private set; }

        public CostumeStatSheet CostumeStatSheet { get; private set; }

        public MimisbrunnrSheet MimisbrunnrSheet { get; private set; }

        public MonsterCollectionSheet MonsterCollectionSheet { get; private set; }

        public MonsterCollectionRewardSheet MonsterCollectionRewardSheet { get; private set; }

        public CrystalEquipmentGrindingSheet CrystalEquipmentGrindingSheet { get; private set; }

        public CrystalMonsterCollectionMultiplierSheet CrystalMonsterCollectionMultiplierSheet { get; private set; }

        public CrystalStageBuffGachaSheet CrystalStageBuffGachaSheet { get; private set; }

        public CrystalRandomBuffSheet CrystalRandomBuffSheet { get; private set; }

        public CrystalMaterialCostSheet CrystalMaterialCostSheet { get; private set; }

        public SweepRequiredCPSheet SweepRequiredCPSheet { get; private set; }

        public StakeRegularRewardSheet StakeRegularRewardSheet { get; private set; }

        public StakeRegularFixedRewardSheet StakeRegularFixedRewardSheet { get; private set; }

        public CrystalFluctuationSheet CrystalFluctuationSheet { get; private set; }
        public StakePolicySheet StakePolicySheet { get; private set; }
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

        public WorldBossKillRewardSheet WorldBossKillRewardSheet { get; private set; }

        public WorldBossRankingRewardSheet WorldBossRankingRewardSheet { get; private set; }

        public WorldBossGlobalHpSheet WorldBossGlobalHpSheet { get; private set; }

        public WorldBossCharacterSheet WorldBossCharacterSheet { get; private set; }

        public WorldBossActionPatternSheet WorldBossActionPatternSheet { get; private set; }

        public WorldBossBattleRewardSheet WorldBossBattleRewardSheet { get; private set; }

        [UsedImplicitly]
        public WorldBossContributionRewardSheet WorldBossContributionRewardSheet { get; private set; }

        public RuneWeightSheet RuneWeightSheet { get; private set; }

        public RuneSheet RuneSheet { get; private set; }

        public SkillActionBuffSheet SkillActionBuffSheet { get; private set; }

        public ActionBuffSheet ActionBuffSheet { get; private set; }
        public RuneListSheet RuneListSheet { get; private set; }
        public RuneCostSheet RuneCostSheet { get; private set; }
        public RuneOptionSheet RuneOptionSheet { get; private set; }
        public RuneLevelBonusSheet RuneLevelBonusSheet { get; private set; }
        public PetSheet PetSheet { get; private set; }
        public PetCostSheet PetCostSheet { get; private set; }
        public PetOptionSheet PetOptionSheet { get; private set; }

        public EquipmentSummonSheet EquipmentSummonSheet { get; private set; }
        public RuneSummonSheet RuneSummonSheet { get; private set; }
        public CostumeSummonSheet CostumeSummonSheet { get; private set; }

        public LoadIntoMyGaragesCostSheet LoadIntoMyGaragesCostSheet { get; private set; }

        public CreateAvatarItemSheet CreateAvatarItemSheet { get; private set; }

        public CreateAvatarFavSheet CreateAvatarFavSheet { get; private set; }

        public CollectionSheet CollectionSheet { get; private set; }

        public BuffLimitSheet BuffLimitSheet { get; private set; }

        public BuffLinkSheet BuffLinkSheet { get; private set; }

        public AdventureBossFloorSheet AdventureBossFloorSheet { get; private set; }

        public AdventureBossFloorWaveSheet AdventureBossFloorWaveSheet { get; private set; }

        public AdventureBossContributionRewardSheet AdventureBossContributionRewardSheet { get; private set; }

        public AdventureBossFloorFirstRewardSheet AdventureBossFloorFirstRewardSheet { get; private set; }

        public AdventureBossFloorPointSheet AdventureBossFloorPointSheet { get; private set; }

        public AdventureBossNcgRewardRatioSheet AdventureBossNcgRewardRatioSheet { get; private set; }

        public AdventureBossSheet AdventureBossSheet { get; private set; }

        public AdventureBossUnlockFloorCostSheet AdventureBossUnlockFloorCostSheet { get; private set; }

        public AdventureBossWantedRewardSheet AdventureBossWantedRewardSheet { get; private set; }

        public CustomEquipmentCraftIconSheet CustomEquipmentCraftIconSheet { get; private set; }
        public CustomEquipmentCraftOptionSheet CustomEquipmentCraftOptionSheet { get; private set; }
        public CustomEquipmentCraftRelationshipSheet CustomEquipmentCraftRelationshipSheet { get; private set; }
        public CustomEquipmentCraftRecipeSheet CustomEquipmentCraftRecipeSheet { get; private set; }
        public CustomEquipmentCraftRecipeSkillSheet CustomEquipmentCraftRecipeSkillSheet { get; private set; }
        public ClaimableGiftsSheet ClaimableGiftsSheet { get; private set; }

        public InfiniteTowerScheduleSheet InfiniteTowerScheduleSheet { get; private set; }

        public InfiniteTowerFloorSheet InfiniteTowerFloorSheet { get; private set; }

        public InfiniteTowerFloorWaveSheet InfiniteTowerFloorWaveSheet { get; private set; }

        public InfiniteTowerConditionSheet InfiniteTowerConditionSheet { get; private set; }

        [UsedImplicitly]
        public PatrolRewardSheet PatrolRewardSheet { get; private set; }

        [UsedImplicitly]
        public SynthesizeSheet SynthesizeSheet { get; private set; }

        [UsedImplicitly]
        public SynthesizeWeightSheet SynthesizeWeightSheet { get; private set; }

        /// <summary>
        /// 아이템·통화별 거래·합성 제한.
        /// </summary>
        /// <remarks>
        /// 행이 없는 것과 제한이 없는 것은 같다 — 조회 API(<c>IsItemMarketRegistrable</c> 등)가
        /// 미등재 키에 대해 "허용"을 돌려주므로, 호출부는 비었는지 따로 보지 않고 그냥 물어도
        /// 된다. 정책이 실제로 실려 있는지 확인하려면 <c>Values.Any()</c> 로 보면 된다.
        ///
        /// 체인에 patch_table_sheet 된 적이 없어도 이 프로퍼티는 채워진다 —
        /// <c>Game.SyncTableSheetsAsync</c> 가 노드에 없는 시트는 번들 CSV 를 기본값으로 쓴다.
        /// 그 폴백이 없으면 빈 값이 내려와 <c>Initialize</c> 가 ERROR_INITIALIZE_FAILED 로
        /// 부팅을 끊는다.
        /// </remarks>
        [UsedImplicitly]
        public RestrictionSheet RestrictionSheet { get; private set; }

        /// <summary>
        /// 기간 한정 보상·비용 배율(블록 구간). 액션이 같은 시트로 소환 보장·합성 재료·룬 강화 비용·
        /// 월드보스·어드벤처보스·스테이지 보상을 조정하므로, 클라가 결과를 다시 계산하는 곳(소환 팝업,
        /// 전투 보상, 비용 표시)은 <c>FindActive(target, id, blockIndex)</c> 로 찾은 행을 넘겨야 한다.
        /// </summary>
        /// <remarks>
        /// 체인에 patch 된 적이 없으면 번들의 빈 시트(헤더만)가 들어가 아무 행도 매칭되지 않는다.
        /// 세션 중 patch 되면 <c>ActionRenderHandler</c> 가, 재접속·부팅 직후에는 <c>BlockRenderHandler</c> 가 노드에서
        /// 다시 받아 <see cref="ReplaceBoostScheduleSheet"/> 로 갈아끼운다. 액션 결과를 재현할 때는 이 속성 대신
        /// 액션이 실제로 본 시트(<c>BoostScheduleFrontHelper.GetSheetForAction</c>)를 써야 한다 — 패치 블록에서는
        /// 둘이 다르다.
        /// </remarks>
        [UsedImplicitly]
        public BoostScheduleSheet BoostScheduleSheet { get; private set; }

        private readonly object _boostScheduleSheetLock = new();

        /// <summary>지금 <see cref="BoostScheduleSheet"/> 의 원문(줄바꿈·앞뒤 공백 정리). 노드에서 다시 받은 값이 바뀌었는지 본다.</summary>
        private string _boostScheduleSheetCsv;

        /// <summary>
        /// 지금 <see cref="BoostScheduleSheet"/> 가 들어온 블록. 이 블록 "다음" 블록부터의 액션만 이 시트를 봤다고
        /// 확신할 수 있다 — 이 블록 이하의 액션은 (같은 블록에서 패치보다 먼저 실행됐거나 그 전 블록이라) 이전 시트를
        /// 봤을 수 있다. <see cref="GetBoostScheduleSheet"/> 참고.
        /// </summary>
        private long _boostScheduleSheetChangedAt;

        /// <summary>
        /// 패치 렌더를 놓쳤을 수 있는 구간(부팅·재접속 직후)이라 노드 재조회로 확인하기 전인지. 이 동안은 지금 시트를
        /// 믿지 않고 결과 재현이 모두 액션 직전 상태에서 시트를 읽게 한다.
        /// </summary>
        private bool _boostScheduleSheetUnverified;

        /// <summary>확인 요청 세대. 진행 중이던 재조회가 그 뒤에 들어온 요청까지 확인한 것으로 지우지 않게 한다.</summary>
        private long _boostScheduleSheetVerifyGeneration;

        /// <summary>지금 확인 요청 세대. 재조회를 시작할 때 읽어 두었다가 <see cref="MarkBoostScheduleSheetVerified"/> 에 넘긴다.</summary>
        public long BoostScheduleSheetVerifyGeneration
        {
            get
            {
                lock (_boostScheduleSheetLock)
                {
                    return _boostScheduleSheetVerifyGeneration;
                }
            }
        }

        /// <summary>
        /// 부팅 때 읽은 시트의 기준점을 기록한다. <paramref name="blockIndex"/> 는 그 값을 읽은 tip(이하) 이다.
        /// </summary>
        public void SetBoostScheduleSheetOrigin(string csv, long blockIndex)
        {
            lock (_boostScheduleSheetLock)
            {
                _boostScheduleSheetCsv = NormalizeBoostScheduleCsv(csv);
                _boostScheduleSheetChangedAt = blockIndex;
            }
        }

        /// <summary>
        /// BoostScheduleSheet 를 <paramref name="blockIndex"/> 블록의 체인 값으로 교체한다. 부팅 때만 읽으면 이벤트
        /// 행이 재시작 전까지 클라 계산에 빠져, 지급과 다른 결과·비용을 보여준다.
        /// </summary>
        /// <param name="csv">체인에 들어간 CSV.</param>
        /// <param name="blockIndex">
        /// 그 값을 본 마지막 블록: 패치 렌더면 패치 tx 의 블록, 노드 재조회면 조회한 상태(tip 의 StateRootHash = tip 직전
        /// 블록까지 실행한 결과)의 블록, 즉 tip - 1.
        /// </param>
        /// <returns>
        /// 교체했는지. 지금 시트보다 오래된 정보(재조회를 시작한 뒤 패치 렌더가 먼저 반영된 경우)이거나 내용이
        /// 같으면 교체하지 않는다.
        /// </returns>
        public bool ReplaceBoostScheduleSheet(string csv, long blockIndex)
        {
            var normalized = NormalizeBoostScheduleCsv(csv);
            lock (_boostScheduleSheetLock)
            {
                if (blockIndex < _boostScheduleSheetChangedAt ||
                    string.Equals(normalized, _boostScheduleSheetCsv, StringComparison.Ordinal))
                {
                    return false;
                }

                var sheet = new BoostScheduleSheet();
                sheet.Set(csv);
                BoostScheduleSheet = sheet;
                _boostScheduleSheetCsv = normalized;
                _boostScheduleSheetChangedAt = blockIndex;
                return true;
            }
        }

        /// <summary>
        /// 지금 시트와, 그 시트가 들어온 블록을 함께 읽는다(교체와 원자적으로).
        /// </summary>
        /// <param name="changedAt">
        /// 이 블록 이하의 액션은 이전 시트를 봤을 수 있다. 그런 액션의 결과를 재현할 때는 액션 직전 상태에서
        /// 시트를 다시 읽어야 한다(<c>BoostScheduleFrontHelper.GetSheetForAction</c>).
        /// </param>
        public BoostScheduleSheet GetBoostScheduleSheet(out long changedAt)
        {
            lock (_boostScheduleSheetLock)
            {
                changedAt = _boostScheduleSheetUnverified ? long.MaxValue : _boostScheduleSheetChangedAt;
                return BoostScheduleSheet;
            }
        }

        /// <summary>
        /// 노드 재조회로 지금 시트를 확인할 때까지 결과 재현이 지금 시트를 쓰지 않게 한다(부팅·재접속 직후).
        /// </summary>
        public void MarkBoostScheduleSheetUnverified()
        {
            lock (_boostScheduleSheetLock)
            {
                _boostScheduleSheetUnverified = true;
                _boostScheduleSheetVerifyGeneration++;
            }
        }

        /// <summary>
        /// 노드 재조회로 지금 시트를 확인했다. 재조회를 시작한 뒤 새 확인 요청(예: 재접속)이 들어왔으면 그 요청은 아직
        /// 확인되지 않았으므로 그대로 둔다.
        /// </summary>
        /// <param name="generation">재조회를 시작할 때 읽은 <see cref="BoostScheduleSheetVerifyGeneration"/>.</param>
        public void MarkBoostScheduleSheetVerified(long generation)
        {
            lock (_boostScheduleSheetLock)
            {
                if (generation == _boostScheduleSheetVerifyGeneration)
                {
                    _boostScheduleSheetUnverified = false;
                }
            }
        }

        /// <summary>노드가 시트 해시를 낼 때와 같은 정규화(<c>Game.NormalizeText</c>).</summary>
        private static string NormalizeBoostScheduleCsv(string csv) =>
            csv?.Replace("\r\n", "\n").Trim() ?? string.Empty;

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
                RuneOptionSheet,
                RuneListSheet,
                RuneLevelBonusSheet
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
                RuneOptionSheet,
                RuneListSheet,
                RuneLevelBonusSheet
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
                RuneOptionSheet,
                RuneListSheet,
                RuneLevelBonusSheet
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
                RuneOptionSheet,
                RuneListSheet,
                RuneLevelBonusSheet
            );
        }
    }
}
