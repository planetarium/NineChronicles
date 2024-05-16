namespace Nekoyume.UI
{
    public static class SummonUtil
    {
        public static int GetBackGroundPosition(CostType grade)
        {
            switch (grade)
            {
                case CostType.SilverDust:
                    return 690;
                case CostType.GoldDust:
                    return 0;
                case CostType.RubyDust:
                    return -690;
                default:
                    return 0;
            }
        }
    }
}
