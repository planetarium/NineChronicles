using System;
using System.Globalization;
using Nekoyume.TableData;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class TableExtensionsTest
    {
        private const string Value = "5.6";
        private static readonly CultureInfo CultureInfo = new CultureInfo("de-DE");

        [Test]
        public void TryParseDecimal()
        {
            TableExtensions.TryParseDecimal(Value, out var expected);
            decimal.TryParse(Value, NumberStyles.Number, CultureInfo.NumberFormat, out var wrong);
            Assert.AreEqual(56m, wrong);
            Assert.AreEqual(5.6m, expected);
        }

        [Test]
        public void TryParseFloat()
        {
            TableExtensions.TryParseFloat(Value, out var expected);
            float.TryParse(Value, NumberStyles.Number, CultureInfo.NumberFormat, out var wrong);
            Assert.AreEqual(56f, wrong);
            Assert.AreEqual(5.6f, expected);
        }

        [Test]
        public void TryParseLong()
        {
            TableExtensions.TryParseFloat("700", out var expected);
            long.TryParse("700", NumberStyles.Number, CultureInfo.NumberFormat, out var wrong);
            Assert.AreEqual(700, wrong);
            Assert.AreEqual(700, expected);
        }

        [Test]
        public void TryParseInt()
        {
            TableExtensions.TryParseInt("700", out var expected);
            int.TryParse("700", NumberStyles.Number, CultureInfo.NumberFormat, out var wrong);
            Assert.AreEqual(700, wrong);
            Assert.AreEqual(700, expected);
        }

        [Test]
        public void ParseInt()
        {
            var expected = TableExtensions.ParseInt("700");
            Assert.AreEqual(700, expected);
            Assert.Throws<ArgumentException>(() => TableExtensions.ParseInt(""));
        }

        [Test]
        public void ParseDecimal()
        {
            var expected = TableExtensions.ParseDecimal(Value);
            Assert.AreEqual(5.6m, expected);
            Assert.Throws<ArgumentException>(() => TableExtensions.ParseDecimal(""));
        }

        [Test]
        public void ParseLong()
        {
            var expected = TableExtensions.ParseLong("700");
            Assert.AreEqual(700, expected);
            Assert.Throws<ArgumentException>(() => TableExtensions.ParseLong(""));

        }
    }
}
