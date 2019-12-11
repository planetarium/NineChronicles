using Mono.Options;
using ShellProgressBar;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml;

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
            string storePath,
            DownloadProgressChangedEventHandler downloadProgressHandler = null)
        {
            var client = new WebClient();
            if (downloadProgressHandler is DownloadProgressChangedEventHandler f)
            {
                client.DownloadProgressChanged += f;
            }
            string tmp = Path.GetTempFileName();
            try
            {
                client.DownloadFile(snapshotUrl, tmp);
                while (Directory.Exists(storePath))
                {
                    Directory.Delete(storePath, true);
                }

                Directory.CreateDirectory(storePath);
                ZipFile.ExtractToDirectory(tmp, storePath, System.Text.Encoding.UTF8);
            }
            finally
            {
                File.Delete(tmp);
            }
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
            console = console ?? Console.Error;
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
                    "b|bucket-url=",
                    $"The cloud bucket where snapshots are placed\n[{bucketUrl}]",
                    value => bucketUrl = new Uri(value)
                },
                {
                    "h|help",
                    "Show this message and exit",
                    value => help = true
                }
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
                (_, Uri latestSnapshotUri) = snapshots.OrderBy(p => p.Item1).Last();
                var progOptions = new ProgressBarOptions
                {
                    ProgressBarOnBottom = false,
                    DisplayTimeInRealTime = true,
                };
                ProgressBar progBar = null;
                Console.Error.WriteLine(
                    "Downloading the latest snapshot from {0}",
                    latestSnapshotUri
                );
                DownloadSnapshot(
                    latestSnapshotUri,
                    storePath,
                    downloadProgressHandler: (_, e) =>
                    {
                        if (progBar is null)
                        {
                            progBar = new ProgressBar(
                                (int) (e.TotalBytesToReceive / 1024L),
                                $"Downloading the latest snapshot from {latestSnapshotUri}",
                                progOptions
                            );
                        }

                        progBar.Tick((int) (e.BytesReceived / 1024L));

                        if (e.TotalBytesToReceive <= e.BytesReceived)
                        {
                            progBar.Dispose();
                            Console.Error.WriteLine("Finished to download the snapshop.");
                            Console.Error.WriteLine("Extracting the snapshot archive...");
                        }
                    }
                );
                Console.Error.WriteLine("Finished.");
            }

            return 0;
        }
    }
}
