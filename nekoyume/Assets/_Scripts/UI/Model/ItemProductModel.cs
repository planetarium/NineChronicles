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
        public long RegisteredBlockIndex { get; set; }
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
