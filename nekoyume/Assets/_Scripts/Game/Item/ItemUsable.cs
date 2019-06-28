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
        private SkillBase SkillBase { get; }

        protected ItemUsable(Data.Table.Item data, SkillBase skillBase = null)
            : base(data)
        {
            Data = (ItemEquipment) data;
            Stats = new Stats();
            SkillBase = skillBase;
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
            sb.Append($" {SkillBase.effect.multiplier * 100}% 위력의");
            sb.Append($" {SkillBase.elementalType}속성 {SkillBase.effect.type}");

            return sb.ToString().TrimEnd();
        }

        public void UpdatePlayer(Player player)
        {
            Stats.UpdatePlayer(player);
            player.Skills.Add(SkillBase);
        }
    }
}
