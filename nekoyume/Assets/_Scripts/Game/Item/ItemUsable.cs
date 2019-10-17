using System;
using System.Collections.Generic;
using System.Text;
using Nekoyume.Data.Table;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public abstract class ItemUsable : ItemBase
    {
        public new ConsumableItemSheet.Row Data { get; }

        public StatsMap StatsMap { get; }
        public List<Skill> Skills { get; }
        public List<BuffSkill> BuffSkills { get; }
        public Guid ItemId { get; }

        protected ItemUsable(ConsumableItemSheet.Row data, Guid id) : base(data)
        {
            Data = data;
            StatsMap = new StatsMap();
            foreach (var statData in data.Stats)
            {
                StatsMap.AddStatValue(statData.StatType, statData.Value);
            }

            Skills = new List<Skill>();
            BuffSkills = new List<BuffSkill>();

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
            return (Data != null ? Data.GetHashCode() : 0) ^ ItemId.GetHashCode();
        }

        // todo: 번역.
        public override string ToItemInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine(StatsMap.GetInformation());

            foreach (var skill in Skills)
            {
                sb.Append($"{skill.chance * 100}% 확률로");
                sb.Append($" {skill.effect.skillTargetType}에게");
                sb.Append($" {skill.power} 위력의");
                sb.Append($" {skill.skillRow.ElementalType}속성 {skill.effect.skillType}");
            }
            
            foreach (var buffSkill in BuffSkills)
            {
                sb.Append($"{buffSkill.chance * 100}% 확률로");
                sb.Append($" {buffSkill.effect.skillTargetType}에게");
                sb.Append($" {buffSkill.power} 위력의");
                sb.Append($" {buffSkill.skillRow.ElementalType}속성 {buffSkill.effect.skillType}");
            }

            return sb.ToString().Trim();
        }
    }
}
