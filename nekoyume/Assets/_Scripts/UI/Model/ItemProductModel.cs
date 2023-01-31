using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Game;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.Model.Market;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Newtonsoft.Json;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class ItemProductModel
    {
        public Guid ProductId { get; set; }
        public Address SellerAgentAddress { get; set; }
        public Address SellerAvatarAddress { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int ItemId { get; set; }
        public int Grade { get; set; }
        public ItemType ItemType { get; set; }
        public ItemSubType ItemSubType { get; set; }
        public ElementalType ElementalType { get; set; }
        public Guid TradableId { get; set; }
        public int SetId { get; set; }
        public int CombatPoint { get; set; }
        public int Level { get; set; }
        public List<SkillModel> Skills { get; set; }
        public List<StatModel> Stats { get; set; } = new();

        public ItemProduct ToItemProduct(TableSheets tableSheets, Currency goldCurrency)
        {
            var itemRow = tableSheets.ItemSheet[ItemId];
                var id = TradableId;
                long requiredBlockIndex = 0L;
                bool madeWithMimisbrunnrRecipe = false;
                ItemUsable itemUsable = null;
                switch (itemRow.ItemSubType)
                {
                    // Consumable
                    case ItemSubType.Food:
                        itemUsable = new Consumable((ConsumableItemSheet.Row) itemRow, id, requiredBlockIndex);
                        break;
                    // Equipment
                    case ItemSubType.Weapon:
                        itemUsable = new Weapon((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                    case ItemSubType.Armor:
                        itemUsable = new Armor((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                    case ItemSubType.Belt:
                        itemUsable = new Belt((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                    case ItemSubType.Necklace:
                        itemUsable = new Necklace((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                    case ItemSubType.Ring:
                        itemUsable = new Ring((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                }

                foreach (var skillModel in Skills)
                {
                    var skillRow = tableSheets.SkillSheet[skillModel.SkillId];
                    var skill = SkillFactory.Get(skillRow, skillModel.Power, skillModel.Chance);
                    itemUsable.Skills.Add(skill);
                }

                foreach (var statModel in Stats)
                {
                    if (statModel.Additional)
                    {
                        itemUsable.StatsMap.AddStatAdditionalValue(statModel.Type, statModel.Value);
                    }
                    else
                    {
                        var current = itemUsable.StatsMap.GetBaseStats(true).First(r => r.statType == statModel.Type).baseValue;
                        itemUsable.StatsMap.AddStatValue(statModel.Type, statModel.Value - current);
                    }
                }

                if (itemUsable is Equipment equipment)
                {
                    equipment.level = Level;
                }

                return new ItemProduct
                {
                    ProductId = ProductId,
                    Price = (BigInteger) Price * goldCurrency,
                    TradableItem = itemUsable,
                    ItemCount = (int) Quantity,
                };
        }
    }

    [Serializable]
    public class SkillModel
    {
        public Guid ItemProductId { get; set; }
        public int SkillId { get; set; }
        public ElementalType ElementalType { get; set; }
        public SkillCategory SkillCategory { get; set; }
        public int HitCount { get; set; }
        public int Cooldown { get; set; }
        public int Power { get; set; }
        public int Chance { get; set; }
    }

    [Serializable]
    public class StatModel
    {
        public int Value { get; set; }
        public StatType Type { get; set; }
        public bool Additional { get; set; }
    }

    [Serializable]
    public class ProductResponse
    {
        public int Limit { get; set; }
        public int Offset { get; set; }
        public IEnumerable<ItemProductModel> ItemProducts { get; set; }
    }
}
