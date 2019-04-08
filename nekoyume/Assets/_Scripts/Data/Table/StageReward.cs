using System.Collections.Generic;

namespace Nekoyume.Data.Table
{
    public class StageReward: Row
    {
        public int id = 0;
        public int item1 = 0;
        public float item1Ratio = 0f;
        public int item1Min = 0;
        public int item1Max = 0;
        public int item2 = 0;
        public float item2Ratio = 0f;
        public int item2Min = 0;
        public int item2Max = 0;
        public int item3 = 0;
        public float item3Ratio = 0f;
        public int item3Min = 0;
        public int item3Max = 0;
        public int item4 = 0;
        public float item4Ratio = 0f;
        public int item4Min = 0;
        public int item4Max = 0;
        public int item5 = 0;
        public float item5Ratio = 0f;
        public int item5Min = 0;
        public int item5Max = 0;

        public class RewardData
        {
            public int id;
            public float ratio;
            public int[] range;
        }

        public List<RewardData> Rewards()
        {
            var data = new RewardData
            {
                id = item1,
                ratio = item1Ratio,
                range = new[] {item1Min, item1Max},
            };
            var data2 = new RewardData
            {
                id = item2,
                ratio = item2Ratio,
                range = new[] {item2Min, item2Max},
            };
            var data3 = new RewardData
            {
                id = item3,
                ratio = item3Ratio,
                range = new[] {item3Min, item3Max},
            };
            var data4 = new RewardData
            {
                id = item4,
                ratio = item4Ratio,
                range = new[] {item4Min, item4Max},
            };
            var data5 = new RewardData
            {
                id = item5,
                ratio = item5Ratio,
                range = new[] {item5Min, item5Max},
            };
            return new List<RewardData>
            {
                data, data2, data3, data4, data5,
            };
        }
    }
}
