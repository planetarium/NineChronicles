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
            var eventDungeonInfo = new EventDungeonInfo();
            eventDungeonInfo.ResetTickets(1, 10);
            eventDungeonInfo.ClearStage(1);
            var serialized = eventDungeonInfo.Serialize();
            var deserialized = new EventDungeonInfo(serialized);
            Assert.Equal(eventDungeonInfo, deserialized);
            var reSerialized = deserialized.Serialize();
            Assert.Equal(serialized, reSerialized);
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue)]
        public void Constructor(
            int resetTicketsInterval,
            int remainingTickets,
            int numberOfTicketPurchases,
            int clearedStageId)
        {
            var eventDungeonInfo = new EventDungeonInfo(
                resetTicketsInterval,
                remainingTickets,
                numberOfTicketPurchases,
                clearedStageId);
            Assert.Equal(
                resetTicketsInterval,
                eventDungeonInfo.ResetTicketsInterval);
            Assert.Equal(
                remainingTickets,
                eventDungeonInfo.RemainingTickets);
            Assert.Equal(
                numberOfTicketPurchases,
                eventDungeonInfo.NumberOfTicketPurchases);
            Assert.Equal(
                clearedStageId,
                eventDungeonInfo.ClearedStageId);
        }

        [Theory]
        [InlineData(-1, 0, 0, 0)]
        [InlineData(0, -1, 0, 0)]
        [InlineData(0, 0, -1, 0)]
        [InlineData(0, 0, 0, -1)]
        [InlineData(int.MinValue, int.MinValue, int.MinValue, int.MinValue)]
        public void Constructor_Throw_ArgumentException(
            int resetTicketsInterval,
            int remainingTickets,
            int numberOfTicketPurchases,
            int clearedStageId) =>
            Assert.Throws<ArgumentException>(() =>
                new EventDungeonInfo(
                    resetTicketsInterval,
                    remainingTickets,
                    numberOfTicketPurchases,
                    clearedStageId));

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void ResetTickets_And_HasTickets(int tickets)
        {
            var eventDungeonInfo = new EventDungeonInfo();
            eventDungeonInfo.ResetTickets(1, tickets);
            for (var i = 0; i < tickets + 2; i++)
            {
                if (i < tickets + 1)
                {
                    Assert.True(eventDungeonInfo.HasTickets(i));
                }
                else
                {
                    Assert.False(eventDungeonInfo.HasTickets(i));
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void ResetTickets_And_TryUseTickets(int tickets)
        {
            var eventDungeonInfo = new EventDungeonInfo();
            eventDungeonInfo.ResetTickets(1, tickets);
            for (var i = 0; i < tickets + 1; i++)
            {
                if (i < tickets)
                {
                    Assert.True(eventDungeonInfo.TryUseTickets(1));
                }
                else
                {
                    Assert.False(eventDungeonInfo.TryUseTickets(1));
                }
            }

            eventDungeonInfo.ResetTickets(2, tickets);
            Assert.True(eventDungeonInfo.TryUseTickets(tickets));
            Assert.False(eventDungeonInfo.TryUseTickets(1));
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, 0)]
        [InlineData(0, -1)]
        [InlineData(int.MinValue, int.MinValue)]
        public void ResetTickets_Throw_ArgumentException(
            int resetTicketsInterval,
            int tickets)
        {
            var eventDungeonInfo = new EventDungeonInfo();
            Assert.Throws<ArgumentException>(() =>
                eventDungeonInfo.ResetTickets(resetTicketsInterval, tickets));
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        public void HasTickets_Throw_ArgumentException(int tickets)
        {
            var eventDungeonInfo = new EventDungeonInfo();
            Assert.Throws<ArgumentException>(() =>
                eventDungeonInfo.HasTickets(tickets));
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        public void TryUseTickets_Throw_ArgumentException(int tickets)
        {
            var eventDungeonInfo = new EventDungeonInfo();
            Assert.Throws<ArgumentException>(() =>
                eventDungeonInfo.TryUseTickets(tickets));
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(0, 10)]
        [InlineData(int.MaxValue - 1, 1)]
        public void IncreaseNumberOfTicketPurchases(
            int initialNumberOfTicketPurchases,
            int numberOfIncrease)
        {
            var eventDungeonInfo = new EventDungeonInfo(
                numberOfTicketPurchases: initialNumberOfTicketPurchases);
            for (var i = 0; i < numberOfIncrease; i++)
            {
                eventDungeonInfo.IncreaseNumberOfTicketPurchases();
            }

            Assert.Equal(
                initialNumberOfTicketPurchases + numberOfIncrease,
                eventDungeonInfo.NumberOfTicketPurchases);
        }

        [Theory]
        [InlineData(int.MaxValue, 1)]
        [InlineData(int.MaxValue - 10, 11)]
        public void IncreaseNumberOfTicketPurchases_Throw_InvalidOperationException(
            int initialNumberOfTicketPurchases,
            int numberOfIncrease)
        {
            var eventDungeonInfo = new EventDungeonInfo(
                numberOfTicketPurchases: initialNumberOfTicketPurchases);
            Assert.Throws<InvalidOperationException>(() =>
            {
                for (var i = 0; i < numberOfIncrease; i++)
                {
                    eventDungeonInfo.IncreaseNumberOfTicketPurchases();
                }
            });
        }

        [Theory]
        [InlineData(10010001)]
        [InlineData(10010010)]
        public void ClearStage_And_IsCleared(int stageId)
        {
            var eventDungeonInfo = new EventDungeonInfo();
            eventDungeonInfo.ClearStage(stageId);
            for (var i = 10010001; i < stageId + 2; i++)
            {
                if (i < stageId + 1)
                {
                    Assert.True(eventDungeonInfo.IsCleared(i));
                }
                else
                {
                    Assert.False(eventDungeonInfo.IsCleared(i));
                }
            }
        }
    }
}
