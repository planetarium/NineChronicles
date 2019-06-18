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
        public float SkillChance { get; }
        public SkillEffect SkillEffect { get; }
        public Data.Table.Elemental.ElementalType SkillElementalType { get; }
        protected StatsMap[] Stats { get; set; }

        protected ItemUsable(Data.Table.Item data, float skillChance = 0f, SkillEffect skillEffect = null,
            Data.Table.Elemental.ElementalType skillElementalType = Nekoyume.Data.Table.Elemental.ElementalType.Normal)
            : base(data)
        {
            Data = (ItemEquipment) data;
            SkillChance = skillChance;
            SkillEffect = skillEffect;
            SkillElementalType = skillElementalType;
        }

        public void UpdatePlayer(Player player)
        {
            foreach (var stat in Stats)
            {
                stat.UpdatePlayer(player);
            }

            if (SkillEffect == null)
            {
                return;
            }

            var skill = SkillFactory.Get(SkillChance, SkillEffect, SkillElementalType);
            skill.caster = player;
            player.Skills.Add(skill);
        }
    }
}
