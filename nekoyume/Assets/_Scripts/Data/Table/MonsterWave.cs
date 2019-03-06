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
            [1001] = Monster1,
            [1002] = Monster2,
            [1003] = Monster3,
            [1004] = Monster4,
            [1005] = Monster5,
            [1101] = Monster6,
            [1102] = Monster7,
            [1103] = Monster8,
            [1104] = Monster9,
        };
    }
}
