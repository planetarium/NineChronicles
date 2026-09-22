using System;

namespace OneStore.Alc.Internal
{
    internal static class Constants
    {   
        public const string Version = "2.2.1";
        public static readonly TimeSpan AsyncTimeout = TimeSpan.FromMilliseconds(30000);

        public const string AppLicenseChecker = "com.onestore.extern.licensing.AppLicenseCheckerImpl";
        public const string AppLicenseCheckerSetupMethod = "initialize";

        public const string AppLicenseCheckerQueryLicenseMethod = "queryLicense";

        public const string AppLicenseCheckerStrickQueryLicenseMethod = "strictQueryLicense";

        public const string AppLicenseCheckerDestroy = "destroy";

        public const string LicenseCheckerListener = "com.onestore.extern.licensing.LicenseCheckerListener";
    }
}