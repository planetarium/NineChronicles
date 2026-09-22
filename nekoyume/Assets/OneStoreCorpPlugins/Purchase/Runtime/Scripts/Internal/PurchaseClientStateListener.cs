using System;
using UnityEngine;

namespace OneStore.Purchasing.Internal
{
    /// <summary>
    /// Callback for setup process of Gaa Purchasing Library.
    /// Public documentation can be found at
    /// https://onestore-dev.gitbook.io/dev/v/eng/tools/tools/v21/references/en-interfaces/en-purchaseclientstatelistener
    /// </summary>
    public class PurchaseClientStateListener : AndroidJavaProxy
    {
        public event Action OnServiceDisconnected = delegate { };
        public event Action<AndroidJavaObject> OnSetupFinished = delegate { };

        public PurchaseClientStateListener() : base(Constants.PurchaseClientStateListener) { }

        void onServiceDisconnected()
        {
            OnServiceDisconnected.Invoke();
        }

        void onSetupFinished(AndroidJavaObject iapResult)
        {
            OnSetupFinished.Invoke(iapResult);
        }
    }
}
