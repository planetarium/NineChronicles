using System;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class CriticalBuff : StatBuff
    {
        public CriticalBuff(StatBuffSheet.Row row) : base(row)
        {
        }

        public CriticalBuff(SkillCustomField customField, StatBuffSheet.Row row)
            : base(customField, row)
        {
        }
    }
}
