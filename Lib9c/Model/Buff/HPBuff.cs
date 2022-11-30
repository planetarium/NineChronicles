using System;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class HPBuff : StatBuff
    {
        public HPBuff(StatBuffSheet.Row row) : base(row)
        {
        }

        public HPBuff(SkillCustomField customField, StatBuffSheet.Row row)
            : base(customField, row)
        {
        }
    }
}
