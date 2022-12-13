using System;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class HitBuff : StatBuff
    {
        public HitBuff(StatBuffSheet.Row row) : base(row)
        {
        }

        public HitBuff(SkillCustomField customField, StatBuffSheet.Row row)
            : base(customField, row)
        {
        }
    }
}
