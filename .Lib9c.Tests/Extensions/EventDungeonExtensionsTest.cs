namespace Lib9c.Tests.Extensions
{
    using System;
    using Nekoyume.Extensions;
    using Xunit;

    public class EventDungeonExtensionsTest
    {
        [Theory]
        [InlineData(10_000_000, 0)]
        [InlineData(99_999_999, 9_999)]
        public void ToEventDungeonStageNumber(
            int eventDungeonStageId,
            int expected) =>
            Assert.Equal(
                expected,
                eventDungeonStageId.ToEventDungeonStageNumber());

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
        public void ToEventDungeonStageNumber_Throw_ArgumentException(
            int eventDungeonStageId) =>
            Assert.Throws<ArgumentException>(() =>
                eventDungeonStageId.ToEventDungeonStageNumber());
    }
}
