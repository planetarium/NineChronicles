namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.IO.Abstractions.TestingHelpers;
    using System.Text;
    using Bencodex;
    using Bencodex.Types;
    using Libplanet;
    using Xunit;

    public class JsonStatesLoaderTest
    {
        [Fact]
        public void Load()
        {
            var codec = new Codec();
            Text barValue = (Text)"bar";
            IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { "/foo", new MockFileData($"{{\"foo\": \"{Convert.ToBase64String(codec.Encode(barValue))}\"}}") },
            });
            var loader = new JsonStatesLoader(fileSystem);
            var states = loader.Load("/foo");
            Assert.Single(states);
            Assert.Equal(barValue, states["foo"]);
        }

        [Fact]
        public void ThrowsWhenFileNotFound()
        {
            IFileSystem fileSystem = new MockFileSystem();
            var loader = new JsonStatesLoader(fileSystem);
            Assert.Throws<FileNotFoundException>(() => loader.Load("/foo"));
        }

        [Fact]
        public void ThrowsWhenPassedNull()
        {
            IFileSystem fileSystem = new MockFileSystem();
            var loader = new JsonStatesLoader(fileSystem);
            Assert.Throws<ArgumentNullException>(() => loader.Load(null));
        }
    }
}
