using Nekoyume.Helper;

namespace Nekoyume.UI
{
    public static class SummonUtil
    {
        public static int GetBackGroundPosition(SummonResult result)
        {
            switch (result)
            {
                case SummonResult.Title:
                    return 1380;
                case SummonResult.FullCostume:
                    return 690;
                case SummonResult.Rune:
                    return 0;
                case SummonResult.Aura:
                    return -690;
                case SummonResult.Grimoire:
                    return -1380;
                default:
                    return 0;
            }
        }
    }
}
