using System;

namespace OneStore.Auth.Internal
{
    internal static class Constants
    {
        public const string Version = "1.2.2";

        public static readonly TimeSpan AsyncTimeout = TimeSpan.FromMilliseconds(30000);

        public const string SignInClient = "com.gaa.sdk.auth.GaaSignInClientImpl";
        public const string SignInClientSetupMethod = "initialize";
        public const string SignInClientSilentSignInMethod = "silentSignIn";
        public const string SignInClientLaunchSignInFlowMethod = "launchSignInFlow";
        // public const string SignInClientLaunchUpdateOrInstallMethod = "launchUpdateOrInstallFlow";

        public const string OnAuthListener = "com.gaa.sdk.auth.OnAuthListener";   
    }
}