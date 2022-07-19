namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Libplanet.Assets;
    using Nekoyume.Helper;
    using Xunit;

    public class RuneHelperTest
    {
        private readonly Currency _crystalCurrency = CrystalCalculator.CRYSTAL;

        [Fact]
        public void CalculateReward()
        {
            var tableSheet = new TableSheets(TableSheetsImporter.ImportSheets());
            var random = new TestRandom();

            foreach (var rewardRow in tableSheet.WorldBossRankRewardSheet)
            {
                var bossId = rewardRow.BossId;
                var rank = rewardRow.Rank;
                var fungibleAssetValues = RuneHelper.CalculateReward(
                    rank,
                    bossId,
                    tableSheet.RuneWeightSheet,
                    tableSheet.WorldBossRankRewardSheet,
                    random
                );
                var expectedRune = rewardRow.Rune;
                var expectedCrystal = rewardRow.Crystal * _crystalCurrency;
                var crystal = fungibleAssetValues.First(f => f.Currency.Equals(_crystalCurrency));
                var rune = fungibleAssetValues
                    .Where(f => !f.Currency.Equals(_crystalCurrency))
                    .Sum(r => (int)r.MajorUnit);

                Assert.Equal(expectedCrystal, crystal);
                Assert.Equal(expectedRune, rune);
            }
        }
    }
}
