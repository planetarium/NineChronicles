using System;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;
using Nekoyume.Model;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public abstract class ItemUsable : ItemBase
    {
        public new ItemEquipment Data { get; }
        public SkillBase SkillBase { get; }
        protected StatsMap[] Stats { get; set; }

        public ItemUsable(Data.Table.Item data, SkillBase skillBase = null)
            : base(data)
        {
            Data = (ItemEquipment) data;
            SkillBase = skillBase;
        }

        public virtual void UpdatePlayer(Player player)
        {
            foreach (var stat in Stats)
            {
                stat.UpdatePlayer(player);
            }
        }
    }
}
