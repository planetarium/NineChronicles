namespace Lib9c.Tests.TableData
{
    using System;
    using Nekoyume.TableData;
    using Xunit;

    public class WorldBossRankingRewardSheetTest
    {
        private const string Csv =
            @"id,boss_id,ranking_min,ranking_max,rate_min,rate_max,rune_1_id,rune_1_qty,rune_2_id,rune_2_qty,rune_3_id,rune_3_qty,crystal
1,900001,1,1,0,0,1,3500,2,1200,3,300,900000
2,900001,2,2,0,0,1,2200,2,650,3,150,625000
3,900001,3,3,0,0,1,1450,2,450,3,100,400000
4,900001,4,10,0,0,1,1000,2,330,3,70,250000
5,900001,11,100,0,0,1,560,2,150,3,40,150000
6,900001,0,0,1,30,1,370,2,105,3,25,100000
7,900001,0,0,31,50,1,230,2,60,3,10,50000
8,900001,0,0,51,70,1,75,2,20,3,5,25000
9,900001,0,0,71,100,1,40,2,10,3,0,15000
";

        [Fact]
        public void Set()
        {
            var sheet = new WorldBossRankingRewardSheet();
            sheet.Set(Csv);
            var row = sheet[1];
            Assert.Equal(1, row.Id);
            Assert.Equal(900001, row.BossId);
            Assert.Equal(1, row.RankingMin);
            Assert.Equal(1, row.RankingMax);
            Assert.Equal(0, row.RateMin);
            Assert.Equal(0, row.RateMax);
            Assert.Equal(3, row.Runes.Count);
            Assert.Equal(900000, row.Crystal);
        }

        [Theory]
        [InlineData(1, 0, 1)]
        [InlineData(1000, 1, 6)]
        public void FindRow(int ranking, int rate, int expected)
        {
            var sheet = new WorldBossRankingRewardSheet();
            sheet.Set(Csv);
            var row = sheet.FindRow(ranking, rate);
            Assert.Equal(expected, row.Id);
        }

        [Fact]
        public void FIndRow_Throw_ArgumentException()
        {
            var sheet = new WorldBossRankingRewardSheet();
            Assert.Throws<ArgumentException>(() => sheet.FindRow(0, 0));
        }

        [Fact]
        public void GetRewards()
        {
            var sheet = new WorldBossRankingRewardSheet();
            sheet.Set(Csv);
            var row = sheet.FindRow(1, 0);
            var runeSheet = new TableSheets(TableSheetsImporter.ImportSheets()).RuneSheet;
            var rewards = row.GetRewards(runeSheet);

            Assert.Equal(4, rewards.Count);
        }
    }
}
