using System;
using UnityEngine;

namespace OneStore.Purchasing.Internal
{
    /// <summary>
    /// Callback when a fetch the store code.
    /// Public documentation can be found at
    /// https://onestore-dev.gitbook.io/dev/v/eng/tools/tools/v21/references/en-interfaces/en-storeinfolistener
    /// </summary>
    public class StoreInfoListener : AndroidJavaProxy
    {
        public event Action<string> OnStoreInfoResponse = delegate { };

        public StoreInfoListener() : base(Constants.StoreInfoListener) { }

        void onStoreInfoResponse(AndroidJavaObject iapResult, string storeCode)
        {
            OnStoreInfoResponse.Invoke(storeCode);
        }
    }
}
