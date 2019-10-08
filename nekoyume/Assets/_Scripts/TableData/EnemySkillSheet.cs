using System;
using System.Collections.Generic;

namespace Nekoyume.TableData
{
    [Serializable]
    public class EnemySkillSheet: Sheet<int, EnemySkillSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => id;
            public int id;
            public int characterId;
            public int skillId;
            public override void Set(IReadOnlyList<string> fields)
            {
                int.TryParse(fields[0], out id);
                int.TryParse(fields[1], out characterId);
                int.TryParse(fields[2], out skillId);
            }
        }
        
        public EnemySkillSheet() : base(nameof(EnemySkillSheet))
        {
        }
    }
}
