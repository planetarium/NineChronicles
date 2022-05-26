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
            Assert.Equal(1, sheet.First.Round.First().Id);
            Assert.Equal(1, sheet.First.Round.First().Round);
            Assert.Equal(ArenaType.OffSeason, sheet.First.Round.First().ArenaType);
            Assert.Equal(0, sheet.First.Round.First().StartBlockIndex);
            Assert.Equal(5, sheet.First.Round.First().EndBlockIndex);
            Assert.Equal(0, sheet.First.Round.First().RequiredMedalCount);
            Assert.Equal(0, sheet.First.Round.First().EntranceFee);
            Assert.Equal(5, sheet.First.Round.First().TicketPrice);
            Assert.Equal(2, sheet.First.Round.First().AdditionalTicketPrice);
        }

        [Theory]
        [InlineData(0, true, 1, 1, ArenaType.OffSeason)]
        [InlineData(21, true, 1, 4, ArenaType.Season)]
        [InlineData(61, true, 1, 8, ArenaType.Championship)]
        [InlineData(-1, false, default(int), default(int), default(ArenaType))]
        [InlineData(200001, false, default(int), default(int), default(ArenaType))]
        public void TryGetRoundByBlockIndexTest(
            long blockIndex,
            bool expectedExist,
            int expectedId,
            int expectedRound,
            ArenaType expectedArenaType)
        {
            if (expectedExist)
            {
                Assert.True(_arenaSheet.TryGetRoundByBlockIndex(blockIndex, out var roundData));
                Assert.Equal(expectedId, roundData.Id);
                Assert.Equal(expectedRound, roundData.Round);
                Assert.Equal(expectedArenaType, roundData.ArenaType);
                return;
            }

            Assert.False(_arenaSheet.TryGetRoundByBlockIndex(blockIndex, out _));
        }
    }
}
