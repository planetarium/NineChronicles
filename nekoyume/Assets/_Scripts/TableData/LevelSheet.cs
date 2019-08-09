using System;
using Nekoyume.Game.Controller;

namespace Nekoyume.TableData
{
    [Serializable]
    public class LevelSheet : Sheet<int, LevelSheet.Row>
    {
        [Serializable]
        public struct Row : ISheetRow<int>
        {
            public int Level { get; private set; }
            public long Exp { get; private set; }
            public long ExpNeed { get; private set; }

            public int Key => Level;
            
            public void Set(string[] fields)
            {
                Level = int.TryParse(fields[0], out var level) ? level : 0;
                Exp = long.TryParse(fields[1], out var exp) ? exp : 0L;
                ExpNeed = long.TryParse(fields[2], out var expNeed) ? expNeed : 0L;
            }
        }

        public int GetLevel(long exp)
        {
            var e = GetEnumerator();
            while (e.MoveNext())
            {
                var row = e.Current;
                if (row.Exp + row.ExpNeed > exp)
                {
                    return row.Key;
                }
            }
            e.Dispose();

            return 0;
        }
    }
}
