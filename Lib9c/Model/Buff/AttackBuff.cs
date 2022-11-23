using System;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class AttackBuff : StatBuff
    {
        public AttackBuff(StatBuffSheet.Row row) : base(row)
        {
        }

        public AttackBuff(SkillCustomField customField, StatBuffSheet.Row row)
            : base(customField, row)
        {
        }
    }
}
