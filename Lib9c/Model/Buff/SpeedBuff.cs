using System;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class SpeedBuff : StatBuff
    {
        public SpeedBuff(StatBuffSheet.Row row) : base(row)
        {
        }

        public SpeedBuff(SkillCustomField customField, StatBuffSheet.Row row)
            : base(customField, row)
        {
        }
    }
}
