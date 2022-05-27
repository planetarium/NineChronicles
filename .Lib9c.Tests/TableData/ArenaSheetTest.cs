namespace Lib9c.Tests.TableData
{
    using System;
    using System.Linq;
    using Nekoyume.Model.EnumType;
    using Nekoyume.TableData;
    using Xunit;

    public class ArenaSheetTest
    {
        private readonly ArenaSheet _arenaSheet;

        public ArenaSheetTest()
        {
            if (!TableSheetsImporter.TryGetCsv(nameof(ArenaSheet), out var arenaCsv))
            {
                throw new Exception($"Not found sheet: {nameof(ArenaSheet)}");
            }

            _arenaSheet = new ArenaSheet();
            _arenaSheet.Set(arenaCsv);
        }

        [Fact]
        public void SetToSheet()
        {
            const string content = @"id,round,arena_type,start_block_index,end_block_index,required_medal_count,entrance_fee,discounted_entrance_fee,ticket_price,additional_ticket_price
1,1,OffSeason,0,5,0,0,0,5,2
1,2,Season,6,10,0,0,0,5,2
1,3,Championship,11,20000,1,0,0,5,2";

            var sheet = new ArenaSheet();
            sheet.Set(content);

            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
            Assert.Equal(3, sheet.First.Round.Count);
            Assert.Equal(1, sheet.First.Round.First().ChampionshipId);
            Assert.Equal(1, sheet.First.Round.First().Round);
            Assert.Equal(ArenaType.OffSeason, sheet.First.Round.First().ArenaType);
            Assert.Equal(0, sheet.First.Round.First().StartBlockIndex);
            Assert.Equal(5, sheet.First.Round.First().EndBlockIndex);
            Assert.Equal(0, sheet.First.Round.First().RequiredMedalCount);
            Assert.Equal(0, sheet.First.Round.First().EntranceFee);
            Assert.Equal(5, sheet.First.Round.First().TicketPrice);
            Assert.Equal(2, sheet.First.Round.First().AdditionalTicketPrice);
        }

        [Fact]
        public void Row_Round_Contains_8_Elements()
        {
            foreach (var row in _arenaSheet.OrderedList)
            {
                Assert.Equal(8, row.Round.Count);
            }
        }

        [Fact]
        public void Row_Round_Has_Deterministic_Pattern()
        {
            foreach (var row in _arenaSheet.OrderedList)
            {
                var rounds = row.Round;
                var round = rounds[0];
                var nextRound = rounds[1];
                Assert.Equal(ArenaType.OffSeason, round.ArenaType);
                Assert.Equal(0, round.RequiredMedalCount);
                Assert.Equal(0L, round.EntranceFee);
                Assert.Equal(0L, round.DiscountedEntranceFee);
                Assert.True(round.StartBlockIndex < round.EndBlockIndex);
                Assert.Equal(round.EndBlockIndex + 1, nextRound.StartBlockIndex);
                round = nextRound;
                nextRound = rounds[2];
                Assert.Equal(ArenaType.Season, round.ArenaType);
                Assert.Equal(0, round.RequiredMedalCount);
                Assert.True(round.EntranceFee > 0L);
                Assert.True(round.DiscountedEntranceFee > 0L);
                Assert.True(round.StartBlockIndex < round.EndBlockIndex);
                Assert.Equal(round.EndBlockIndex + 1, nextRound.StartBlockIndex);
                round = nextRound;
                nextRound = rounds[3];
                Assert.Equal(ArenaType.OffSeason, round.ArenaType);
                Assert.Equal(0, round.RequiredMedalCount);
                Assert.Equal(0L, round.EntranceFee);
                Assert.Equal(0L, round.DiscountedEntranceFee);
                Assert.True(round.StartBlockIndex < round.EndBlockIndex);
                Assert.Equal(round.EndBlockIndex + 1, nextRound.StartBlockIndex);
                round = nextRound;
                nextRound = rounds[4];
                Assert.Equal(ArenaType.Season, round.ArenaType);
                Assert.Equal(0, round.RequiredMedalCount);
                Assert.True(round.EntranceFee > 0L);
                Assert.True(round.DiscountedEntranceFee > 0L);
                Assert.True(round.StartBlockIndex < round.EndBlockIndex);
                Assert.Equal(round.EndBlockIndex + 1, nextRound.StartBlockIndex);
                round = nextRound;
                nextRound = rounds[5];
                Assert.Equal(ArenaType.OffSeason, round.ArenaType);
                Assert.Equal(0, round.RequiredMedalCount);
                Assert.Equal(0L, round.EntranceFee);
                Assert.Equal(0L, round.DiscountedEntranceFee);
                Assert.True(round.StartBlockIndex < round.EndBlockIndex);
                Assert.Equal(round.EndBlockIndex + 1, nextRound.StartBlockIndex);
                round = nextRound;
                nextRound = rounds[6];
                Assert.Equal(ArenaType.Season, round.ArenaType);
                Assert.Equal(0, round.RequiredMedalCount);
                Assert.True(round.EntranceFee > 0L);
                Assert.True(round.DiscountedEntranceFee > 0L);
                Assert.True(round.StartBlockIndex < round.EndBlockIndex);
                Assert.Equal(round.EndBlockIndex + 1, nextRound.StartBlockIndex);
                round = nextRound;
                nextRound = rounds[7];
                Assert.Equal(ArenaType.OffSeason, round.ArenaType);
                Assert.Equal(0, round.RequiredMedalCount);
                Assert.Equal(0L, round.EntranceFee);
                Assert.Equal(0L, round.DiscountedEntranceFee);
                Assert.True(round.StartBlockIndex < round.EndBlockIndex);
                Assert.Equal(round.EndBlockIndex + 1, nextRound.StartBlockIndex);
                round = nextRound;
                Assert.Equal(ArenaType.Championship, round.ArenaType);
                Assert.True(round.RequiredMedalCount > 0);
                Assert.True(round.EntranceFee > 0L);
                Assert.True(round.DiscountedEntranceFee > 0L);
                Assert.True(round.StartBlockIndex < round.EndBlockIndex);
            }
        }

        [Theory]
        [InlineData(-1, false, default(int), default(int))]
        [InlineData(0, true, 1, 8)]
        [InlineData(20000, true, 1, 8)]
        [InlineData(20001, false, default(int), default(int))]
        public void GetRowByBlockIndexTest(
            long blockIndex,
            bool expectedExist,
            int expectedChampionshipId,
            int expectedRoundCount)
        {
            if (expectedExist)
            {
                var row = _arenaSheet.GetRowByBlockIndex(blockIndex);
                Assert.NotNull(row);
                Assert.Equal(expectedChampionshipId, row.ChampionshipId);
                Assert.Equal(expectedRoundCount, row.Round.Count);
                return;
            }

            Assert.Throws<InvalidOperationException>(() =>
                _arenaSheet.GetRowByBlockIndex(blockIndex));
        }

        [Theory]
        [InlineData(-1, false, default(int), default(int), default(ArenaType))]
        [InlineData(0, true, 1, 1, ArenaType.OffSeason)]
        [InlineData(21, true, 1, 4, ArenaType.Season)]
        [InlineData(61, true, 1, 8, ArenaType.Championship)]
        [InlineData(20001, false, default(int), default(int), default(ArenaType))]
        public void GetRoundByBlockIndexTest(
            long blockIndex,
            bool expectedExist,
            int expectedId,
            int expectedRound,
            ArenaType expectedArenaType)
        {
            if (expectedExist)
            {
                var roundData = _arenaSheet.GetRoundByBlockIndex(blockIndex);
                Assert.NotNull(roundData);
                Assert.Equal(expectedId, roundData.ChampionshipId);
                Assert.Equal(expectedRound, roundData.Round);
                Assert.Equal(expectedArenaType, roundData.ArenaType);
                return;
            }

            Assert.Throws<InvalidOperationException>(() =>
                _arenaSheet.GetRoundByBlockIndex(blockIndex));
        }

        [Fact]
        public void TryGetSeasonNumberTest()
        {
            Assert.True(_arenaSheet.TryGetValue(1, out var row));
            Assert.False(row.TryGetSeasonNumber(1, out var seasonNumber));
            Assert.True(row.TryGetSeasonNumber(2, out seasonNumber));
            Assert.Equal(1, seasonNumber);
            Assert.False(row.TryGetSeasonNumber(3, out seasonNumber));
            Assert.True(row.TryGetSeasonNumber(4, out seasonNumber));
            Assert.Equal(2, seasonNumber);
            Assert.False(row.TryGetSeasonNumber(5, out seasonNumber));
            Assert.True(row.TryGetSeasonNumber(6, out seasonNumber));
            Assert.Equal(3, seasonNumber);
            Assert.False(row.TryGetSeasonNumber(7, out seasonNumber));
            Assert.False(row.TryGetSeasonNumber(8, out seasonNumber));
        }
    }
}
