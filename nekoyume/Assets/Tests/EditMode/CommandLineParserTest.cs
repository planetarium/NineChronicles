using Nekoyume.Helper;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class CommandLineParserTest
    {
        private const string PrivateKey =
            "00000102030405060708090a0102030405060708090b0102030405060708090c";

        [Test]
        public void FilterSensitiveArgs_PrivateKeyWithEqualsSign()
        {
            var args = new[] { "Nine Chronicles", $"--private-key={PrivateKey}", "--port", "5555" };
            CollectionAssert.AreEqual(
                new[] { "Nine Chronicles", "--port", "5555" },
                CommandLineParser.FilterSensitiveArgs(args));
        }

        [Test]
        public void FilterSensitiveArgs_PrivateKeyAsSeparateArgument()
        {
            var args = new[] { "Nine Chronicles", "--private-key", PrivateKey, "--port", "5555" };
            CollectionAssert.AreEqual(
                new[] { "Nine Chronicles", "--port", "5555" },
                CommandLineParser.FilterSensitiveArgs(args));
        }

        [Test]
        public void FilterSensitiveArgs_PrivateKeyOptionIsCaseInsensitive()
        {
            var args = new[] { "--Private-Key", PrivateKey, "--console-sink" };
            CollectionAssert.AreEqual(
                new[] { "--console-sink" },
                CommandLineParser.FilterSensitiveArgs(args));
        }

        [Test]
        public void FilterSensitiveArgs_KeepsNextOptionWhenPrivateKeyValueIsMissing()
        {
            var args = new[] { "--private-key", "--console-sink" };
            CollectionAssert.AreEqual(
                new[] { "--console-sink" },
                CommandLineParser.FilterSensitiveArgs(args));
        }

        [Test]
        public void FilterSensitiveArgs_PrivateKeyAsLastArgument()
        {
            var args = new[] { "--console-sink", "--private-key" };
            CollectionAssert.AreEqual(
                new[] { "--console-sink" },
                CommandLineParser.FilterSensitiveArgs(args));
        }

        [Test]
        public void FilterSensitiveArgs_KeepsArgsWithoutPrivateKey()
        {
            var args = new[] { "Nine Chronicles", "--host", "localhost", "--port", "5555" };
            CollectionAssert.AreEqual(args, CommandLineParser.FilterSensitiveArgs(args));
        }
    }
}
