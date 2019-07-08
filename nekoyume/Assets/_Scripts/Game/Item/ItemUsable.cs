using System;
using System.Text;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;
using Nekoyume.Model;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public abstract class ItemUsable : ItemBase
    {
        public new ItemEquipment Data { get; }
        public Stats Stats { get; }
        public SkillBase SkillBase { get; }

        protected ItemUsable(Data.Table.Item data, SkillBase skillBase = null)
            : base(data)
        {
            Data = (ItemEquipment) data;
            Stats = new Stats();
            SkillBase = skillBase;
            
            if (ValidateAbility(Data.ability1, Data.value1))
            {
                Stats.SetStatValue(Data.ability1, Data.value1);
            }

            if (ValidateAbility(Data.ability2, Data.value2))
            {
                Stats.SetStatValue(Data.ability2, Data.value2);
            }
        }

        protected bool Equals(ItemUsable other)
        {
            return base.Equals(other) &&
                   Data.id == other.Data.id &&
                   Equals(Stats, other.Stats) &&
                   Equals(SkillBase, other.SkillBase);
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
                return (base.GetHashCode() * 397) ^ (Stats != null ? Stats.GetHashCode() : 0);
            }
        }

        public override string ToItemInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Stats.GetInformation());

            if (SkillBase == null)
            {
                return sb.ToString().TrimEnd();
            }

            sb.Append($"{SkillBase.chance * 100}% 확률로");
            sb.Append($" {SkillBase.effect.target}에게");
            sb.Append($" {SkillBase.power} 위력의");
            sb.Append($" {SkillBase.elementalType}속성 {SkillBase.effect.type}");

            return sb.ToString().TrimEnd();
        }

        public void UpdatePlayer(Player player)
        {
            Stats.UpdatePlayer(player);
            player.Skills.Add(SkillBase);
        }

        protected bool ValidateAbility(string key, int value)
        {
            return !string.IsNullOrEmpty(key) && value > 0;
        }
    }
}
