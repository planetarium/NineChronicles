namespace Lib9c.Tests.Model.State
{
    using System.Collections.Generic;
    using System.Numerics;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Model.State;
    using Xunit;

    public class StateExtensionsTest
    {
        public static IEnumerable<object[]> Get_Long_MemberData()
        {
            yield return new object[] { long.MinValue };
            yield return new object[] { 0L };
            yield return new object[] { long.MaxValue };
        }

        public static IEnumerable<object[]> Get_BigInteger_MemberData()
        {
            yield return new object[] { (BigInteger)decimal.MinValue };
            yield return new object[] { (BigInteger)0 };
            yield return new object[] { (BigInteger)decimal.MaxValue };
        }

        [Fact]
        public void Address()
        {
            var addr = new PrivateKey().ToAddress();
            var ser = addr.Serialize();
            var de = ser.ToAddress();
            Assert.Equal(addr, de);
            var ser2 = de.Serialize();
            Assert.Equal(ser, ser2);
        }

        [Fact]
        public void NullableAddress()
        {
            Address? addr = new PrivateKey().ToAddress();
            var ser = addr.Serialize();
            var de = ser.ToNullableAddress();
            Assert.Equal(addr, de);
            var ser2 = de.Serialize();
            Assert.Equal(ser, ser2);
        }

        [Theory]
        [MemberData(nameof(Get_Long_MemberData))]
        public void Long(long value)
        {
            var ser = value.Serialize();
            var de = ser.ToLong();
            Assert.Equal(value, de);
            var ser2 = de.Serialize();
            Assert.Equal(ser, ser2);
        }

        [Theory]
        [MemberData(nameof(Get_Long_MemberData))]
        public void NullableLong(long? value)
        {
            var ser = value.Serialize();
            var de = ser.ToNullableLong();
            Assert.Equal(value, de);
            var ser2 = de.Serialize();
            Assert.Equal(ser, ser2);
        }

        [Theory]
        [MemberData(nameof(Get_BigInteger_MemberData))]
        public void BigInteger(BigInteger value)
        {
            var ser = value.Serialize();
            var de = ser.ToBigInteger();
            Assert.Equal(value, de);
            var ser2 = de.Serialize();
            Assert.Equal(ser, ser2);
        }

        [Theory]
        [MemberData(nameof(Get_BigInteger_MemberData))]
        public void NullableBigInteger(BigInteger? value)
        {
            var ser = value.Serialize();
            var de = ser.ToNullableBigInteger();
            Assert.Equal(value, de);
            var ser2 = de.Serialize();
            Assert.Equal(ser, ser2);
        }
    }
}
