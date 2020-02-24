using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

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
                TryParseInt(fields[0], out id);
                TryParseInt(fields[1], out characterId);
                TryParseInt(fields[2], out skillId);
            }
        }
        
        public EnemySkillSheet() : base(nameof(EnemySkillSheet))
        {
        }
    }
}
