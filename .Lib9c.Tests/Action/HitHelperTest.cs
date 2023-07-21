namespace Lib9c.Tests.Action
{
    using System;
    using Nekoyume.Battle;
    using Xunit;

    public class HitHelperTest
    {
        [Fact]
        public void GetHitStep2()
        {
            // copy from previous logic
            int GetHitStep2Legacy(int attackerHit, int defenderHit)
            {
                attackerHit = Math.Max(1, attackerHit);
                defenderHit = Math.Max(1, defenderHit);
                var additionalCorrection = (int)((attackerHit - defenderHit / 3m) / defenderHit * 100);
                return Math.Min(
                    Math.Max(additionalCorrection, HitHelper.GetHitStep2AdditionalCorrectionMin),
                    HitHelper.GetHitStep2AdditionalCorrectionMax);
            }

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    var legacy = GetHitStep2Legacy(i, j);
                    var current = HitHelper.GetHitStep2(i, j);
                    Assert.True(legacy == current, $"{i}, {j}, {legacy}, {current}");
                }
            }
        }
    }
}
