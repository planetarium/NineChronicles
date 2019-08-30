using System;
using System.Collections.Generic;
using System.Text;
using Nekoyume.Data.Table;
using Nekoyume.Helper;
using Nekoyume.Model;
using UnityEngine;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public abstract class ItemUsable : ItemBase
    {
        public new ItemEquipment Data { get; }
        public Stats Stats { get; }
        public List<Skill> Skills { get; }
        public Guid ItemId { get; }

        protected ItemUsable(Data.Table.Item data, Guid id)
            : base(data)
        {
            Data = (ItemEquipment) data;
            Stats = new Stats();
            Skills = new List<Skill>();

            if (ValidateAbility(Data.ability1, Data.value1))
            {
                Stats.SetStatValue(Data.ability1, Data.value1);
            }

            if (ValidateAbility(Data.ability2, Data.value2))
            {
                Stats.SetStatValue(Data.ability2, Data.value2);
            }

            ItemId = id;
        }

        protected bool Equals(ItemUsable other)
        {
            return base.Equals(other) && string.Equals(ItemId, other.ItemId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ItemUsable) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ ItemId.GetHashCode();
            }
        }

        public override string ToItemInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Stats.GetInformation());

            if (Skills.Count == 0)
            {
                return sb.ToString().Trim();
            }

            foreach (var skill in Skills)
            {
                sb.Append($"{skill.chance * 100}% 확률로");
                sb.Append($" {skill.effect.skillTargetType}에게");
                sb.Append($" {skill.power} 위력의");
                sb.Append($" {skill.skillRow.ElementalType}속성 {skill.effect.skillType}");
            }

            return sb.ToString().Trim();
        }

        public override Sprite GetIconSprite()
        {
            return SpriteHelper.GetItemIcon(Data.resourceId);
        }

        public void UpdatePlayer(Player player)
        {
            Stats.UpdatePlayer(player);
            Skills.ForEach(skill => player.Skills.Add(skill));
        }

        protected bool ValidateAbility(string key, int value)
        {
            return !string.IsNullOrEmpty(key) && value > 0;
        }
    }
}
