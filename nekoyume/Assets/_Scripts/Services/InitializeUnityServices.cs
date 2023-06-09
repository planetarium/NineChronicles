using System;
using Cysharp.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

namespace Nekoyume.Services
{
    public static class InitializeUnityServices
    {
        public static bool Initialized { get; private set; }

        public static async UniTask InitializeAsync(
            string environmentName = "production",
            System.Action onInitialized = null,
            Action<string> onFailed = null)
        {
            if (Initialized)
            {
                return;
            }

            try
            {
                var options = new InitializationOptions().SetEnvironmentName(environmentName);
                await UnityServices.InitializeAsync(options).ContinueWith(_ =>
                {
                    if (onInitialized == null)
                    {
                        OnInitializedDefault();
                    }
                    else
                    {
                        onInitialized();
                    }
                });
                Initialized = true;
            }
            catch (Exception exception)
            {
                if (onFailed == null)
                {
                    OnFailedDefault(exception.Message);
                }
                else
                {
                    onFailed(exception.Message);
                }
            }
        }

        private static void OnInitializedDefault()
        {
            Initialized = true;
            Debug.Log("Unity Gaming Services has been successfully initialized.");
        }

        private static void OnFailedDefault(string message)
        {
            Debug.LogError($"Unity Gaming Services failed to initialize with error: {message}.");
        }
    }
}
