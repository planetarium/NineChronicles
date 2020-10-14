namespace Lib9c.Tests
{
    using System;
    using System.IO;
    using Bencodex;
    using Bencodex.Types;
    using Libplanet;
    using Xunit;
    using Zio;
    using Zio.FileSystems;

    public class JsonStatesLoaderTest
    {
        [Fact]
        public void Load()
        {
            IFileSystem fileSystem = new MemoryFileSystem();
            var codec = new Codec();
            Text barValue = (Text)"bar";
            fileSystem.WriteAllText("/foo", $"{{\"foo\": \"{ByteUtil.Hex(codec.Encode(barValue))}\"}}");
            var loader = new JsonStatesLoader(fileSystem);
            var states = loader.Load("/foo");
            Assert.Single(states);
            Assert.Equal(barValue, states["foo"]);
        }

        [Fact]
        public void ThrowsWhenFileNotFound()
        {
            IFileSystem fileSystem = new MemoryFileSystem();
            var loader = new JsonStatesLoader(fileSystem);
            Assert.Throws<FileNotFoundException>(() => loader.Load("/foo"));
        }

        [Fact]
        public void ThrowsWhenPassedNull()
        {
            IFileSystem fileSystem = new MemoryFileSystem();
            var loader = new JsonStatesLoader(fileSystem);
            Assert.Throws<ArgumentNullException>(() => loader.Load(null));
        }
    }
}
