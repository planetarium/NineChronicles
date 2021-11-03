using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Cocona;
using Libplanet;
using Libplanet.Blocks;
using Libplanet.RocksDBStore;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Lib9c.Tools.SubCommand
{
    public class Store
    {
        [Command(Description = "Migrate old store to new store.")]
        public void Migration(
            [Option('o', Description = "Path to migration target root path.")]
            string originRootPath,
            [Option('d', Description = "Path to migrated dist root path.")]
            string distRootPath,
            [Option('c', Description = "Skip the copy from target store.")]
            bool skipCopy,
            [Option('b', Description = "Skip the block from target store.")]
            bool skipBlock,
            [Option('t', Description = "Skip the transaction from target store.")]
            bool skipTransaction
            )
        {
            if (!skipCopy)
            {
                Console.WriteLine("Copy Start!");
                DirectoryCopy(originRootPath, distRootPath, true);
                Directory.Delete(Path.Combine(distRootPath, "tx"), true);
                Directory.Delete(Path.Combine(distRootPath, "block"), true);
                Console.WriteLine("Copy Success!");
            }
            else
            {
                Console.WriteLine("Skip copy");
            }

            var originStore = new MonoRocksDBStore(originRootPath);
            var distStore = new RocksDBStore(distRootPath);

            var totalLength = originStore.CountBlocks();
            if (!skipBlock)
            {
                Console.WriteLine("Start migrate block.");
                foreach (var item in
                    originStore.IterateBlockHashes().Select((value, i) => new {i, value}))
                {
                    Console.WriteLine($"block progress: {item.i}/{totalLength}");
                    Block<NCAction> block = originStore.GetBlock<NCAction>(
                        _ => HashAlgorithmType.Of<SHA256>(),  // thunk getter
                        item.value
                    );
                    distStore.PutBlock(block);
                }

                Console.WriteLine("Finish migrate block.");
            }

            totalLength = originStore.CountTransactions();
            if (!skipTransaction)
            {
                Console.WriteLine("Start migrate transaction.");
                foreach (var item in
                    originStore.IterateTransactionIds()
                        .Select((value, i) => new {i, value}))
                {
                    Console.WriteLine($"tx progress: {item.i}/{totalLength}");
                    distStore.PutTransaction(originStore.GetTransaction<NCAction>(item.value));
                }

                Console.WriteLine("Finish migrate transaction.");
            }

            originStore.Dispose();
            distStore.Dispose();

            Console.WriteLine("Migration Success!");
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
    }
}
