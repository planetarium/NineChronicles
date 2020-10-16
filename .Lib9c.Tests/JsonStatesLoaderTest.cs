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
    using Org.BouncyCastle.Utilities.Encoders;
    using Xunit;

    public class JsonStatesLoaderTest
    {
        [Fact]
        public void Load()
        {
            var codec = new Codec();
            Text barValue = (Text)"bar";

            byte[] Base64Encode(byte[] data)
            {
                var encoder = new Base64Encoder();
                using var stream = new MemoryStream();
                encoder.Encode(data, 0, data.Length, stream);
                return stream.ToArray();
            }

            string Base64EncodeString(byte[] data)
                => Encoding.UTF8.GetString(Base64Encode(data));

            IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { "/foo", new MockFileData($"{{\"foo\": \"{Base64EncodeString(codec.Encode(barValue))}\"}}") },
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
