using System;
using System.Globalization;
using Bencodex.Types;

namespace Nekoyume
{
    public struct AppProtocolVersionExtra
    {
        private readonly struct DownloadUrls
        {
            private const string MacOSBinaryUrlKey = "macOS";

            private const string WindowsBinaryUrlKey = "Windows";

            public readonly string MacOSBinaryUrl;

            public readonly string WindowsBinaryUrl;

            public DownloadUrls(string macOsBinaryUrl, string windowsBinaryUrl)
            {
                MacOSBinaryUrl = macOsBinaryUrl;
                WindowsBinaryUrl = windowsBinaryUrl;
            }

            public DownloadUrls(Bencodex.Types.Dictionary dictionary)
            {
                MacOSBinaryUrl = (Text) dictionary[MacOSBinaryUrlKey];
                WindowsBinaryUrl = (Text) dictionary[WindowsBinaryUrlKey];
            }

            public IValue Serialize()
            {
                return Bencodex.Types.Dictionary.Empty
                    .Add(MacOSBinaryUrlKey, MacOSBinaryUrl)
                    .Add(WindowsBinaryUrlKey, WindowsBinaryUrl);
            }
        }

        private const string DownloadUrlsKey = "downloadUrls";

        private const string TimestampKey = "timestamp";

        private readonly DownloadUrls _downloadUrls;

        public string MacOSBinaryUrl => _downloadUrls.MacOSBinaryUrl;

        public string WindowsBinaryUrl => _downloadUrls.WindowsBinaryUrl;

        public readonly DateTimeOffset Timestamp;

        public AppProtocolVersionExtra(string macOsBinaryUrl, string windowsBinaryUrl, DateTimeOffset timestamp)
        {
            _downloadUrls = new DownloadUrls(macOsBinaryUrl, windowsBinaryUrl);
            Timestamp = timestamp;
        }

        public AppProtocolVersionExtra(Bencodex.Types.Dictionary dictionary)
        {
            _downloadUrls = new DownloadUrls((Bencodex.Types.Dictionary) dictionary[DownloadUrlsKey]);
            Timestamp = DateTimeOffset.Parse((Text) dictionary[TimestampKey], CultureInfo.InvariantCulture);
        }

        public IValue Serialize()
        {
            return Bencodex.Types.Dictionary.Empty
                .Add(DownloadUrlsKey, _downloadUrls.Serialize())
                .Add(TimestampKey, Timestamp.ToString(CultureInfo.InvariantCulture));;
        }
    }
}
