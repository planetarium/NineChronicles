using System;
using System.IO;
using static Launcher.RuntimePlatform.RuntimePlatform;

namespace Launcher.Storage
{
    // TODO: make IStorage.
    public class S3Storage
    {
        // TODO: it should be configurable.
        private const string S3Host = "9c-test.s3.ap-northeast-2.amazonaws.com";

        private const ushort HttpPort = 80;

        private const string VersionHistoryFilename = "versions.json";

        private static Uri BuildS3Uri(string path) =>
            new UriBuilder(
                Uri.UriSchemeHttp,
                S3Host,
                HttpPort,
                path
            ).Uri;

        // TODO: the path should be separated with version one more time.
        public Uri GameBinaryDownloadUri(string deployBranch) =>
            BuildS3Uri(Path.Combine(deployBranch, CurrentPlatform.GameBinaryDownloadFilename));

        public Uri VersionHistoryUri(string deployBranch) =>
            BuildS3Uri(Path.Combine(deployBranch, VersionHistoryFilename));
    }
}
