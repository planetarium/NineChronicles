namespace Lib9c.Tests.Model
{
    using System;
    using Nekoyume.Model.Event;
    using Xunit;

    public class EventDungeonInfoTest
    {
        [Fact]
        public void Serialize()
        {
            var eventDungeonClearState = new EventDungeonInfo();
            eventDungeonClearState.ClearStage(1);
            var serialized = eventDungeonClearState.Serialize();
            var deserialized = new EventDungeonInfo(serialized);
            Assert.Equal(eventDungeonClearState, deserialized);
            var reSerialized = deserialized.Serialize();
            Assert.Equal(serialized, reSerialized);
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        public void ResetTickets_Throw_ArgumentException(int tickets)
        {
            var eventDungeonClearState = new EventDungeonInfo();
            Assert.Throws<ArgumentException>(() =>
                eventDungeonClearState.ResetTickets(tickets));
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        public void HasTickets_Throw_ArgumentException(int tickets)
        {
            var eventDungeonClearState = new EventDungeonInfo();
            Assert.Throws<ArgumentException>(() =>
                eventDungeonClearState.HasTickets(tickets));
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        public void TryUseTickets_Throw_ArgumentException(int tickets)
        {
            var eventDungeonClearState = new EventDungeonInfo();
            Assert.Throws<ArgumentException>(() =>
                eventDungeonClearState.TryUseTickets(tickets));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void ResetTickets_And_HasTickets(int tickets)
        {
            var eventDungeonClearState = new EventDungeonInfo();
            eventDungeonClearState.ResetTickets(tickets);
            for (var i = 0; i < tickets + 2; i++)
            {
                if (i < tickets + 1)
                {
                    Assert.True(eventDungeonClearState.HasTickets(i));
                }
                else
                {
                    Assert.False(eventDungeonClearState.HasTickets(i));
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void ResetTickets_And_TryUseTickets(int tickets)
        {
            var eventDungeonClearState = new EventDungeonInfo();
            eventDungeonClearState.ResetTickets(tickets);
            for (var i = 0; i < tickets + 1; i++)
            {
                if (i < tickets)
                {
                    Assert.True(eventDungeonClearState.TryUseTickets(1));
                }
                else
                {
                    Assert.False(eventDungeonClearState.TryUseTickets(1));
                }
            }

            eventDungeonClearState.ResetTickets(tickets);
            Assert.True(eventDungeonClearState.TryUseTickets(tickets));
            Assert.False(eventDungeonClearState.TryUseTickets(1));
        }

        [Theory]
        [InlineData(10010001)]
        [InlineData(10010010)]
        public void ClearStage_And_IsCleared(int stageId)
        {
            var eventDungeonClearState = new EventDungeonInfo();
            eventDungeonClearState.ClearStage(stageId);
            for (var i = 10010001; i < stageId + 2; i++)
            {
                if (i < stageId + 1)
                {
                    Assert.True(eventDungeonClearState.IsCleared(i));
                }
                else
                {
                    Assert.False(eventDungeonClearState.IsCleared(i));
                }
            }
        }
    }
}
