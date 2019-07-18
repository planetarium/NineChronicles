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
        public string ItemId { get; }

        protected ItemUsable(Data.Table.Item data, SkillBase skillBase = null, string id = null)
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
            var guid = Guid.NewGuid();
            ItemId = id ?? guid.ToString();
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
                return (base.GetHashCode() * 397) ^ (ItemId != null ? ItemId.GetHashCode() : 0);
            }
        }

        public override string ToItemInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Stats.GetInformation());

            if (SkillBase == null)
            {
                return sb.ToString().Trim();
            }

            sb.Append($"{SkillBase.chance * 100}% 확률로");
            sb.Append($" {SkillBase.effect.target}에게");
            sb.Append($" {SkillBase.power} 위력의");
            sb.Append($" {SkillBase.elementalType}속성 {SkillBase.effect.type}");

            return sb.ToString().Trim();
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
