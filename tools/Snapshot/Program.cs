using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using Cocona;
using Libplanet;
using Libplanet.RocksDBStore;

namespace Snapshot
{
    class Program
    {
        static void Main(string[] args)
        {
            CoconaLiteApp.Run<Program>(args);
        }

        [Command]
        public void Snapshot(
            [Option('o')]
            string outputDirectory = null,
            string storePath = null)
        {
            string defaultStorePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "planetarium",
                "9c"
            );
            outputDirectory = string.IsNullOrEmpty(outputDirectory)
                ? Environment.CurrentDirectory
                : outputDirectory;
            storePath = string.IsNullOrEmpty(storePath) ? defaultStorePath : storePath;
            if (!Directory.Exists(storePath))
            {
                throw new CommandExitedException("Invalid store path. Please check --store-path is valid.", -1);
            }

            var store = new RocksDBStore(storePath);
            var canonicalChainId = store.GetCanonicalChainId();
            if (canonicalChainId is Guid chainId)
            {
                var genesisHash = store.IterateIndexes(chainId,0, 1).First();
                var tipHash = store.IterateIndexes(chainId, 0, null).Last();
                var tipDigest = store.GetBlockDigest(tipHash);
                store.Dispose();

                var genesisHashHex = ByteUtil.Hex(genesisHash.ToByteArray());
                var tipHashHex = ByteUtil.Hex(tipHash.ToByteArray());
                var snapshotFilename = $"{genesisHashHex}-snapshot-{tipHashHex}.zip";
                var snapshotPath = Path.Combine(outputDirectory, snapshotFilename);
                if (File.Exists(snapshotPath))
                {
                    File.Delete(snapshotPath);
                }

                ZipFile.CreateFromDirectory(storePath, snapshotPath);

                if (tipDigest is null)
                {
                    throw new CommandExitedException("Tip does not exists.", -1);
                }

                var tipHeader = tipDigest.Value.Header;
                var json = JsonSerializer.Serialize(tipHeader);
                var metadataFilename = $"{genesisHashHex}-snapshot-{tipHashHex}.meta";
                var metadataPath = Path.Combine(outputDirectory, metadataFilename);
                if (File.Exists(metadataPath))
                {
                    File.Delete(metadataPath);
                }

                File.WriteAllText(metadataPath, json);
            }
            else
            {
                throw new CommandExitedException("Canonical chain doesn't exists.", -1);
            }
        }
    }
}
