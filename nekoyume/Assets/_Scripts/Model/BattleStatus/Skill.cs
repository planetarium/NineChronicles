using System;
using System.Collections.Generic;

namespace Nekoyume.Model
{
    [Serializable]
    public abstract class Skill : EventBase
    {
        public IEnumerable<SkillInfo> skillInfos;

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
