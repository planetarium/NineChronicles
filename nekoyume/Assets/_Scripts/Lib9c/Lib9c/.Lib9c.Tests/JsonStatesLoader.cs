namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using Bencodex;
    using Bencodex.Types;
    using JsonSerializer = System.Text.Json.JsonSerializer;

    public class JsonStatesLoader
    {
        private readonly IFileSystem _fileSystem;

        public JsonStatesLoader(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public static JsonStatesLoader Default => new JsonStatesLoader(new FileSystem());

        public Dictionary<string, IValue> Load(string jsonFilePath)
        {
            if (jsonFilePath is null)
            {
                throw new ArgumentNullException(nameof(jsonFilePath));
            }

            if (!_fileSystem.File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException();
            }

            string rawJsonString = _fileSystem.File.ReadAllText(jsonFilePath);
            Dictionary<string, byte[]> json = JsonSerializer.Deserialize<Dictionary<string, byte[]>>(rawJsonString);
            var codec = new Codec();
            return json.ToDictionary(pair => pair.Key, pair => codec.Decode(pair.Value));
        }
    }
}
