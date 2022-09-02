using System;
using System.Collections.Generic;
using Nekoyume.Model.Skill;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public abstract class Buff : ICloneable
    {
        public BuffInfo BuffInfo { get; }
        public int OriginalDuration { get; }
        public int RemainedDuration { get; set; }

        protected Buff(BuffInfo buffInfo)
        {
            BuffInfo = buffInfo;
            OriginalDuration = RemainedDuration = buffInfo.Duration;
        }

        protected Buff(Buff value)
        {
            BuffInfo = value.BuffInfo;
            OriginalDuration = value.OriginalDuration;
            RemainedDuration = value.RemainedDuration;
        }

        public virtual IEnumerable<CharacterBase> GetTarget(CharacterBase caster)
        {
            return BuffInfo.SkillTargetType.GetTarget(caster);
        }

        public abstract object Clone();
    }

    [Serializable]
    public struct BuffInfo
    {
        public int Id;
        public int GroupId;
        public int Chance;
        public int Duration;
        public SkillTargetType SkillTargetType;

        public BuffInfo(int id, int groupId, int chance, int duration, SkillTargetType skillTargetType)
        {
            Id = id;
            GroupId = groupId;
            Chance = chance;
            Duration = duration;
            SkillTargetType = skillTargetType;
        }
    }

}
