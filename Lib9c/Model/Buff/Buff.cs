using System;
using System.Collections.Generic;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public abstract class Buff : ICloneable
    {
        public SkillTargetType TargetType { get; }
        public int OriginalDuration { get; }
        public int RemainedDuration { get; set; }

        protected Buff(
            SkillTargetType targetType,
            int duration)
        {
            TargetType = targetType;
            OriginalDuration = RemainedDuration = duration;
        }

        protected Buff(Buff value)
        {
            TargetType = value.TargetType;
            OriginalDuration = value.OriginalDuration;
            RemainedDuration = value.RemainedDuration;
        }

        public virtual IEnumerable<CharacterBase> GetTarget(CharacterBase caster)
        {
            return TargetType.GetTarget(caster);
        }

        public abstract object Clone();
    }
}
