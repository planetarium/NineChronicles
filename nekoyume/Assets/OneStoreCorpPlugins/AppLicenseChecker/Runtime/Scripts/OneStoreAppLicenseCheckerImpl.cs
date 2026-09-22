#if UNITY_ANDROID || !UNITY_EDITOR

using System;
using OneStore.Common;
using OneStore.Alc.Internal;
using UnityEngine;
using Logger = OneStore.Common.OneStoreLogger;

namespace OneStore.Alc
{
    public class OneStoreAppLicenseCheckerImpl
    {
        private AndroidJavaObject _appLicenseChecker;
        private readonly string _licenseKey;
        private LicenseCheckerListener _listener;
        private ILicenseCheckCallback _callback;

        /// <summary>
        /// Initializes the ONE store App License Checker with the provided license key.
        /// Ensures that the platform is Android before proceeding.
        /// </summary>
        /// <param name="licenseKey">The license key required for validation.</param>
        public OneStoreAppLicenseCheckerImpl(string licenseKey)
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                throw new PlatformNotSupportedException("Operation is not supported on this platform.");
            }

            _licenseKey = licenseKey;
        }

        /// <summary>
        /// Initializes the App License Checker (ALC) and establishes a connection.
        /// </summary>
        /// <param name="callback">Callback interface for handling license check results.</param>
        public void Initialize(ILicenseCheckCallback callback)
        {
            _callback = callback;
            _appLicenseChecker = new AndroidJavaObject(Constants.AppLicenseChecker, _licenseKey);

            var context = JniHelper.GetUnityAndroidActivity();
            _listener = new LicenseCheckerListener();
            _listener.Granted += OnGranted;
            _listener.Denied += OnDenied;
            _listener.Error += OnError;

            // Sets up the license checker with the Unity activity context and the listener.
            _appLicenseChecker.Call(
                Constants.AppLicenseCheckerSetupMethod,
                context,
                _listener
            );
        }

        /// <summary>
        /// Calls the Cached API to query the license.
        /// Uses cached license information when available.
        /// </summary>
        public void QueryLicense()
        {
            Logger.Log("do queryLicense");
            _appLicenseChecker.Call(Constants.AppLicenseCheckerQueryLicenseMethod);
        }

        /// <summary>
        /// Calls the Non-Cached API to query the license.
        /// Does not use cached license information and forces a fresh validation.
        /// </summary>
        public void StrictQueryLicense()
        {
            Logger.Log("do strictQueryLicense");
            _appLicenseChecker.Call(Constants.AppLicenseCheckerStrickQueryLicenseMethod);
        }

        /// <summary>
        /// Disconnects and releases resources related to the App License Checker (ALC).
        /// </summary>
        public void Destroy()
        {
            Logger.Log("do destroy");
            _appLicenseChecker.Call(Constants.AppLicenseCheckerDestroy);
        }

        /// <summary>
        /// Callback triggered when the license is successfully granted.
        /// </summary>
        /// <param name="license">The granted license key.</param>
        /// <param name="signature">The license signature for verification.</param>
        private void OnGranted(string license, string signature)
        {
            _callback.OnGranted(license, signature);
        }

        /// <summary>
        /// Callback triggered when the license validation is denied.
        /// </summary>
        private void OnDenied()
        {
            _callback.OnDenied();
        }

        /// <summary>
        /// Callback triggered when an error occurs during license validation.
        /// </summary>
        /// <param name="code">The error code returned.</param>
        /// <param name="message">A message describing the error.</param>
        private void OnError(int code, string message)
        {
            _callback.OnError(code, message);
        }
    }
}

#endif