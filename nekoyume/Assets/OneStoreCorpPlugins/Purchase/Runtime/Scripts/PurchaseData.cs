using System;
using UnityEngine;
using OneStore.Purchasing.Internal;
using Logger = OneStore.Common.OneStoreLogger;

namespace OneStore.Purchasing
{
    /// <summary>
    /// Represents a ONE Store in-app purchase data.
    /// Public documentation can be found at
    /// https://onestore-dev.gitbook.io/dev/v/eng/tools/tools/v21/references/en-classes/en-purchasedata
    /// </summary>
    [Serializable]
    public class PurchaseData
    {
        private readonly PurchaseMeta _purchaseMeta;
        private readonly PurchaseReceipt _purchaseReceipt;

        private PurchaseData(PurchaseMeta purchaseMeta, string jsonPurchaseData, string signature)
        {
            _purchaseMeta = purchaseMeta;
            _purchaseReceipt = new PurchaseReceipt(jsonPurchaseData, signature);
        }

        public string OrderId { get { return _purchaseMeta.orderId; } }

		public string PackageName { get { return _purchaseMeta.packageName; } }

        public string ProductId { get { return _purchaseMeta.productId; } }

        public long PurchaseTime { get { return _purchaseMeta.purchaseTime; } }

        [Obsolete]
        public string PurchaseId { get { return _purchaseMeta.purchaseId; } }

        public string PurchaseToken { get { return _purchaseMeta.purchaseToken; } }

        public string DeveloperPayload { get { return _purchaseMeta.developerPayload; } }

        public int PurchaseState { get { return _purchaseMeta.purchaseState; } }

		public int RecurringState { get { return _purchaseMeta.recurringState; } }
        
		public int Quantity { get { return _purchaseMeta.quantity < 1 ? 1 : _purchaseMeta.quantity; } }

        public bool Acknowledged { get { return _purchaseMeta.acknowledgeState == (int) AcknowledgeState.ACKNOWLEDGED; } }

        public string JsonReceipt { get { return JsonUtility.ToJson(_purchaseReceipt); } }


        /// <summary>
        /// Creates a purchase data object from the JSON representation.
        /// </summary>
        /// <returns>
        /// true if a <cref="PurchaseData"/> object gets created successfully; otherwise, it returns false and sets
        /// input purchase data to null.
        /// </returns>
        public static bool FromJson(string jsonPurchaseData, string signature, out PurchaseData purchaseData)
        {
            try
            {
                var purchaseMeta = JsonUtility.FromJson<PurchaseMeta>(jsonPurchaseData);
                purchaseData = new PurchaseData(purchaseMeta, jsonPurchaseData, signature);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("[PurchaseData]: Failed to parse purchase data: {0}", jsonPurchaseData);
                Logger.Exception(ex);
            
                purchaseData = null;
                return false;
            }
        }

        public AndroidJavaObject ToJava()
        {
            return new AndroidJavaObject(
                Constants.PurchaseDataClass,
                _purchaseReceipt.json,
                _purchaseReceipt.signature,
                null
            );
        }

        [Serializable]
        private class PurchaseMeta
        {
#pragma warning disable 649
            public string productId;
            public string packageName;
            public string orderId;
            public string purchaseId;
            public string purchaseToken;
            public string developerPayload;
            public int purchaseState;
            public long purchaseTime;
            public int acknowledgeState;
            public int recurringState;
            public int quantity;
#pragma warning restore 649
        }

        [Serializable]
        private class PurchaseReceipt
        {
            public string json;
            public string signature;

            public PurchaseReceipt(string jsonPurchaseData, string signature)
            {
                json = jsonPurchaseData;
                this.signature = signature;
            }
        }
    }
}
