using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;
using Nekoyume.State;
using UnityEngine;

namespace Nekoyume.Action
{
    [ActionType("combination")]
    public class Combination : GameAction
    {
        [Serializable]
        public struct ItemModel
        {
            public int id;
            public int count;

            public ItemModel(int id, int count)
            {
                this.id = id;
                this.count = count;
            }

            public ItemModel(UI.Model.CountableItem item)
            {
                id = item.item.Value.Data.id;
                count = item.count.Value;
                Debug.Log($"ItemModel | Id:{id}, Count:{count}");
            }
        }

        private struct ItemModelInventoryItemPair
        {
            public readonly ItemModel ItemModel;
            public readonly Inventory.InventoryItem InventoryItem;

            public ItemModelInventoryItemPair(ItemModel itemModel, Inventory.InventoryItem inventoryItem)
            {
                ItemModel = itemModel;
                InventoryItem = inventoryItem;
            }
        }

        public struct ResultModel
        {
            public ItemModel Item;
        }

        public List<ItemModel> Materials { get; private set; }
        public ResultModel Result { get; private set; }

        protected override IImmutableDictionary<string, object> PlainValueInternal =>
            new Dictionary<string, object>
            {
                ["Materials"] = ByteSerializer.Serialize(Materials),
            }.ToImmutableDictionary();

        public Combination()
        {
            Materials = new List<ItemModel>();
        }

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            Materials = ByteSerializer.Deserialize<List<ItemModel>>((byte[]) plainValue["Materials"]);
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
            var pairs = new List<ItemModelInventoryItemPair>();
            for (var i = Materials.Count - 1; i >= 0; i--)
            {
                var m = Materials[i];
                try
                {
                    var inventoryItem =
                        avatarState.items.First(item => item.Item.Data.id == m.id && item.Count >= m.count);
                    pairs.Add(new ItemModelInventoryItemPair(m, inventoryItem));
                }
                catch (InvalidOperationException)
                {
                    return SimpleError(ctx, ErrorCode.CombinationNotFoundMaterials);
                }
            }

            // 조합식 테이블 로드.
            var recipeTable = Tables.instance.Recipe;

            // 조합식 검증.
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
                    resultCount = e.Current.Value.CalculateCount(Materials);
                    break;
                }
            }

            // 사용한 재료를 인벤토리에서 제거.
            pairs.ForEach(pair =>
            {
                pair.InventoryItem.Count -= pair.ItemModel.count;
                if (pair.InventoryItem.Count == 0)
                {
                    avatarState.items.Remove(pair.InventoryItem);
                }
            });

            // 뽀각!!
            if (ReferenceEquals(resultItem, null) ||
                resultCount == 0)
            {
                return SimpleAvatarError(ctx, ctx.Signer, avatarState, ErrorCode.CombinationNoResultItem);
            }
            
            // 조합 결과 획득.
            {
                if (Tables.instance.TryGetItemEquipment(resultItem.Id, out var itemEquipment))
                {
                    var itemUsable = GetItemUsableWithRandomSkill(itemEquipment, ctx.Random.Next(int.MaxValue));
                    avatarState.items.Add(new Inventory.InventoryItem(itemUsable, resultCount));
                }
                else
                {
                    return SimpleError(ctx, ErrorCode.KeyNotFoundInTable);
                }
            }

            Result = new ResultModel()
            {
                Item = new ItemModel(resultItem.Id, resultCount)
            };

            avatarState.updatedAt = DateTimeOffset.UtcNow;
            return states.SetState(ctx.Signer, avatarState);
        }
        
        private ItemUsable GetItemUsableWithRandomSkill(ItemEquipment itemEquipment, int randomValue)
        {
            var table = Tables.instance.SkillEffect;
            var skillEffect = table.ElementAt(randomValue % table.Count);   
            var elementalValues = Enum.GetValues(typeof(Elemental.ElementalType));
            var elementalType = (Elemental.ElementalType) elementalValues.GetValue(randomValue % elementalValues.Length);
            return (ItemUsable) ItemBase.ItemFactory(itemEquipment, 1f, skillEffect.Value, elementalType);
        }
    }
}
