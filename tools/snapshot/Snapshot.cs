using Mono.Options;
using ShellProgressBar;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Store;

namespace NineChroniclesSnapshot
{
    public sealed class Snapshot
    {
        public static readonly Uri DefaultBucketUrl =
            new Uri("https://9c-data-snapshots.s3.ap-northeast-2.amazonaws.com/");

        public static string DefaultStorePath
        {
            get
            {
                string s = Path.Join(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "planetarium",
                    "9c"
                );
                return Path.GetFullPath(s);
            }
        }

        // unit: minutes
        public static int DefaultStaleTime => 3600; 

        public static bool IsStaled(string storePath, int staleTime, DateTimeOffset snapshotCreated)
        {
            if (Directory.Exists(storePath))
            {
                using var store = new DefaultStore(storePath);
                Guid chainId = store.GetCanonicalChainId() ?? throw new ArgumentNullException(nameof(chainId));
                var tipHash = store.IterateIndexes(chainId, (int)store.CountIndex(chainId) - 1, 1).First();
                var tip = store.GetBlock<DumbAction>(tipHash);
                Console.Error.Write($"tip.Timestamp: {tip.Timestamp} | snapshotCreated: {snapshotCreated}");
                return snapshotCreated - tip.Timestamp > TimeSpan.FromMinutes(staleTime);
            }
            else
            {
                return true;
            }
        }

        public static string MakeSnapshot(string sourcePath)
        {
            string filename = $"9c-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.zip";
            ZipFile.CreateFromDirectory(
                sourcePath,
                filename,
                CompressionLevel.Optimal,
                includeBaseDirectory: false,
                entryNameEncoding: System.Text.Encoding.UTF8
            );
            return filename;
        }

        public static IEnumerable<(string, Uri)> ListSnapshotUrls(Uri rootUrl)
        {
            var client = new WebClient();
            var doc = new XmlDocument();
            using (Stream data = client.OpenRead(rootUrl))
            {
                doc.Load(data);
            }

            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("s3", doc.DocumentElement.NamespaceURI);
            XmlNodeList nodes = doc.DocumentElement.SelectNodes(
                "/s3:ListBucketResult/s3:Contents/s3:Key/text()",
                nsmgr
            );

            var filenamePattern = new Regex(@"9c-[0-9]{14}\.zip");
            foreach (XmlNode node in nodes)
            {
                string filename = node.Value;
                if (filenamePattern.IsMatch(filename))
                {
                    yield return (filename, new Uri(rootUrl, filename));
                }
            }
        }

        public static void DownloadSnapshot(
            Uri snapshotUrl,
            string storePath)
        {
            var progOptions = new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Yellow,
                BackgroundColor = ConsoleColor.DarkGreen,
                ProgressCharacter = '-',
                BackgroundCharacter = '-',
                ProgressBarOnBottom = false,
                DisplayTimeInRealTime = true,
            };
            ProgressBar progBar = new ProgressBar(
                0,
                "Downloading",
                progOptions
            );

            var client = new WebClient();
            client.DownloadProgressChanged += (_, e) =>
            {
                progBar.MaxTicks = (int) (e.TotalBytesToReceive / 1024L);
                progBar.Tick((int) (e.BytesReceived / 1024L));
                progBar.Message = $"Downloading... ({(int) (e.BytesReceived / 1024L)}KB/{(int) (e.TotalBytesToReceive / 1024L)}KB)";
            };
            client.DownloadFileCompleted += (_, __) =>
            {
                progBar.Dispose();
                Console.WriteLine("Finished to download the snapshop.");
                Console.WriteLine("Extracting the snapshot archive...");
            };

            var oldStorePath = storePath + "_old";
            if (Directory.Exists(oldStorePath))
            {
                Directory.Delete(oldStorePath, recursive: true);
            }

            if (Directory.Exists(storePath))
            {
                Directory.Move(storePath, oldStorePath);
            }

            string tmp = Path.GetTempFileName();
            var downloadSnapshotTask = Task.Run(() =>
            {
                string tmp = Path.GetTempFileName();
                try
                {
                    Console.WriteLine($"Downloading the latest snapshot from {snapshotUrl}...");
                    client.DownloadFileTaskAsync(snapshotUrl, tmp).Wait();
                    Directory.CreateDirectory(storePath);
                    using (FileStream stream = new FileStream(tmp, FileMode.OpenOrCreate))
                    using (ZipArchive archive =
                        new ZipArchive(stream, ZipArchiveMode.Read, false, System.Text.Encoding.UTF8))
                    {
                        var totalProgress = archive.Entries.Count;
                        var count = 0;
                        progBar = new ProgressBar(
                            totalProgress,
                            $"Extracting files... (0/{totalProgress})",
                            progOptions
                        );

                        foreach (var entry in archive.Entries)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(storePath, entry.FullName)));
                            entry.ExtractToFile(Path.Combine(storePath, entry.FullName), true);
                            count++;

                            // update progess there
                            progBar.Tick(count);
                            progBar.Message = $"Extracting files... ({count}/{totalProgress})";
                        }

                        Console.WriteLine();
                    }
                }
                finally
                {
                    File.Delete(tmp);
                }
            });

            var removeOldStoreTask = Task.Run(() =>
            {
                while (Directory.Exists(storePath))
                {
                    Directory.Delete(storePath, true);
                }
            });

            Task.WaitAll(removeOldStoreTask, downloadSnapshotTask);
        }

        public static void WriteHelp(OptionSet options, TextWriter console = null)
        {
            console = console ?? Console.Out;
            console.WriteLine(
                "usage: {0} [OPTIONS]\noptions:",
                Path.GetFileName(Environment.GetCommandLineArgs()[0])
            );
            options.WriteOptionDescriptions(console);
        }

        public static int WriteError(OptionSet options, string message, TextWriter console = null)
        {
            console ??= Console.Error;
            console.WriteLine("error: {0}", message);
            WriteHelp(options, console);
            Environment.Exit(1);
            return 1;
        }

        public static int Main(string[] args)
        {
            bool help = false;
            bool dump = false;
            string storePath = DefaultStorePath;
            int staleTime = DefaultStaleTime; 
            Uri bucketUrl = DefaultBucketUrl;
            var options = new OptionSet
            {
                {
                    "d|dump-store",
                    "Do not import a snapshot, but make a new snapshot instead",
                    value => dump = !(value is null)
                },
                {
                    "s|store-path=",
                    $"The Nine Chronicles' data store directory\n[{storePath}]",
                    value => storePath = Directory.Exists(value)
                        ? value
                        : throw new OptionException("not a directory", "-s/--store-path")
                },
                {
                    "t|stale-time=",
                    "The interval to determine if the chain is older than network (unit: minutes)",
                    value => staleTime = int.Parse(value)
                },
                {
                    "b|bucket-url=",
                    $"The cloud bucket where snapshots are placed\n[{bucketUrl}]",
                    value => bucketUrl = new Uri(value)
                },
                {
                    "h|help",
                    "Show this message and exit",
                    value => help = true
                },
            };

            IList<string> extraArgs;
            try
            {
                extraArgs = options.Parse(args);
            }
            catch (OptionException e)
            {
                return WriteError(
                    options, e.OptionName is null ? e.Message : $"{e.OptionName}: {e.Message}"
                );
            }

            if (help)
            {
                WriteHelp(options);
                return 0;
            }
            else if (extraArgs.Any())
            {
                return WriteError(options, "unexpected arguments");
            }
            else if (dump)
            {
                string destination = MakeSnapshot(storePath);
                Console.WriteLine(destination);
            }
            else
            {
                var snapshots = ListSnapshotUrls(bucketUrl);
                (string filename, Uri latestSnapshotUri) = snapshots.OrderByDescending(p => p.Item1).First();
                var snapshotCreated = DateTimeOffset.ParseExact(
                    filename.Substring(3, 14),
                    new[] { "yyyyMMddHHmmss" },
                    CultureInfo.InvariantCulture).ToUniversalTime();
                if (IsStaled(storePath, staleTime, snapshotCreated))
                {
                    Console.WriteLine(
                        "Downloading the latest snapshot from {0}",
                        latestSnapshotUri
                    );
                    DownloadSnapshot(latestSnapshotUri, storePath);
                    Console.WriteLine("Finished.");
                }
            }

            return 0;
        }
    }
    
    class DumbAction : IAction
    {
        public void LoadPlainValue(IValue plainValue)
        {
        }

        public IAccountStateDelta Execute(IActionContext context)
        {
            return context.PreviousStates;
        }

        public void Render(IActionContext context, IAccountStateDelta nextStates)
        {
        }

        public void Unrender(IActionContext context, IAccountStateDelta nextStates)
        {
        }

        public IValue PlainValue => default(Null);
    }
}
