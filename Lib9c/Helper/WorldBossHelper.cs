using System;

namespace Nekoyume.Helper
{
    public static class WorldBossHelper
    {
        public static int CalculateRank(int highScore)
        {
            return Math.Min(5, highScore / 10_000);
        }
    }
}
