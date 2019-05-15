using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Data.Table;

namespace Nekoyume.Model
{
    [Serializable]
    public class Skill : EventBase
    {
        public SkillEffect.SkillType type;
        public IEnumerable<SkillInfo> skillInfos;

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoSkill(character, type, skillInfos);
        }

        [Serializable]
        public class SkillInfo
        {
            public readonly CharacterBase Target;
            public readonly int Effect;
            public readonly bool Critical;

            public SkillInfo(CharacterBase character, int effect, bool critical)
            {
                Target = character;
                Effect = effect;
                Critical = critical;
            }
        }
    }
}
