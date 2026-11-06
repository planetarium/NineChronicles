using Nekoyume;
using NUnit.Framework;

namespace Tests.EditMode.Extensions
{
    public class BigIntegerExtensionsTest
    {
        [TestCase(999L, "999")]
        [TestCase(1_500L, "1.5K")]
        [TestCase(2_000_000L, "2M")]
        [TestCase(999_990_000_000L, "999.99B")]
        [TestCase(1_000_000_000_000L, "1T")]
        [TestCase(1_234_560_000_000L, "1.23T")]
        [TestCase(5_000_000_000_000_000L, "5,000T")]
        public void ToCurrencyNotation_Long(long value, string expected)
        {
            Assert.AreEqual(expected, value.ToCurrencyNotation());
        }
    }
}
