using System;
using System.Globalization;
using Bencodex.Types;

namespace Nekoyume
{
    public readonly struct AppProtocolVersionExtra
    {
        private const string MacOSBinaryUrlKey = "macOSBinaryUrl";

        private const string WindowsBinaryUrlKey = "WindowsBinaryUrl";

        private const string TimestampKey = "timestamp";

        public readonly string MacOSBinaryUrl;

        public readonly string WindowsBinaryUrl;

        public readonly DateTimeOffset Timestamp;

        public AppProtocolVersionExtra(string macOSBinaryUrl, string windowsBinaryUrl, DateTimeOffset timestamp)
        {
            MacOSBinaryUrl = macOSBinaryUrl;
            WindowsBinaryUrl = windowsBinaryUrl;
            Timestamp = timestamp;
        }

        public AppProtocolVersionExtra(Bencodex.Types.Dictionary dictionary)
        {
            MacOSBinaryUrl = (Text) dictionary[MacOSBinaryUrlKey];
            WindowsBinaryUrl = (Text) dictionary[WindowsBinaryUrlKey];
            Timestamp = DateTimeOffset.Parse((Text) dictionary[TimestampKey], CultureInfo.InvariantCulture);
        }

        public IValue Serialize()
        {
            return Bencodex.Types.Dictionary.Empty
                .Add(MacOSBinaryUrlKey, MacOSBinaryUrl)
                .Add(WindowsBinaryUrlKey, WindowsBinaryUrl)
                .Add(TimestampKey, Timestamp.ToString("O", CultureInfo.InvariantCulture));;
        }
    }
}
