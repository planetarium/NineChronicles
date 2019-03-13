using System;

namespace Nekoyume.Data.Table
{
    [Serializable]
    public class Monster : Row
    {
        public int Id = 0;
        public WeightType WeightType = WeightType.Small;
        public int Attack = 0;
        public int Defense = 0;
        public int Health = 0;
        public string Resistance = "";
        public string skill0 = "";
        public string skill1 = "";
        public string skill2 = "";
        public string skill3 = "";
        public int RewardExp = 0;
    }
}
