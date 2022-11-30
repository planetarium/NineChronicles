namespace Lib9c.Tests.TableData
{
    using System;
    using Nekoyume.TableData;
    using Xunit;

    public class WorldBossRankingRewardSheetTest
    {
        private const string Csv =
            @"id,boss_id,ranking_min,ranking_max,rate_min,rate_max,rune_1_id,rune_1_qty,rune_2_id,rune_2_qty,rune_3_id,rune_3_qty,crystal
1,900001,1,1,0,0,10001,3500,10002,1200,10003,300,900000
2,900001,2,2,0,0,10001,2200,10002,650,10003,150,625000
3,900001,3,3,0,0,10001,1450,10002,450,10003,100,400000
4,900001,4,10,0,0,10001,1000,10002,330,10003,70,250000
5,900001,11,100,0,0,10001,560,10002,150,10003,40,150000
6,900001,0,0,1,30,10001,370,10002,105,10003,25,100000
7,900001,0,0,31,50,10001,230,10002,60,10003,10,50000
8,900001,0,0,51,70,10001,75,10002,20,10003,5,25000
9,900001,0,0,71,100,10001,40,10002,10,10003,0,15000
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
