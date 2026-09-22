using System;
using UnityEngine;

namespace OneStore.Purchasing.Internal
{
    /// <summary>
    /// It is a default listener for in-app purchase API responses.
    /// Public documentation can be found at
    /// https://onestore-dev.gitbook.io/dev/v/eng/tools/tools/v21/references/en-interfaces/en-iapresultlistener
    /// </summary>
    public class IapResultListener : AndroidJavaProxy
    {
        public event Action<AndroidJavaObject> OnResponse = delegate { };

        public IapResultListener() : base(Constants.IapResultListener) { }

        void onResponse(AndroidJavaObject iapResult)
        {
            OnResponse.Invoke(iapResult);
        }
    }
}
