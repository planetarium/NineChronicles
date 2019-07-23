using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;
using Nekoyume.Game.Skill;
using Nekoyume.Model;
using Nekoyume.State;
using UniRx.Async;

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

        public List<Material> Materials { get; private set; }
        public Address avatarAddress;

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
            {
                return states;
            }

            // 사용한 재료를 인벤토리에서 제거.
            foreach (var material in Materials)
            {
                if (!avatarState.inventory.RemoveFungibleItem(material.id, material.count))
                {
                    return states;
                }
            }

            // 조합식 테이블 로드.
            var recipeTable = Tables.instance.Recipe;

            // 조합 재료 정렬.
            var orderedMaterials = Materials.OrderBy(order => order.id).ToList();

            // 장비인지 소모품인지 확인.
            var equipmentMaterials = orderedMaterials.Where(item => GameConfig.EquipmentMaterials.Contains(item.id))
                .ToList();
            if (equipmentMaterials.Any())
            {
                // 장비
                orderedMaterials.RemoveAll(item => equipmentMaterials.Contains(item));
                if (orderedMaterials.Count == 0)
                {
                    return states;
                }

                var equipmentMaterial = equipmentMaterials[0];
                var monsterPartsMaterial = orderedMaterials[0];

                if (!Tables.instance.Item.TryGetValue(equipmentMaterial.id, out var outEquipmentMaterialRow) ||
                    !Tables.instance.Item.TryGetValue(monsterPartsMaterial.id, out var outMonsterPartsMaterialRow))
                {
                    return states;
                }

                if (!TryGetItemType(outEquipmentMaterialRow.id, out var outItemType))
                {
                    return states;
                }

                if (!TryGetItemEquipmentRow(outItemType, outMonsterPartsMaterialRow.elemental,
                    outEquipmentMaterialRow.grade,
                    out var itemEquipmentRow))
                {
                    return states;
                }

                var normalizedRandomValue = ctx.Random.Next(0, 100000) * 0.00001f;
                var roll = GetRoll(monsterPartsMaterial.count, 0, normalizedRandomValue);

                // 조합 결과 획득.
                var itemUsable = GetEquipment(itemEquipmentRow, outMonsterPartsMaterialRow, roll);

                // 추가 스탯 적용.
                var stat = GetStat(outMonsterPartsMaterialRow, roll);
                itemUsable.Stats.SetStatAdditionalValue(stat.Key, stat.Value);

                avatarState.inventory.AddNonFungibleItem(itemUsable);
            }
            else
            {
                ItemEquipment itemEquipmentRow = null;
                // 소모품
                foreach (var recipe in recipeTable)
                {
                    if (!recipe.Value.IsMatchForConsumable(orderedMaterials))
                    {
                        continue;
                    }

                    if (!Tables.instance.ItemEquipment.TryGetValue(recipe.Value.ResultId, out itemEquipmentRow))
                    {
                        break;
                    }

                    if (recipe.Value.GetCombinationResultCountForConsumable(orderedMaterials) == 0)
                    {
                        break;
                    }
                }

                if (itemEquipmentRow == null
                    && !Tables.instance.ItemEquipment.TryGetValue(GameConfig.CombinationDefaultFoodId, out itemEquipmentRow))
                {
                    return states;
                }
                
                // 조합 결과 획득.
                var itemUsable = GetFood(itemEquipmentRow);
                avatarState.inventory.AddNonFungibleItem(itemUsable);
            }

            avatarState.updatedAt = DateTimeOffset.UtcNow;
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

        private bool TryGetItemEquipmentRow(ItemBase.ItemType itemType, Elemental.ElementalType elementalType,
            int grade, out ItemEquipment outItemEquipmentRow)
        {
            foreach (var pair in Tables.instance.ItemEquipment)
            {
                if (pair.Value.cls.ToEnumItemType() != itemType ||
                    pair.Value.elemental != elementalType ||
                    pair.Value.grade != grade)
                {
                    continue;
                }

                outItemEquipmentRow = pair.Value;
                return true;
            }

            outItemEquipmentRow = null;
            return false;
        }

        private float GetRoll(int monsterPartsCount, int deltaLevel, float normalizedRandomValue)
        {
            var rollMax = Math.Pow(1f / (1f + GameConfig.CombinationValueP1 / monsterPartsCount),
                              GameConfig.CombinationValueP2) *
                          (deltaLevel <= 0
                              ? 1f
                              : Math.Pow(1f / (1f + GameConfig.CombinationValueL1 / deltaLevel),
                                  GameConfig.CombinationValueL2));
            var rollMin = rollMax * 0.7f;
            return (float) (rollMin + (rollMax - rollMin) *
                            Math.Pow(normalizedRandomValue, GameConfig.CombinationValueR1));
        }

        private StatMap GetStat(Item itemRow, float roll)
        {
            var key = itemRow.stat;
            var value = (float) Math.Floor(itemRow.minStat + (itemRow.maxStat - itemRow.minStat) * roll);
            return new StatMap(key, value);
        }

        private static ItemUsable GetFood(ItemEquipment itemEquipment)
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
            return (ItemUsable) ItemBase.ItemFactory(itemEquipment);
        }

        public static Equipment GetEquipment(ItemEquipment itemEquipment, Item monsterParts, float roll)
        {
            var table = Tables.instance.SkillEffect;
            SkillBase skill;
            try
            {
                var skillEffect = table.First(r => r.Value.id == monsterParts.skillId);
                var elementalType = monsterParts.elemental;
                var chance = (float) Math.Floor(monsterParts.minChance +
                                                (monsterParts.maxChance - monsterParts.minChance) * roll);
                chance = Math.Max(monsterParts.minChance, chance);
                var value = (int) Math.Floor(monsterParts.minDamage +
                                             (monsterParts.maxDamage - monsterParts.minDamage) * roll);
                skill = SkillFactory.Get(chance, skillEffect.Value, elementalType, value);
            }
            catch (InvalidOperationException)
            {
                skill = null;
            }

            return (Equipment) ItemBase.ItemFactory(itemEquipment, skill);
        }
    }
}
