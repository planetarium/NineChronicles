using System;
using System.Runtime.InteropServices;

namespace Updater.Common.RuntimePlatform
{
    public static class RuntimePlatform
    {
        public static IRuntimePlatform CurrentPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? new OSXPlatform() as IRuntimePlatform
            : RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new WindowsPlatform() as IRuntimePlatform
                : throw new PlatformNotSupportedException();

    }
}
