namespace Lib9c.Tests.Model.Order
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Nekoyume.Model.State;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class OrderBaseTest
    {
        [Theory]
        [InlineData(-1, 0, typeof(ArgumentOutOfRangeException))]
        [InlineData(0, 0, null)]
        [InlineData(0, -1, typeof(ArgumentOutOfRangeException))]
        [InlineData(2, 1, typeof(ArgumentOutOfRangeException))]
        [InlineData(2, 2, null)]
        public void OrderBase(long startedBlockIndex, long expiredBlockIndex, Type exc)
        {
            if (exc is null)
            {
                var orderBase = new OrderBase(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    startedBlockIndex,
                    expiredBlockIndex
                );

                Assert.Equal(startedBlockIndex, orderBase.StartedBlockIndex);
                Assert.Equal(expiredBlockIndex, orderBase.ExpiredBlockIndex);
                Assert.Equal(orderBase, new OrderBase((Dictionary)orderBase.Serialize()));
            }
            else
            {
                Assert.Throws(exc, () => new OrderBase(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        startedBlockIndex,
                        expiredBlockIndex
                    )
                );

                var dict = (Dictionary)new OrderBase(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    0,
                    0
                ).Serialize();
                dict = dict
                    .SetItem(StartedBlockIndexKey, startedBlockIndex.Serialize())
                    .SetItem(ExpiredBlockIndexKey, expiredBlockIndex.Serialize());

                Assert.Throws(exc, () => new OrderDigest(dict));
            }
        }

        [Fact]
        public void Serialize()
        {
            var orderBase = new OrderBase(
                Guid.NewGuid(),
                Guid.NewGuid(),
                1,
                2
            );

            Assert.Equal(orderBase, new OrderBase((Dictionary)orderBase.Serialize()));

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, orderBase);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (OrderBase)formatter.Deserialize(ms);

            Assert.Equal(orderBase, deserialized);
            Assert.Equal(orderBase.Serialize(), deserialized.Serialize());
        }
    }
}
