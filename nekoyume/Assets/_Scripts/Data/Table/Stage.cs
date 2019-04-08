using System.Collections.Generic;

namespace Nekoyume.Data.Table
{
    public class Stage : Row
    {
        public int id = 0;
        public int stage = 0;
        public int wave = 0;
        public int monster1Id = 0;
        public int monster1Level = 0;
        public string monster1Visual = "";
        public int monster1Count = 0;
        public int monster2Id = 0;
        public int monster2Level = 0;
        public string monster2Visual = "";
        public int monster2Count = 0;
        public int monster3Id = 0;
        public int monster3Level = 0;
        public string monster3Visual = "";
        public int monster3Count = 0;
        public int monster4Id = 0;
        public int monster4Level = 0;
        public string monster4Visual = "";
        public int monster4Count = 0;
        public bool isBoss = false;
        public int reward = 0;
        public long exp = 0;

        public class MonsterData
        {
            public int id;
            public int level;
            public string visual;
            public int count;
        }

        public List<MonsterData> Monsters()
        {
            var data = new MonsterData
            {
                id = monster1Id,
                level = monster1Level,
                visual = monster1Visual,
                count = monster1Count
            };
            var data2 = new MonsterData
            {
                id = monster2Id,
                level = monster2Level,
                visual = monster2Visual,
                count = monster2Count
            };
            var data3 = new MonsterData
            {
                id = monster3Id,
                level = monster3Level,
                visual = monster3Visual,
                count = monster3Count
            };
            var data4 = new MonsterData
            {
                id = monster4Id,
                level = monster4Level,
                visual = monster4Visual,
                count = monster4Count
            };
            return new List<MonsterData>
            {
                data,
                data2,
                data3,
                data4,
            };
        }
    }
}
