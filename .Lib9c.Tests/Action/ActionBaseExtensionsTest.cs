namespace Lib9c.Tests.Action
{
    using System.Collections.Immutable;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Xunit;

    public class ActionBaseExtensionsTest
    {
        [Fact]
        public void CalculateUpdateAddresses()
        {
            var actions = new ActionBase[]
            {
                new TransferAsset(
                    sender: default,
                    recipient: default,
                    amount: Currencies.DailyRewardRune * 1
                ),
            };

            IImmutableSet<Address> actual = actions.CalculateUpdateAddresses();
            Assert.Equal(
                new Address[]
                {
                    default,
                },
                actual
            );
        }

        [Fact]
        public void CalculateUpdateAddressesWithIncompatibles()
        {
            var actions = new ActionBase[]
            {
                new TransferAsset(
                    sender: default,
                    recipient: default,
                    amount: Currencies.DailyRewardRune * 1
                ),
                new HackAndSlashRandomBuff(),
            };

            IImmutableSet<Address> actual = actions.CalculateUpdateAddresses();
            Assert.Equal(
                new Address[]
                {
                    default,
                },
                actual
            );
        }
    }
}
