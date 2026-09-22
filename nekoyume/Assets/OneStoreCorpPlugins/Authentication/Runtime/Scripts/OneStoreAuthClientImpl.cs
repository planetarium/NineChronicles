
#if UNITY_ANDROID || !UNITY_EDITOR

using System;
using OneStore.Common;
using OneStore.Auth.Internal;
using UnityEngine;
using Logger = OneStore.Common.OneStoreLogger;

namespace OneStore.Auth
{
    public class OneStoreAuthClientImpl
    {
        private AndroidJavaObject _signInClient;

        /// <summary>
        /// Initializes the ONE store authentication client.
        /// Ensures that the platform is Android before proceeding.
        /// </summary>
        public OneStoreAuthClientImpl()
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                throw new PlatformNotSupportedException("Operation is not supported on this platform.");
            }
        }

        /// <summary>
        /// Attempts to sign in silently using a stored login token.
        /// This method can only be called in the background.
        /// </summary>
        /// <param name="callback">Callback function to handle the sign-in result.</param>
        public void SilentSignIn(Action<SignInResult> callback)
        {
            SignInInternal(true, callback);
        }

        /// <summary>
        /// Attempts to sign in using the stored login token first.
        /// If silent login fails, a login screen is displayed to prompt the user to log in.
        /// This method must be called in the foreground.
        /// </summary>
        /// <param name="callback">Callback function to handle the sign-in result.</param>
        public void LaunchSignInFlow(Action<SignInResult> callback)
        {
            SignInInternal(false, callback);
        }

        /// <summary>
        /// Handles the sign-in process.
        /// If `isSilent` is true, a silent login is attempted.
        /// If `isSilent` is false or the silent login fails, a login screen is displayed.
        /// </summary>
        /// <param name="isSilent">Determines whether the sign-in should be silent.</param>
        /// <param name="callback">Callback function to return the sign-in result.</param>
        private void SignInInternal(bool isSilent, Action<SignInResult> callback)
        {
            var context = JniHelper.GetApplicationContext();
            _signInClient = new AndroidJavaObject(Constants.SignInClient, Constants.Version);
            _signInClient.Call(Constants.SignInClientSetupMethod, context);

            var authListener = new OnAuthListener();
            authListener.OnAuthResponse += (javaSignInResult) => {
                var signInResult = AuthHelper.ParseJavaSignInpResult(javaSignInResult);
                var responseCode = AuthHelper.GetResponseCodeFromSignInResult(signInResult);
                if (responseCode != ResponseCode.RESULT_OK)
                {
                    Logger.Error("Failed to signIn with error code {0} and message: {1}", signInResult.Code, signInResult.Message);
                }

                RunOnMainThread(() => callback?.Invoke(signInResult));
            };

            if (isSilent)
            {
                _signInClient.Call(
                    Constants.SignInClientSilentSignInMethod,
                    authListener
                );
            }
            else
            {
                _signInClient.Call(
                    Constants.SignInClientLaunchSignInFlowMethod,
                    JniHelper.GetUnityAndroidActivity(),
                    authListener
                );
            }
        }

        // public void LaunchUpdateOrInstallFlow()
        // {
        //     var resultListener = new ResultListener();
        //     resultListener.OnResponse += (code, message) =>
        //     {
        //         if (code == 0)
        //         {
        //             LaunchSignInFlow();
        //         }
        //         else
        //         {
        //             var signInResult = new SignInResult(code, message);
        //             if (!HandleErrorCode(signInResult))
        //             {
        //                 RunOnMainThread(() => _callback.OnFailed(signInResult));
        //             }
        //         }
        //     };

        //     _signInClient.Call(
        //         Constants.SignInClientLaunchUpdateOrInstallMethod,
        //         JniHelper.GetUnityAndroidActivity(),
        //         resultListener
        //     );
        // }

        /// <summary>
        /// Runs the provided action on the main thread.
        /// </summary>
        /// <param name="action">The action to execute on the main thread.</param>
        private void RunOnMainThread(Action action)
        {
            OneStoreDispatcher.RunOnMainThread(() => action());
        }
    }
}

#endif
