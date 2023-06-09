using System;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.UI;

namespace Samples.Purchasing.Core.InitializeGamingServices
{
    public class InitializeGamingServices : MonoBehaviour
    {
        public Text informationText;

        const string k_Environment = "production";

        void Awake()
        {
            // Uncomment this line to initialize Unity Gaming Services.
            Initialize(OnSuccess, OnError);
        }

        void Initialize(Action onSuccess, Action<string> onError)
        {
            try
            {
                var options = new InitializationOptions().SetEnvironmentName(k_Environment);

                UnityServices.InitializeAsync(options).ContinueWith(task => onSuccess());
            }
            catch (Exception exception)
            {
                onError(exception.Message);
            }
        }

        void OnSuccess()
        {
            var text = "Congratulations!\nUnity Gaming Services has been successfully initialized.";
            informationText.text = text;
            Debug.Log(text);
        }

        void OnError(string message)
        {
            var text = $"Unity Gaming Services failed to initialize with error: {message}.";
            informationText.text = text;
            Debug.LogError(text);
        }

        void Start()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                var text =
                    "Error: Unity Gaming Services not initialized.\n" +
                    "To initialize Unity Gaming Services, open the file \"InitializeGamingServices.cs\" " +
                    "and uncomment the line \"Initialize(OnSuccess, OnError);\" in the \"Awake\" method.";
                informationText.text = text;
                Debug.LogError(text);
            }
        }
    }
}
