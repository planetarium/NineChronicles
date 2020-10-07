namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Bencodex;
    using Bencodex.Types;
    using Libplanet;
    using Zio;
    using Zio.FileSystems;
    using JsonSerializer = System.Text.Json.JsonSerializer;

    public class JsonStatesLoader
    {
        private readonly IFileSystem _fileSystem;

        public JsonStatesLoader(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public static JsonStatesLoader Default => new JsonStatesLoader(new PhysicalFileSystem());

        public Dictionary<string, IValue> Load(string jsonFilePath)
        {
            if (jsonFilePath is null)
            {
                throw new ArgumentNullException(nameof(jsonFilePath));
            }

            if (!_fileSystem.FileExists(jsonFilePath))
            {
                throw new FileNotFoundException();
            }

            string rawJsonString = _fileSystem.ReadAllText(jsonFilePath);
            Dictionary<string, string> json = JsonSerializer.Deserialize<Dictionary<string, string>>(rawJsonString);
            var codec = new Codec();
            return json.ToDictionary(pair => pair.Key, pair => codec.Decode(ByteUtil.ParseHex(pair.Value)));
        }
    }
}
