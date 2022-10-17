namespace Lib9c.Tests.TableData
{
    using System;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.Model.Arena;
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
            const string content = @"id,round,arena_type,start_block_index,end_block_index,required_medal_count,entrance_fee,ticket_price,additional_ticket_price
1,1,OffSeason,1,2,0,0,5,2
1,2,Season,3,4,0,100,50,20
1,3,OffSeason,5,1005284,0,0,5,2";

            var sheet = new ArenaSheet();
            sheet.Set(content);

            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
            Assert.Equal(3, sheet.First.Round.Count);
            Assert.Equal(1, sheet.First.Round.First().ChampionshipId);
            Assert.Equal(1, sheet.First.Round.First().Round);
            Assert.Equal(ArenaType.OffSeason, sheet.First.Round.First().ArenaType);
            Assert.Equal(1, sheet.First.Round.First().StartBlockIndex);
            Assert.Equal(2, sheet.First.Round.First().EndBlockIndex);
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
                Assert.True(round.StartBlockIndex < round.EndBlockIndex);
                Assert.Equal(round.EndBlockIndex + 1, nextRound.StartBlockIndex);
                round = nextRound;
                nextRound = rounds[2];
                Assert.Equal(ArenaType.Season, round.ArenaType);
                Assert.Equal(0, round.RequiredMedalCount);
                Assert.True(round.EntranceFee > 0L);
                Assert.True(round.StartBlockIndex < round.EndBlockIndex);
                Assert.Equal(round.EndBlockIndex + 1, nextRound.StartBlockIndex);
                round = nextRound;
                nextRound = rounds[3];
                Assert.Equal(ArenaType.OffSeason, round.ArenaType);
                Assert.Equal(0, round.RequiredMedalCount);
                Assert.Equal(0L, round.EntranceFee);
                Assert.True(round.StartBlockIndex < round.EndBlockIndex);
                Assert.Equal(round.EndBlockIndex + 1, nextRound.StartBlockIndex);
                round = nextRound;
                nextRound = rounds[4];
                Assert.Equal(ArenaType.Season, round.ArenaType);
                Assert.Equal(0, round.RequiredMedalCount);
                Assert.True(round.EntranceFee > 0L);
                Assert.True(round.StartBlockIndex < round.EndBlockIndex);
                Assert.Equal(round.EndBlockIndex + 1, nextRound.StartBlockIndex);
                round = nextRound;
                nextRound = rounds[5];
                Assert.Equal(ArenaType.OffSeason, round.ArenaType);
                Assert.Equal(0, round.RequiredMedalCount);
                Assert.Equal(0L, round.EntranceFee);
                Assert.True(round.StartBlockIndex < round.EndBlockIndex);
                Assert.Equal(round.EndBlockIndex + 1, nextRound.StartBlockIndex);
                round = nextRound;
                nextRound = rounds[6];
                if (round.ChampionshipId != 2)
                {
                    Assert.Equal(ArenaType.Season, round.ArenaType);
                    Assert.Equal(0, round.RequiredMedalCount);
                }
                else
                {
                    Assert.Equal(ArenaType.Championship, round.ArenaType);
                    Assert.Equal(160, round.RequiredMedalCount);
                }

                Assert.True(round.EntranceFee > 0L);
                Assert.True(round.StartBlockIndex < round.EndBlockIndex);
                Assert.Equal(round.EndBlockIndex + 1, nextRound.StartBlockIndex);

                round = nextRound;
                nextRound = rounds[7];
                Assert.Equal(ArenaType.OffSeason, round.ArenaType);
                Assert.Equal(0, round.RequiredMedalCount);
                Assert.Equal(0L, round.EntranceFee);
                Assert.True(round.StartBlockIndex < round.EndBlockIndex);
                Assert.Equal(round.EndBlockIndex + 1, nextRound.StartBlockIndex);
                round = nextRound;
                if (round.ChampionshipId != 2)
                {
                    Assert.Equal(ArenaType.Championship, round.ArenaType);
                }
                else
                {
                    Assert.Equal(ArenaType.OffSeason, round.ArenaType);
                }

                Assert.True(round.RequiredMedalCount > 0);
                Assert.True(round.EntranceFee > 0L);
                Assert.True(round.StartBlockIndex < round.EndBlockIndex);
            }
        }

        [Fact]
        public void GetRowByBlockIndexTest()
        {
            var random = new TestRandom();
            var expectRow = _arenaSheet.OrderedList[random.Next(0, _arenaSheet.Count)];
            var expectRound = expectRow.Round[random.Next(0, 8)];
            var blockIndex = expectRound.StartBlockIndex;
            var testRow = _arenaSheet.GetRowByBlockIndex(blockIndex);
            Assert.NotNull(testRow);
            Assert.Equal(expectRow.ChampionshipId, testRow.ChampionshipId);
            blockIndex = expectRound.EndBlockIndex;
            testRow = _arenaSheet.GetRowByBlockIndex(blockIndex);
            Assert.NotNull(testRow);
            Assert.Equal(expectRow.ChampionshipId, testRow.ChampionshipId);

            var lastRound = expectRow.Round[7];
            blockIndex = lastRound.EndBlockIndex + 1;
            Assert.Throws<InvalidOperationException>(() =>
                _arenaSheet.GetRowByBlockIndex(blockIndex));
        }

        [Fact]
        public void GetRoundByBlockIndexTest()
        {
            var random = new TestRandom();
            var expectRow = _arenaSheet.OrderedList[random.Next(0, _arenaSheet.Count)];
            var expectRound = expectRow.Round[random.Next(0, 8)];
            var blockIndex = expectRound.StartBlockIndex;
            var testRound = _arenaSheet.GetRoundByBlockIndex(blockIndex);
            Assert.NotNull(testRound);
            Assert.Equal(expectRound.ChampionshipId, testRound.ChampionshipId);
            Assert.Equal(expectRound.Round, testRound.Round);
            Assert.Equal(expectRound.ArenaType, testRound.ArenaType);
            blockIndex = expectRound.EndBlockIndex;
            testRound = _arenaSheet.GetRoundByBlockIndex(blockIndex);
            Assert.NotNull(testRound);
            Assert.Equal(expectRound.ChampionshipId, testRound.ChampionshipId);
            Assert.Equal(expectRound.Round, testRound.Round);
            Assert.Equal(expectRound.ArenaType, testRound.ArenaType);

            var lastRound = expectRow.Round[7];
            blockIndex = lastRound.EndBlockIndex + 1;
            Assert.Throws<RoundNotFoundException>(() =>
                _arenaSheet.GetRoundByBlockIndex(blockIndex));
        }
    }
}
