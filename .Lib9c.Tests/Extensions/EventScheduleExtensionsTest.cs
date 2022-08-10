namespace Lib9c.Tests.Extensions
{
    using System;
    using Nekoyume.Extensions;
    using Nekoyume.TableData.Event;
    using Xunit;

    public class EventScheduleExtensionsTest
    {
        [Theory]
        [InlineData(10_000_000, 1_000)]
        [InlineData(99_999_999, 9_999)]
        public void ToEventScheduleId(
            int eventDungeonId,
            int expected) =>
            Assert.Equal(
                expected,
                eventDungeonId.ToEventScheduleId());

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1_000)]
        [InlineData(10_000)]
        [InlineData(100_000)]
        [InlineData(1_000_000)]
        [InlineData(9_999_999)]
        [InlineData(100_000_000)]
        [InlineData(int.MaxValue)]
        public void ToEventScheduleId_Throw_ArgumentException(
            int eventDungeonId) =>
            Assert.Throws<ArgumentException>(() =>
                eventDungeonId.ToEventScheduleId());

        [Theory]
        [InlineData(0, 0, 0, 0L)]
        [InlineData(0, 0, 1, 0L)]
        [InlineData(0, 0, 10, 0L)]
        [InlineData(int.MaxValue, 0, 0, int.MaxValue)]
        [InlineData(int.MaxValue, 0, 1, int.MaxValue)]
        [InlineData(int.MaxValue, 0, 10, int.MaxValue)]
        [InlineData(0, int.MaxValue, 0, 0)]
        [InlineData(0, int.MaxValue, 1, int.MaxValue)]
        [InlineData(0, int.MaxValue, 10, int.MaxValue * 10L)]
        [InlineData(int.MaxValue, int.MaxValue, 0, int.MaxValue)]
        [InlineData(int.MaxValue, int.MaxValue, 1, int.MaxValue * 2L)]
        [InlineData(int.MaxValue, int.MaxValue, 10, int.MaxValue * 11L)]
        public void GetDungeonTicketCost(
            int dungeonTicketPrice,
            int dungeonTicketAdditionalPrice,
            int numberOfTicketPurchases,
            long expectedCost)
        {
            var scheduleRow = new EventScheduleSheet.Row();
            scheduleRow.Set(new[]
            {
                "0",
                "0",
                "0",
                "0",
                "0",
                dungeonTicketPrice.ToString(),
                dungeonTicketAdditionalPrice.ToString(),
                "0",
                "0",
            });
            var cost = scheduleRow.GetDungeonTicketCost(numberOfTicketPurchases);
            Assert.Equal(expectedCost, cost);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void GetDungeonTicketCost_Throw_ArgumentException(
            int numberOfTicketPurchases) =>
            Assert.Throws<ArgumentException>(() =>
                GetDungeonTicketCost(
                    default,
                    default,
                    numberOfTicketPurchases,
                    default));

        [Theory]
        [InlineData(1001, 1, 1, 1, 1)]
        [InlineData(1001, 1, 10, 1, 1)]
        [InlineData(1001, 1, 11, 1, 2)]
        [InlineData(1001, 1, 20, 1, 2)]
        public void GetStageExp(
            int scheduleId,
            int expSeedValue,
            int stageNumber,
            int multiplier,
            int expected)
        {
            var scheduleRow = new EventScheduleSheet.Row();
            scheduleRow.Set(new[]
            {
                scheduleId.ToString(),
                "0",
                "0",
                "0",
                "0",
                "0",
                "0",
                expSeedValue.ToString(),
                "0",
            });
            var actual = scheduleRow.GetStageExp(stageNumber, multiplier);
            Assert.Equal(expected, actual);
        }
    }
}
