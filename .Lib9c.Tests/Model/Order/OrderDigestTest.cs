namespace Lib9c.Tests.Model.Order
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Libplanet.Assets;
    using Xunit;

    public class OrderDigestTest
    {
        private readonly Currency _currency;

        public OrderDigestTest()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
        }

        [Fact]
        public void Serialize()
        {
            var digest = new OrderDigest(
                default,
                1,
                2,
                Guid.NewGuid(),
                Guid.NewGuid(),
                new FungibleAssetValue(_currency, 100, 0),
                1,
                3,
                100,
                1
            );
            Dictionary serialized = (Dictionary)digest.Serialize();
            Assert.Equal(digest, new OrderDigest(serialized));
        }

        [Fact]
        public void Serialize_DotNet_Api()
        {
            var digest = new OrderDigest(
                default,
                1,
                2,
                Guid.NewGuid(),
                Guid.NewGuid(),
                new FungibleAssetValue(_currency, 100, 0),
                1,
                3,
                100,
                1
            );

            BinaryFormatter formatter = new BinaryFormatter();
            using MemoryStream ms = new MemoryStream();
            formatter.Serialize(ms, digest);
            ms.Seek(0, SeekOrigin.Begin);

            OrderDigest deserialized = (OrderDigest)formatter.Deserialize(ms);
            Assert.Equal(digest, deserialized);
        }
    }
}
