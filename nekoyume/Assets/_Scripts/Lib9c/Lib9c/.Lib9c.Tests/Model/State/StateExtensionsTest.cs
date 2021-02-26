namespace Lib9c.Tests.Model.State
{
    using System;
    using System.Numerics;
    using Nekoyume.Battle;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.State;
    using Xunit;

    public class StateExtensionsTest
    {
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
                default(Bencodex.Types.Null),
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
                default(Bencodex.Types.Null).ToNullableBigInteger()
            );
        }
    }
}
