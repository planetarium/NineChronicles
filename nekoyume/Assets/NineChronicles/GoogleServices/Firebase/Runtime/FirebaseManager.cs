#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#nullable enable

using System;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Analytics;
using UnityEngine;

namespace NineChronicles.GoogleServices.Firebase.Runtime
{
    public static class FirebaseManager
    {
        private static readonly string NameTag = $"[{nameof(FirebaseManager)}]";

        public static FirebaseApp? FirebaseAppInstance { get; private set; }

        public static bool IsAvailable => FirebaseAppInstance is not null;

        public static async UniTask InitializeAsync()
        {
            Debug.Log($"{NameTag}Initialize Firebase.");
            var result = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (result != DependencyStatus.Available)
            {
                var msg = $"{NameTag}Could not resolve all Firebase dependencies: {result}";
                Debug.LogError(msg);
                FirebaseAppInstance = null;
                return;
            }

            FirebaseAppInstance = FirebaseApp.DefaultInstance;
            InitializeAnalytics();
            Debug.Log($"{NameTag}Initialize Firebase done.");
        }

        private static void InitializeAnalytics()
        {
            Debug.Log($"{NameTag}Initialize Firebase Analytics.");
            Debug.Log($"{NameTag}Enabling data collection.");
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

            // NOTE: Uncomment if we can obtain user email address.
            // Debug.Log($"{NameTag}Initiate on-device conversion measurement.");
            // FirebaseAnalytics.InitiateOnDeviceConversionMeasurementWithEmailAddress(
            //     "test@testemail.com");

            // Set default session duration values.
            Debug.Log($"{NameTag}Set session timeout to 30 minutes.");
            FirebaseAnalytics.SetSessionTimeoutDuration(new TimeSpan(0, 30, 0));
            Debug.Log($"{NameTag}Initialize Firebase Analytics done.");
        }
    }
}
#endif
