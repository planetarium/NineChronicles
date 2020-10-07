namespace Lib9c.Tests.Model.State
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Nekoyume.Model.State;
    using Xunit;

    public class AuthorizedMinersStateTest
    {
        [Fact]
        public void Serialize()
        {
            var miners = GetNewMiners();
            var state = new AuthorizedMinersState(
                miners: miners,
                interval: 50,
                validUntil: 1000
            );

            var serialized = (Dictionary)state.Serialize();
            var deserialized = new AuthorizedMinersState(serialized);

            Assert.Equal(miners, deserialized.Miners);
            Assert.Equal(50, deserialized.Interval);
            Assert.Equal(1000, deserialized.ValidUntil);
        }

        private static Address[] GetNewMiners()
        {
            return new[]
            {
                new Address(
                    new byte[]
                    {
                        0x01, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                    }
                ),
                new Address(
                    new byte[]
                    {
                        0x02, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                    }
                ),
            };
        }
    }
}
