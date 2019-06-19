using System;
using System.Linq;
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
        protected StatsMap[] Stats { get; set; }
        public SkillBase SkillBase { get; }

        protected ItemUsable(Data.Table.Item data, SkillBase skillBase = null)
            : base(data)
        {
            Data = (ItemEquipment) data;
            SkillBase = skillBase;
        }
        
        public override string ToItemInfo()
        {
            var sb = new StringBuilder();
            foreach (var statsMap in Stats)
            {
                var info = statsMap.GetInformation();
                if (string.IsNullOrEmpty(info))
                {
                    continue;
                }

                sb.AppendLine(info);
            }
            
            if (SkillBase?.effect != null)
            {
                sb.AppendLine($"{SkillBase.chance * 100}% 확률로 {SkillBase.effect.target}에게 {SkillBase.effect.multiplier * 100}% 위력으로 {SkillBase.effect.type}");
            }

            return sb.ToString();
        }

        public void UpdatePlayer(Player player)
        {
            foreach (var stat in Stats)
            {
                stat.UpdatePlayer(player);
            }

            if (SkillBase == null)
            {
                return;
            }

            SkillBase.caster = player;
            player.Skills.Add(SkillBase);
        }
    }
}
