using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class CharacterLevelSheet : Sheet<int, CharacterLevelSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Level;
            public int Level { get; private set; }
            public long Exp { get; private set; }
            public long ExpNeed { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Level = TryParseInt(fields[0], out var level) ? level : 0;
                Exp = TryParseLong(fields[1], out var exp) ? exp : 0L;
                ExpNeed = TryParseLong(fields[2], out var expNeed) ? expNeed : 0L;
            }
        }

        public CharacterLevelSheet() : base(nameof(CharacterLevelSheet))
        {
        }

        public int GetLevel(long exp)
        {
            foreach (var row in OrderedList)
            {
                if (row.Exp + row.ExpNeed > exp)
                {
                    return row.Key;
                }
            }

            return 0;
        }

        public bool TryGetLevel(long exp, out int level)
        {
            foreach (var row in OrderedList)
            {
                if (row.Exp + row.ExpNeed > exp)
                {
                    level = row.Key;
                    return true;
                }
            }

            level = default;
            return false;
        }
    }
}
