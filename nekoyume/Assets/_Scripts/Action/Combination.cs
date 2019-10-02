using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DecimalMath;
using Libplanet;
using Libplanet.Action;
using Nekoyume.BlockChain;
using Nekoyume.Data;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Mail;
using Nekoyume.Model;
using Nekoyume.State;
using Nekoyume.TableData;
using UnityEngine;
using Skill = Nekoyume.Game.Skill;

namespace Nekoyume.Action
{
    [ActionType("combination")]
    public class Combination : GameAction
    {
        [Serializable]
        public class Material
        {
            public int id;
            public int count;

            public Material(UI.Model.CountableItem item) : this(item.item.Value.Data.Id, item.count.Value)
            {
            }

            public Material(int id, int count)
            {
                this.id = id;
                this.count = count;
            }
        }

        [Serializable]
        public class Result : AttachmentActionResult
        {
            public List<Material> materials;
        }

        public class MaterialRow
        {
            public MaterialItemSheet.Row row;
            public int count;

            public MaterialRow(Material material)
            {
                var sheet = Game.Game.instance.TableSheets.MaterialItemSheet;
                if (!sheet.TryGetValue(material.id, out var outRow))
                {
                    throw new SheetRowNotFoundException(nameof(sheet), material.id.ToString());
                }

                row = outRow;
                count = material.count;
            }
        }

        public List<Material> Materials { get; private set; }
        public Address avatarAddress;
        public Result result;

        protected override IImmutableDictionary<string, object> PlainValueInternal =>
            new Dictionary<string, object>
            {
                ["Materials"] = ByteSerializer.Serialize(Materials),
                ["avatarAddress"] = avatarAddress.ToByteArray(),
            }.ToImmutableDictionary();

        public Combination()
        {
            Materials = new List<Material>();
        }

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            Materials = ByteSerializer.Deserialize<List<Material>>((byte[]) plainValue["Materials"]);
            avatarAddress = new Address((byte[]) plainValue["avatarAddress"]);
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(avatarAddress, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }

            var agentState = (AgentState) states.GetState(ctx.Signer);
            if (!agentState.avatarAddresses.ContainsValue(avatarAddress))
                return states;

            var avatarState = (AvatarState) states.GetState(avatarAddress);
            if (avatarState == null)
                return states;

            Debug.Log($"Execute Combination. player : `{avatarAddress}` " +
                      $"node : `{States.Instance?.agentState?.Value?.address}` " +
                      $"current avatar: `{States.Instance?.currentAvatarState?.Value?.address}`");

            // 사용한 재료를 인벤토리에서 제거.
            foreach (var material in Materials)
            {
                if (!avatarState.inventory.RemoveFungibleItem(material.id, material.count))
                    return states;
            }

            // 액션 결과
            result = new Result
            {
                materials = Materials,
            };

            // 조합식 테이블 로드.
            var recipeTable = Tables.instance.Recipe;

            // 모든 재료를 테이블 값으로.
            var materialRows = Materials.Select(material => new MaterialRow(material)).ToList();
            var isEquipment = materialRows.Any(row => row.row.ItemSubType == ItemSubType.EquipmentMaterial);
            if (isEquipment)
            {
                var zippedMaterialRows = Zip(materialRows);
                var equipmentMaterials = zippedMaterialRows
                    .Where(materialRow => materialRow.row.ItemSubType == ItemSubType.EquipmentMaterial)
                    .ToList();
                if (equipmentMaterials.Count == 0 || equipmentMaterials.Count > 1)
                {
                    // 장비 베이스의 수량 에러.
                    return states;
                }

                var equipmentMaterial = equipmentMaterials[0];
                if (!TryGetItemType(equipmentMaterial.row.Id, out var outItemType))
                {
                    // 장비 베이스의 Id로 장비의 타입을 추측할 수 없는 에러.
                    return states;
                }

                var monsterParts = zippedMaterialRows
                    .Where(materialRow => materialRow.row.ItemSubType != ItemSubType.EquipmentMaterial)
                    .ToList();
                if (monsterParts.Count == 0 || monsterParts.Count > 4)
                {
                    // 몬스터 파츠의 수량 에러.
                    return states;
                }

                var elementalType = GetElementalType(monsterParts, ctx.Random);

                var itemId = ctx.Random.GenerateRandomGuid();
                Equipment equipment = null;
                var isFirst = true;
                foreach (var monsterPart in monsterParts)
                {
                    if (isFirst)
                    {
                        isFirst = false;

                        if (!TryGetItemEquipmentRow(outItemType, elementalType, equipmentMaterial.row.Grade,
                            out var itemEquipmentRow))
                        {
                            // 장비 테이블 값 가져오기 실패.
                            return states;
                        }

                        try
                        {
                            equipment = (Equipment) ItemFactory.Create(itemEquipmentRow, itemId);
                        }
                        catch (ArgumentOutOfRangeException e)
                        {
                            // 장비 생성 실패.
                            Debug.LogException(e);

                            return states;
                        }
                    }

                    var normalizedRandomValue = ctx.Random.Next(0, 100000) * 0.00001m;
                    var roll = GetRoll(monsterPart.count, 0, normalizedRandomValue);

                    if (TryGetStat(monsterPart.row, roll, out var statMap))
                        equipment.Stats.AddStatAdditionalValue(statMap.StatType, statMap.Value);

                    if (TryGetSkill(monsterPart.row, roll, out var skill))
                        equipment.Skills.Add(skill);
                }

                result.itemUsable = equipment;
                var mail = new CombinationMail(result, ctx.BlockIndex);
                avatarState.Update(mail);
                avatarState.questList.UpdateCombinationQuest(equipment);
            }
            else
            {
                var foodMaterials = materialRows.OrderBy(order => order.row.Id).ToList();
                ConsumableItemSheet.Row itemEquipmentRow = null;
                // 소모품
                foreach (var recipe in recipeTable)
                {
                    if (!recipe.Value.IsMatchForConsumable(foodMaterials))
                        continue;

                    if (!Game.Game.instance.TableSheets.ConsumableItemSheet.TryGetValue(recipe.Value.ResultId,
                        out itemEquipmentRow))
                        break;

                    if (recipe.Value.GetCombinationResultCountForConsumable(foodMaterials) == 0)
                        break;
                }

                if (itemEquipmentRow == null &&
                    !Game.Game.instance.TableSheets.ConsumableItemSheet
                        .TryGetValue(GameConfig.CombinationDefaultFoodId, out itemEquipmentRow))
                {
                    // 소모품 테이블 값 가져오기 실패.
                    return states;
                }

                // 조합 결과 획득.
                var itemId = ctx.Random.GenerateRandomGuid();
                var itemUsable = GetFood(itemEquipmentRow, itemId);
                result.itemUsable = itemUsable;
                var mail = new CombinationMail(result, ctx.BlockIndex);
                avatarState.Update(mail);
                avatarState.questList.UpdateCombinationQuest(itemUsable);
            }

            avatarState.updatedAt = DateTimeOffset.UtcNow;
            avatarState.BlockIndex = ctx.BlockIndex;
            states = states.SetState(avatarAddress, avatarState);
            return states.SetState(ctx.Signer, agentState);
        }

        private static List<MaterialRow> Zip(IEnumerable<MaterialRow> materialRows)
        {
            var zippedMaterialRows = new List<MaterialRow>();
            foreach (var source in materialRows)
            {
                var shouldToAdd = true;
                foreach (var target in zippedMaterialRows.Where(target => target.row.Id == source.row.Id))
                {
                    target.count += source.count;
                    shouldToAdd = false;
                    break;
                }

                if (shouldToAdd)
                {
                    zippedMaterialRows.Add(source);
                }
            }

            return zippedMaterialRows;
        }

        private static ElementalType GetElementalType(IEnumerable<MaterialRow> monsterParts, IRandom random)
        {
            var elementalTypeCountForEachGrades =
                new Dictionary<ElementalType, Dictionary<int, int>>(ElementalTypeComparer.Instance);
            var maxGrade = 0;

            // 전체 속성 가중치가 가장 큰 것을 리턴하기.
            var elementalTypeWeights = new Dictionary<ElementalType, int>(ElementalTypeComparer.Instance);
            var maxWeightElementalTypes = new List<ElementalType>();
            var maxWeight = 0;

            foreach (var monsterPart in monsterParts)
            {
                var key = monsterPart.row.ElementalType;
                var grade = Math.Max(1, monsterPart.row.Grade);
                if (grade > maxGrade)
                {
                    maxGrade = grade;
                }

                if (!elementalTypeCountForEachGrades.ContainsKey(key))
                {
                    elementalTypeCountForEachGrades[key] = new Dictionary<int, int>();
                }

                if (!elementalTypeCountForEachGrades[key].ContainsKey(grade))
                {
                    elementalTypeCountForEachGrades[key][grade] = 0;
                }
                
                elementalTypeCountForEachGrades[key][grade] += monsterPart.count;

                var weight = (int) Math.Pow(10, grade - 1) * monsterPart.count;

                if (!elementalTypeWeights.ContainsKey(key))
                {
                    elementalTypeWeights[key] = 0;
                }
                
                elementalTypeWeights[key] += weight;

                var totalWeight = elementalTypeWeights[key];
                if (totalWeight < maxWeight)
                    continue;

                if (totalWeight == maxWeight &&
                    !maxWeightElementalTypes.Contains(key))
                {
                    maxWeightElementalTypes.Add(key);

                    continue;
                }

                maxWeightElementalTypes.Clear();
                maxWeightElementalTypes.Add(key);
                maxWeight = totalWeight;
            }

            if (maxWeightElementalTypes.Count == 1)
                return maxWeightElementalTypes[0];

            // 높은 등급의 재료를 더 많이 갖고 있는 것을 리턴하기.
            var maxGradeCountElementalTypes = new List<ElementalType>();
            var maxGradeCount = 0;
            foreach (var elementalType in maxWeightElementalTypes)
            {
                if (!elementalTypeCountForEachGrades[elementalType].ContainsKey(maxGrade))
                    continue;

                var gradeCount = elementalTypeCountForEachGrades[elementalType][maxGrade];
                if (gradeCount < maxGradeCount)
                    continue;

                if (gradeCount == maxGradeCount &&
                    !maxGradeCountElementalTypes.Contains(elementalType))
                {
                    maxGradeCountElementalTypes.Add(elementalType);

                    continue;
                }

                maxGradeCountElementalTypes.Clear();
                maxGradeCountElementalTypes.Add(elementalType);
                maxGradeCount = gradeCount;
            }

            if (maxGradeCountElementalTypes.Count == 1)
                return maxGradeCountElementalTypes[0];

            // 무작위로 하나 고르기.
            var index = random.Next(0, maxGradeCountElementalTypes.Count);
            // todo: libplanet 에서 max 값 -1까지만 리턴하도록 수정된 후에 삭제.
            if (index == maxGradeCountElementalTypes.Count)
            {
                index--;
            }
            return maxGradeCountElementalTypes[index];
        }

        private static bool TryGetItemType(int itemId, out ItemSubType outItemType)
        {
            var type = itemId.ToString().Substring(0, 4);
            switch (type)
            {
                case "3030":
                    outItemType = ItemSubType.Weapon;
                    return true;
                case "3031":
                    outItemType = ItemSubType.Armor;
                    return true;
                case "3032":
                    outItemType = ItemSubType.Belt;
                    return true;
                case "3033":
                    outItemType = ItemSubType.Necklace;
                    return true;
                case "3034":
                    outItemType = ItemSubType.Ring;
                    return true;
                default:
                    outItemType = ItemSubType.Armor;
                    return false;
            }
        }

        private static bool TryGetItemEquipmentRow(ItemSubType itemSubType, ElementalType elementalType,
            int grade, out EquipmentItemSheet.Row outItemEquipmentRow)
        {
            foreach (var row in Game.Game.instance.TableSheets.EquipmentItemSheet)
            {
                if (row.ItemSubType != itemSubType ||
                    row.ElementalType != elementalType ||
                    row.Grade != grade)
                    continue;

                outItemEquipmentRow = row;
                return true;
            }

            outItemEquipmentRow = null;

            return false;
        }

        private static decimal GetRoll(int monsterPartsCount, int deltaLevel, decimal normalizedRandomValue)
        {
            var rollMax = DecimalEx.Pow(1m / (1m + GameConfig.CombinationValueP1 / monsterPartsCount),
                              GameConfig.CombinationValueP2) *
                          (deltaLevel <= 0
                              ? 1m
                              : DecimalEx.Pow(1m / (1m + GameConfig.CombinationValueL1 / deltaLevel),
                                  GameConfig.CombinationValueL2));
            var rollMin = rollMax * 0.7m;
            return rollMin + (rollMax - rollMin) *
                   DecimalEx.Pow(normalizedRandomValue, GameConfig.CombinationValueR1);
        }

        private static bool TryGetStat(MaterialItemSheet.Row itemRow, decimal roll, out StatMap statMap)
        {
            if (!itemRow.StatType.HasValue)
            {
                statMap = null;

                return false;
            }

            var key = itemRow.StatType.Value;
            var value = Math.Floor(itemRow.StatMin + (itemRow.StatMax - itemRow.StatMin) * roll);
            statMap = new StatMap(key, value);
            return true;
        }

        public static bool TryGetSkill(MaterialItemSheet.Row monsterParts, decimal roll, out Skill skill)
        {
            try
            {
                var skillRow =
                    Game.Game.instance.TableSheets.SkillSheet.OrderedList.First(r => r.Id == monsterParts.SkillId);
                var chance = Math.Floor(monsterParts.SkillChanceMin +
                                        (monsterParts.SkillChanceMax - monsterParts.SkillChanceMin) * roll);
                chance = Math.Max(monsterParts.SkillChanceMin, chance);
                var value = (int) Math.Floor(monsterParts.SkillDamageMin +
                                             (monsterParts.SkillDamageMax - monsterParts.SkillDamageMin) * roll);

                skill = SkillFactory.Get(skillRow, value, chance);

                return true;
            }
            catch (InvalidOperationException)
            {
                skill = null;

                return false;
            }
        }

        private static ItemUsable GetFood(ConsumableItemSheet.Row equipmentItemRow, Guid itemId)
        {
            // FixMe. 소모품에 랜덤 스킬을 할당했을 때, `HackAndSlash` 액션에서 예외 발생. 그래서 소모품은 랜덤 스킬을 할당하지 않음.
            /*
             * InvalidTxSignatureException: 8383de6800f00416bfec1be66745895134083b431bd48766f1f6c50b699f6708: The signature (3045022100c2fffb0e28150fd6ddb53116cc790f15ca595b19ba82af8c6842344bd9f6aae10220705c37401ff35c3eb471f01f384ea6a110dd7e192d436ca99b91c9bed9b6db17) is failed to verify.
             * Libplanet.Tx.Transaction`1[T].Validate () (at <7284bf7c1f1547329a0963c7fa3ab23e>:0)
             * Libplanet.Blocks.Block`1[T].Validate (System.DateTimeOffset currentTime) (at <7284bf7c1f1547329a0963c7fa3ab23e>:0)
             * Libplanet.Store.BlockSet`1[T].set_Item (Libplanet.HashDigest`1[T] key, Libplanet.Blocks.Block`1[T] value) (at <7284bf7c1f1547329a0963c7fa3ab23e>:0)
             * Libplanet.Blockchain.BlockChain`1[T].Append (Libplanet.Blocks.Block`1[T] block, System.DateTimeOffset currentTime, System.Boolean render) (at <7284bf7c1f1547329a0963c7fa3ab23e>:0)
             * Libplanet.Blockchain.BlockChain`1[T].Append (Libplanet.Blocks.Block`1[T] block, System.DateTimeOffset currentTime) (at <7284bf7c1f1547329a0963c7fa3ab23e>:0)
             * Libplanet.Blockchain.BlockChain`1[T].MineBlock (Libplanet.Address miner, System.DateTimeOffset currentTime) (at <7284bf7c1f1547329a0963c7fa3ab23e>:0)
             * Libplanet.Blockchain.BlockChain`1[T].MineBlock (Libplanet.Address miner) (at <7284bf7c1f1547329a0963c7fa3ab23e>:0)
             * Nekoyume.BlockChain.Agent+<>c__DisplayClass31_0.<CoMiner>b__0 () (at Assets/_Scripts/BlockChain/Agent.cs:168)
             * System.Threading.Tasks.Task`1[TResult].InnerInvoke () (at <1f0c1ef1ad524c38bbc5536809c46b48>:0)
             * System.Threading.Tasks.Task.Execute () (at <1f0c1ef1ad524c38bbc5536809c46b48>:0)
             * UnityEngine.Debug:LogException(Exception)
             * Nekoyume.BlockChain.<CoMiner>d__31:MoveNext() (at Assets/_Scripts/BlockChain/Agent.cs:208)
             * UnityEngine.SetupCoroutine:InvokeMoveNext(IEnumerator, IntPtr)
             */
            return (ItemUsable) ItemFactory.Create(equipmentItemRow, itemId);
        }
    }
}
