using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;
using Nekoyume.Game.Skill;
using Nekoyume.State;
using UnityEngine;

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

            public Material(int id, int count)
            {
                this.id = id;
                this.count = count;
            }

            public Material(UI.Model.CountableItem item)
            {
                id = item.item.Value.Data.id;
                count = item.count.Value;
            }
        }

        public List<Material> Materials { get; private set; }

        protected override IImmutableDictionary<string, object> PlainValueInternal =>
            new Dictionary<string, object>
            {
                ["Materials"] = ByteSerializer.Serialize(Materials),
            }.ToImmutableDictionary();

        public Combination()
        {
            Materials = new List<Material>();
        }

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            Materials = ByteSerializer.Deserialize<List<Material>>((byte[]) plainValue["Materials"]);
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.SetState(ctx.Signer, MarkChanged);
            }

            var avatarState = (AvatarState) states.GetState(ctx.Signer);
            if (avatarState == null)
            {
                return states;
            }

            // 인벤토리에 재료를 갖고 있는지 검증.
            foreach (var material in Materials)
            {
                if (avatarState.inventory.TryGetFungibleItem(material.id, out var outFungibleItem))
                {
                    continue;
                }
                
                return states;
            }

            // 조합식 테이블 로드.
            var recipeTable = Tables.instance.Recipe;

            // 조합식 검증.
            // ToDo. 조합식 재개발 필요.
            Recipe resultItem = null;
            var resultCount = 0;
            using (var e = recipeTable.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (!e.Current.Value.IsMatch(Materials))
                    {
                        continue;
                    }

                    resultItem = e.Current.Value;
                    resultCount = e.Current.Value.CalculateCount(Materials) > 0 ? 1 : 0; // 1개 이상일 때 1개만 나오도록.
                    break;
                }
            }

            // 사용한 재료를 인벤토리에서 제거.
            foreach (var material in Materials)
            {
                avatarState.inventory.RemoveFungibleItem(material.id, material.count);
            }

            // 뽀각!!
            if (ReferenceEquals(resultItem, null) ||
                resultCount == 0)
            {
                return states.SetState(ctx.Signer, avatarState);
            }
            
            // 조합 결과 획득.
            if (Tables.instance.TryGetItemEquipment(resultItem.Id, out var itemEquipment))
            {
                for (var i = 0; i < resultCount; i++)
                {
                    var itemUsable = GetItemUsableWithRandomSkill(itemEquipment, ctx.Random.Next());
                    avatarState.inventory.AddUnfungibleItem(itemUsable);
                }
            }
            else
            {
                return states;
            }

            avatarState.updatedAt = DateTimeOffset.UtcNow;
            return states.SetState(ctx.Signer, avatarState);
        }
        
        // ToDo. 순수 랜덤이 아닌 조합식이 적용되어야 함.
        private ItemUsable GetItemUsableWithRandomSkill(ItemEquipment itemEquipment, int randomValue)
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
            if (itemEquipment.cls.ToEnumItemType() == ItemBase.ItemType.Food)
            {
                return (ItemUsable) ItemBase.ItemFactory(itemEquipment);
            }
            
            var table = Tables.instance.SkillEffect;
            var skillEffect = table.ElementAt(randomValue % table.Count);   
            var elementalValues = Enum.GetValues(typeof(Elemental.ElementalType));
            var elementalType = (Elemental.ElementalType) elementalValues.GetValue(randomValue % elementalValues.Length);
            var skill = SkillFactory.Get(0.05f, skillEffect.Value, elementalType); // FixMe. 테스트를 위해서 5% 확률로 발동되도록 함.
            return (ItemUsable) ItemBase.ItemFactory(itemEquipment, skill);
        }
    }
}
