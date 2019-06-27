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
            if (SkillBase?.effect != null)
            {
                sb.AppendLine(
                    $"{SkillBase.chance * 100}% 확률로 {SkillBase.effect.target}에게 {SkillBase.effect.multiplier * 100}% 위력으로 {SkillBase.effect.type}");
            }

            return sb.ToString().TrimEnd();
        }

        public void UpdatePlayer(Player player)
        {
            Stats.UpdatePlayer(player);
            player.Skills.Add(SkillBase);
        }
    }
}
