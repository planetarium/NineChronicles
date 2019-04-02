using System.Collections.Generic;

namespace Nekoyume.Data.Table
{
    public class MonsterWave : Row
    {
        public int Stage;
        public int Monster1;
        public int Monster2;
        public int Monster3;
        public int Monster4;
        public int Monster5;
        public int Monster6;
        public int Monster7;
        public int Monster8;
        public int Monster9;
        public int Wave = 0;
        public int BossId;

        public Dictionary<int, int> Monsters => new Dictionary<int, int>
        {
            [201001] = Monster1,
            [201002] = Monster2,
            [201003] = Monster3,
            [201004] = Monster4,
            [201005] = Monster5,
            [202001] = Monster6,
            [202002] = Monster7,
            [202003] = Monster8,
            [202004] = Monster9,
        };
    }
}
