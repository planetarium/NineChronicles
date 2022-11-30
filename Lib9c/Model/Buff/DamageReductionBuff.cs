using System;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class DamageReductionBuff : StatBuff
    {
        public DamageReductionBuff(StatBuffSheet.Row row) : base(row)
        {
        }

        public DamageReductionBuff(SkillCustomField customField, StatBuffSheet.Row row)
            : base(customField, row)
        {
        }
    }
}
