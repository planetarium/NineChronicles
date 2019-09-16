using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DecimalMath;
using Libplanet;
using Libplanet.Action;
using Nekoyume.BlockChain;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Mail;
using Nekoyume.Model;
using Nekoyume.State;
using UnityEngine;
using Elemental = Nekoyume.Data.Table.Elemental;
using Skill = Nekoyume.Game.Skill;

namespace Nekoyume.Action
{
    [ActionType("combination")]
    public class Combination : GameAction
    {
        [Serializable]
        public struct Material
        {
            public int id;
            public int count;

            public Material(UI.Model.CountableItem item)
            {
                id = item.item.Value.Data.id;
                count = item.count.Value;
            }
        }

        [Serializable]
        public class Result : AttachmentActionResult
        {
            public List<Material> materials;
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

            // 장비인지 소모품인지 확인.
            var equipmentMaterials = Materials.Where(item => GameConfig.EquipmentMaterials.Contains(item.id))
                .ToList();
            if (equipmentMaterials.Any())
            {
                // 장비
                var orderedMaterials = Materials.OrderByDescending(order => order.count).ToList();
                orderedMaterials.RemoveAll(item => equipmentMaterials.Contains(item));
                if (orderedMaterials.Count == 0)
                    return states;

                var equipmentMaterial = equipmentMaterials[0];
                if (!Tables.instance.Item.TryGetValue(equipmentMaterial.id, out var outEquipmentMaterialRow))
                    return states;

                if (!TryGetItemType(outEquipmentMaterialRow.id, out var outItemType))
                    return states;

                var itemId = ctx.Random.GenerateUUID4();
                Equipment equipment = null;
                var isFirst = true;
                foreach (var monsterPartsMaterial in orderedMaterials)
                {
                    if (!Tables.instance.Item.TryGetValue(monsterPartsMaterial.id, out var outMonsterPartsMaterialRow))
                        return states;

                    if (isFirst)
                    {
                        isFirst = false;

                        if (!TryGetItemEquipmentRow(outItemType, outMonsterPartsMaterialRow.elemental,
                            outEquipmentMaterialRow.grade,
                            out var itemEquipmentRow))
                            return states;

                        try
                        {
                            equipment = (Equipment) ItemFactory.Create(itemEquipmentRow, itemId);
                        }
                        catch (ArgumentOutOfRangeException e)
                        {
                            Debug.LogException(e);

                            return states;
                        }
                    }

                    var normalizedRandomValue = ctx.Random.Next(0, 100000) * 0.00001m;
                    var roll = GetRoll(monsterPartsMaterial.count, 0, normalizedRandomValue);

                    if (TryGetStat(outMonsterPartsMaterialRow, roll, out var statMap))
                        equipment.Stats.SetStatAdditionalValue(statMap.Key, statMap.Value);

                    if (TryGetSkill(outMonsterPartsMaterialRow, roll, out var skill))
                        equipment.Skills.Add(skill);
                }

                result.itemUsable = equipment;
                var mail = new CombinationMail(result, ctx.BlockIndex);
                avatarState.Update(mail);
                avatarState.questList.UpdateCombinationQuest(equipment);
            }
            else
            {
                var orderedMaterials = Materials.OrderBy(order => order.id).ToList();
                ItemEquipment itemEquipmentRow = null;
                // 소모품
                foreach (var recipe in recipeTable)
                {
                    if (!recipe.Value.IsMatchForConsumable(orderedMaterials))
                        continue;

                    if (!Tables.instance.ItemEquipment.TryGetValue(recipe.Value.ResultId, out itemEquipmentRow))
                        break;

                    if (recipe.Value.GetCombinationResultCountForConsumable(orderedMaterials) == 0)
                        break;
                }

                if (itemEquipmentRow == null &&
                    !Tables.instance.ItemEquipment.TryGetValue(GameConfig.CombinationDefaultFoodId,
                        out itemEquipmentRow))
                    return states;

                // 조합 결과 획득.
                var itemId = ctx.Random.GenerateUUID4();
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

        private bool TryGetItemType(int itemId, out ItemBase.ItemType outItemType)
        {
            var type = itemId.ToString().Substring(0, 4);
            switch (type)
            {
                case "3030":
                    outItemType = ItemBase.ItemType.Weapon;
                    return true;
                case "3031":
                    outItemType = ItemBase.ItemType.Armor;
                    return true;
                case "3032":
                    outItemType = ItemBase.ItemType.Belt;
                    return true;
                case "3033":
                    outItemType = ItemBase.ItemType.Necklace;
                    return true;
                case "3034":
                    outItemType = ItemBase.ItemType.Ring;
                    return true;
                default:
                    outItemType = ItemBase.ItemType.Material;
                    return false;
            }
        }

        private static bool TryGetItemEquipmentRow(ItemBase.ItemType itemType, Elemental.ElementalType elementalType,
            int grade, out ItemEquipment outItemEquipmentRow)
        {
            foreach (var pair in Tables.instance.ItemEquipment)
            {
                if (pair.Value.cls.ToEnumItemType() != itemType ||
                    pair.Value.elemental != elementalType ||
                    pair.Value.grade != grade)
                    continue;

                outItemEquipmentRow = pair.Value;
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

        private static bool TryGetStat(Item itemRow, decimal roll, out StatMap statMap)
        {
            if (string.IsNullOrEmpty(itemRow.stat))
            {
                statMap = null;

                return false;
            }

            var key = itemRow.stat;
            var value = Math.Floor(itemRow.minStat + (itemRow.maxStat - itemRow.minStat) * roll);
            statMap = new StatMap(key, value);
            return true;
        }

        public static bool TryGetSkill(Item monsterParts, decimal roll, out Skill skill)
        {
            var table = Game.Game.instance.TableSheets.SkillSheet;
            try
            {
                var skillRow = table.ToOrderedList().First(r => r.Id == monsterParts.skillId);
                var chance = Math.Floor(monsterParts.minChance +
                                        (monsterParts.maxChance - monsterParts.minChance) * roll);
                chance = Math.Max(monsterParts.minChance, chance);
                var value = (int) Math.Floor(monsterParts.minDamage +
                                             (monsterParts.maxDamage - monsterParts.minDamage) * roll);

                skill = SkillFactory.Get(skillRow, value, chance);

                return true;
            }
            catch (InvalidOperationException)
            {
                skill = null;

                return false;
            }
        }

        public static bool TryGetEquipment(ItemEquipment itemEquipment, Item monsterParts, decimal roll, Guid itemId,
            out Equipment equipment)
        {
            if (!TryGetSkill(monsterParts, roll, out var skill))
            {
                equipment = null;

                return false;
            }

            equipment = (Equipment) ItemFactory.Create(itemEquipment, itemId);
            equipment.Skills.Add(skill);

            return true;
        }

        private static ItemUsable GetFood(ItemEquipment itemEquipment, Guid itemId)
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
            return (ItemUsable) ItemFactory.Create(itemEquipment, itemId);
        }
    }
}
