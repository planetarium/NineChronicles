using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bencodex.Types;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public abstract class ItemUsable : ItemBase
    {
        public new ConsumableItemSheet.Row Data { get; }
        public Guid ItemId { get; }
        public StatsMap StatsMap { get; }
        public List<Skill> Skills { get; }
        public List<BuffSkill> BuffSkills { get; }

        protected ItemUsable(ConsumableItemSheet.Row data, Guid id) : base(data)
        {
            Data = data;
            ItemId = id;
            StatsMap = new StatsMap();
            foreach (var statData in data.Stats)
            {
                StatsMap.AddStatValue(statData.StatType, statData.Value);
            }

            Skills = new List<Skill>();
            BuffSkills = new List<BuffSkill>();
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

        public int GetOptionCount()
        {
            return StatsMap.GetAdditionalStats(true).Count()
                   + Skills.Count
                   + BuffSkills.Count;
        }

        // todo: 번역.
        public override string ToItemInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine(StatsMap.GetInformation());

            foreach (var skill in Skills)
            {
                sb.Append($"{skill.chance * 100}% 확률로");
                sb.Append($" {skill.skillRow.SkillTargetType}에게");
                sb.Append($" {skill.power} 위력의");
                sb.Append($" {skill.skillRow.ElementalType}속성 {skill.skillRow.SkillType}");
            }

            foreach (var buffSkill in BuffSkills)
            {
                sb.Append($"{buffSkill.chance * 100}% 확률로");
                sb.Append($" {buffSkill.skillRow.SkillTargetType}에게");
                sb.Append($" {buffSkill.power} 위력의");
                sb.Append($" {buffSkill.skillRow.ElementalType}속성 {buffSkill.skillRow.SkillType}");
            }

            return sb.ToString().Trim();
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "itemId"] = ItemId.Serialize(),
                [(Text) "statsMap"] = StatsMap.Serialize(),
                [(Text) "skills"] = new Bencodex.Types.List(Skills.Select(s => s.Serialize())),
                [(Text) "buffSkills"] = new Bencodex.Types.List(BuffSkills.Select(s => s.Serialize())),
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
    }
}
