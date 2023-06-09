using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using UnityEngine.UI;

namespace Samples.Purchasing.Core.LocalReceiptValidation
{
    public class LocalReceiptValidation : MonoBehaviour, IStoreListener
    {
        IStoreController m_StoreController;

        CrossPlatformValidator m_Validator = null;

        //Your products IDs. They should match the ids of your products in your store.
        public string goldProductId = "com.mycompany.mygame.gold1";
        public ProductType productType = ProductType.Consumable;

        public Text GoldCountText;

        public UserWarning userWarning;

        public Toggle appleCertificateToggle;

        int m_GoldCount;
        bool m_UseAppleStoreKitTestCertificate;

        void Start()
        {
            userWarning.Clear();
            appleCertificateToggle.onValueChanged.AddListener(OnAppleStoreKitTestCertificateChanged);
            m_UseAppleStoreKitTestCertificate = appleCertificateToggle.isOn;
            InitializePurchasing();
            UpdateUI();
        }

        static bool IsCurrentStoreSupportedByValidator()
        {
            //The CrossPlatform validator only supports the GooglePlayStore and Apple's App Stores.
            return IsGooglePlayStoreSelected() || IsAppleAppStoreSelected();
        }

        static bool IsGooglePlayStoreSelected()
        {
            var currentAppStore = StandardPurchasingModule.Instance().appStore;
            return currentAppStore == AppStore.GooglePlay;
        }

        static bool IsAppleAppStoreSelected()
        {
            var currentAppStore = StandardPurchasingModule.Instance().appStore;
            return currentAppStore == AppStore.AppleAppStore ||
                   currentAppStore == AppStore.MacAppStore;
        }

        void InitializePurchasing()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            builder.AddProduct(goldProductId, productType);

            UnityPurchasing.Initialize(this, builder);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("In-App Purchasing successfully initialized");
            m_StoreController = controller;
            InitializeValidator();
        }

        void InitializeValidator()
        {
            if (IsCurrentStoreSupportedByValidator())
            {
#if !UNITY_EDITOR
                var appleTangleData = m_UseAppleStoreKitTestCertificate ? AppleStoreKitTestTangle.Data() : AppleTangle.Data();
                m_Validator = new CrossPlatformValidator(GooglePlayTangle.Data(), appleTangleData, Application.identifier);
#endif
            }
            else
            {
                userWarning.WarnInvalidStore(StandardPurchasingModule.Instance().appStore);
            }
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            OnInitializeFailed(error, null);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            var errorMessage = $"Purchasing failed to initialize. Reason: {error}.";

            if (message != null)
            {
                errorMessage += $" More details: {message}";
            }

            Debug.Log(errorMessage);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.Log($"Purchase failed - Product: '{product.definition.id}', PurchaseFailureReason: {failureReason}");
        }

        public void BuyGold()
        {
            m_StoreController.InitiatePurchase(goldProductId);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            //Retrieve the purchased product
            var product = args.purchasedProduct;

            var isPurchaseValid = IsPurchaseValid(product);

            if (isPurchaseValid)
            {
                //Add the purchased product to the players inventory
                UnlockContent(product);
                Debug.Log("Valid receipt, unlocking content.");
            }
            else
            {
                Debug.Log("Invalid receipt, not unlocking content.");
            }

            //We return Complete, informing Unity IAP that the processing on our side is done and the transaction can be closed.
            return PurchaseProcessingResult.Complete;
        }

        bool IsPurchaseValid(Product product)
        {
            //If we the validator doesn't support the current store, we assume the purchase is valid
            if (IsCurrentStoreSupportedByValidator())
            {
                try
                {
                    var result = m_Validator.Validate(product.receipt);
                    //The validator returns parsed receipts.
                    LogReceipts(result);
                }
                //If the purchase is deemed invalid, the validator throws an IAPSecurityException.
                catch (IAPSecurityException reason)
                {
                    Debug.Log($"Invalid receipt: {reason}");
                    return false;
                }
            }

            return true;
        }

        void UnlockContent(Product product)
        {
            if (product.definition.id == goldProductId)
            {
                AddGold();
            }
        }

        void AddGold()
        {
            m_GoldCount++;
            UpdateUI();
        }

        void UpdateUI()
        {
            GoldCountText.text = $"Your Gold: {m_GoldCount}";
        }

        static void LogReceipts(IEnumerable<IPurchaseReceipt> receipts)
        {
            Debug.Log("Receipt is valid. Contents:");
            foreach (var receipt in receipts)
            {
                LogReceipt(receipt);
            }
        }

        static void LogReceipt(IPurchaseReceipt receipt)
        {
            Debug.Log($"Product ID: {receipt.productID}\n" +
                      $"Purchase Date: {receipt.purchaseDate}\n" +
                      $"Transaction ID: {receipt.transactionID}");

            if (receipt is GooglePlayReceipt googleReceipt)
            {
                Debug.Log($"Purchase State: {googleReceipt.purchaseState}\n" +
                          $"Purchase Token: {googleReceipt.purchaseToken}");
            }

            if (receipt is AppleInAppPurchaseReceipt appleReceipt)
            {
                Debug.Log($"Original Transaction ID: {appleReceipt.originalTransactionIdentifier}\n" +
                          $"Subscription Expiration Date: {appleReceipt.subscriptionExpirationDate}\n" +
                          $"Cancellation Date: {appleReceipt.cancellationDate}\n" +
                          $"Quantity: {appleReceipt.quantity}");
            }
        }

        void OnAppleStoreKitTestCertificateChanged(bool value)
        {
            m_UseAppleStoreKitTestCertificate = value;
            InitializeValidator();
        }
    }
}
