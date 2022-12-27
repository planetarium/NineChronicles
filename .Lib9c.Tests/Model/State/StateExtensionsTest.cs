namespace Lib9c.Tests.Model.State
{
    using System.Numerics;
    using Nekoyume.Model.State;
    using Xunit;

    public class StateExtensionsTest
    {
        [Theory]
        [InlineData(long.MinValue)]
        [InlineData(0L)]
        [InlineData(long.MaxValue)]
        public void Long(long value)
        {
            var ser = value.Serialize();
            var de = ser.ToLong();
            Assert.Equal(value, de);
            var ser2 = de.Serialize();
            Assert.Equal(ser, ser2);
        }

        [Theory]
        [InlineData(long.MinValue)]
        [InlineData(0L)]
        [InlineData(long.MaxValue)]
        public void NullableLong(long? value)
        {
            var ser = value.Serialize();
            var de = ser.ToNullableLong();
            Assert.Equal(value, de);
            var ser2 = de.Serialize();
            Assert.Equal(ser, ser2);
        }

        [Fact]
        public void SerializeBigInteger()
        {
            Assert.Equal(
                (Bencodex.Types.Integer)123,
                StateExtensions.Serialize((BigInteger)123)
            );
            Assert.Equal(
                (Bencodex.Types.Integer)456,
                StateExtensions.Serialize((BigInteger?)456)
            );
            Assert.Equal(
                Bencodex.Types.Null.Value,
                StateExtensions.Serialize((BigInteger?)null)
            );
        }

        [Fact]
        public void DeserializeBigInteger()
        {
            Assert.Equal(
                (BigInteger)123,
                new Bencodex.Types.Integer(123).ToBigInteger()
            );
            Assert.Equal(
                (BigInteger?)123,
                new Bencodex.Types.Integer(123).ToNullableBigInteger()
            );
            Assert.Equal(
                (BigInteger?)null,
                Bencodex.Types.Null.Value.ToNullableBigInteger()
            );
        }
    }
}
