namespace Nekoyume.UI
{
    public static class SummonUtil
    {
        public static int GetBackGroundPosition(CostType grade)
        {
            switch (grade)
            {
                case CostType.SilverDust:
                    return 1025;
                case CostType.GoldDust:
                    return 355;
                case CostType.RubyDust:
                    return -355;
                case CostType.EmeraldDust:
                    return -1025;
                default:
                    return 0;
            }
        }
    }
}
