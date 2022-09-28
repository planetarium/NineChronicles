namespace Lib9c.Tests.Action
{
    using Libplanet.Assets;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class WorldBossHelperTest
    {
        [Theory]
        [InlineData(10, 10, 0, 10)]
        [InlineData(10, 10, 1, 20)]
        [InlineData(10, 10, 5, 60)]
        public void CalculateTicketPrice(int ticketPrice, int additionalTicketPrice, int purchaseCount, int expected)
        {
            var row = new WorldBossListSheet.Row
            {
                TicketPrice = ticketPrice,
                AdditionalTicketPrice = additionalTicketPrice,
            };
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var raiderState = new RaiderState
            {
                PurchaseCount = purchaseCount,
            };
            Assert.Equal(expected * currency, WorldBossHelper.CalculateTicketPrice(row, raiderState, currency));
        }

        [Theory]
        [InlineData(7200L, 0L, 0L, true)]
        [InlineData(7250L, 7180L, 0L, true)]
        [InlineData(14400L, 14399L, 0L, true)]
        [InlineData(7250L, 7210L, 0L, false)]
        [InlineData(17200L, 10003L, 10000L, true)]
        [InlineData(17199L, 10003L, 10000L, false)]
        public void CanRefillTicket(long blockIndex, long refilledBlockIndex, long startedBlockIndex, bool expected)
        {
            Assert.Equal(expected, WorldBossHelper.CanRefillTicket(blockIndex, refilledBlockIndex, startedBlockIndex));
        }
    }
}
