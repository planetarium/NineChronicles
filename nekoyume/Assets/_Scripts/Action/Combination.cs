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

        private struct ItemModelInventoryItemPair
        {
            public readonly Material material;
            public readonly Inventory.Item item;

            public ItemModelInventoryItemPair(Material material, Inventory.Item item)
            {
                this.material = material;
                this.item = item;
            }
        }

        public List<Material> Materials { get; private set; }
        public List<ItemUsable> Results { get; }

        protected override IImmutableDictionary<string, object> PlainValueInternal =>
            new Dictionary<string, object>
            {
                ["Materials"] = ByteSerializer.Serialize(Materials),
            }.ToImmutableDictionary();

        public Combination()
        {
            Materials = new List<Material>();
            Results = new List<ItemUsable>();
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
                return SimpleError(ctx, ErrorCode.AvatarNotFound);
            }

            // 인벤토리에 재료를 갖고 있는지 검증.
            foreach (var material in Materials)
            {
                if (avatarState.inventory.TryGetFungibleItem(material.id, out var outFungibleItem))
                {
                    continue;
                }
                
                return SimpleError(ctx, ErrorCode.CombinationNotFoundMaterials);
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
                    resultCount = e.Current.Value.CalculateCount(Materials) > 0 ? 1 : 0; // 무조건 한 개만 나오도록.
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
                return SimpleAvatarError(ctx, ctx.Signer, avatarState, ErrorCode.CombinationNoResultItem);
            }
            
            // 조합 결과 획득.
            if (Tables.instance.TryGetItemEquipment(resultItem.Id, out var itemEquipment))
            {
                for (var i = 0; i < resultCount; i++)
                {
                    var itemUsable = GetItemUsableWithRandomSkill(itemEquipment, ctx.Random.Next());
                    avatarState.inventory.AddUnfungibleItem(itemUsable);
                    Results.Add(itemUsable);
                }
            }
            else
            {
                return SimpleError(ctx, ErrorCode.KeyNotFoundInTable);
            }

            avatarState.updatedAt = DateTimeOffset.UtcNow;
            return states.SetState(ctx.Signer, avatarState);
        }
        
        // ToDo. 순수 랜덤이 아닌 조합식이 적용되어야 함.
        private ItemUsable GetItemUsableWithRandomSkill(ItemEquipment itemEquipment, int randomValue)
        {
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
