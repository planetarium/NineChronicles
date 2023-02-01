using System;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class CriticalDamageBuff : StatBuff
    {
        public CriticalDamageBuff(StatBuffSheet.Row row) : base(row)
        {
        }

        public CriticalDamageBuff(SkillCustomField customField, StatBuffSheet.Row row)
            : base(customField, row)
        {
        }
    }
}
