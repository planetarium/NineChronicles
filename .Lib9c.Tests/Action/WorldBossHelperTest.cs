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
            var currency = new Currency("NCG", decimalPlaces: 2, minters: null);
            var raiderState = new RaiderState
            {
                PurchaseCount = purchaseCount,
            };
            Assert.Equal(expected * currency, WorldBossHelper.CalculateTicketPrice(row, raiderState, currency));
        }
    }
}
