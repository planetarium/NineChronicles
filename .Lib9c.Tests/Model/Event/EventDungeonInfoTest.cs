namespace Lib9c.Tests.Model
{
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

        [Fact]
        public void ResetTickets_And_HasTickets()
        {
            var eventDungeonClearState = new EventDungeonInfo();
            eventDungeonClearState.ResetTickets(1);
            Assert.True(eventDungeonClearState.HasTickets(1));
            Assert.False(eventDungeonClearState.HasTickets(2));
            eventDungeonClearState.ResetTickets(2);
            Assert.True(eventDungeonClearState.HasTickets(1));
            Assert.True(eventDungeonClearState.HasTickets(2));
            Assert.False(eventDungeonClearState.HasTickets(3));
        }

        [Fact]
        public void ResetTickets_And_TryUseTickets()
        {
            var eventDungeonClearState = new EventDungeonInfo();
            eventDungeonClearState.ResetTickets(1);
            Assert.True(eventDungeonClearState.TryUseTicket(1));
            Assert.False(eventDungeonClearState.TryUseTicket(1));
            eventDungeonClearState.ResetTickets(2);
            Assert.True(eventDungeonClearState.TryUseTicket(1));
            Assert.True(eventDungeonClearState.TryUseTicket(1));
            Assert.False(eventDungeonClearState.TryUseTicket(1));
            eventDungeonClearState.ResetTickets(3);
            Assert.True(eventDungeonClearState.TryUseTicket(2));
            eventDungeonClearState.ResetTickets(3);
            Assert.True(eventDungeonClearState.TryUseTicket(3));
            eventDungeonClearState.ResetTickets(3);
            Assert.False(eventDungeonClearState.TryUseTicket(4));
        }

        [Fact]
        public void ClearStage_And_IsCleared()
        {
            var eventDungeonClearState = new EventDungeonInfo();
            eventDungeonClearState.ClearStage(1);
            Assert.True(eventDungeonClearState.IsCleared(1));
            Assert.False(eventDungeonClearState.IsCleared(2));
            eventDungeonClearState.ClearStage(2);
            Assert.True(eventDungeonClearState.IsCleared(1));
            Assert.True(eventDungeonClearState.IsCleared(2));
            Assert.False(eventDungeonClearState.IsCleared(3));
            eventDungeonClearState.ClearStage(1);
            Assert.True(eventDungeonClearState.IsCleared(1));
            Assert.True(eventDungeonClearState.IsCleared(2));
            Assert.False(eventDungeonClearState.IsCleared(3));
        }
    }
}
