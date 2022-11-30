using System;
using Nekoyume.Model;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class DefenseBuff : StatBuff
    {
        public DefenseBuff(StatBuffSheet.Row row) : base(row)
        {
        }

        public DefenseBuff(SkillCustomField customField, StatBuffSheet.Row row)
            : base(customField, row)
        {
        }
    }
}
