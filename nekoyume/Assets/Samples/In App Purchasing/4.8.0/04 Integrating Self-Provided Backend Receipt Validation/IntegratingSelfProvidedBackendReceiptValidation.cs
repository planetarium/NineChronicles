using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Samples.Purchasing.Core.IntegratingSelfProvidedBackendReceiptValidation
{
    public class IntegratingSelfProvidedBackendReceiptValidation : MonoBehaviour, IStoreListener
    {
        IStoreController m_StoreController;

        public string goldProductId = "com.mycompany.mygame.gold1";
        public ProductType goldType = ProductType.Consumable;

        public Text GoldCountText;
        public Text ProcessingPurchasesCountText;

        int m_GoldCount;
        int m_ProcessingPurchasesCount;

        void Start()
        {
            InitializePurchasing();
            UpdateUI();
        }

        void InitializePurchasing()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            builder.AddProduct(goldProductId, goldType);

            UnityPurchasing.Initialize(this, builder);
        }

        public void BuyGold()
        {
            m_StoreController.InitiatePurchase(goldProductId);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("In-App Purchasing successfully initialized");
            m_StoreController = controller;
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

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            //Retrieve the purchased product
            var product = args.purchasedProduct;

            StartCoroutine(BackEndValidation(product));

            //We return Pending, informing IAP to keep the transaction open while we validate the purchase on our side.
            return PurchaseProcessingResult.Pending;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.Log($"Purchase failed - Product: '{product.definition.id}', PurchaseFailureReason: {failureReason}");
        }

        IEnumerator BackEndValidation(Product product)
        {
            m_ProcessingPurchasesCount++;
            UpdateUI();

            //Mock backend validation. Here you would call your own backend and wait for its response.
            //If the app is closed during this time, ProcessPurchase will be called again for the same purchase once the app is opened again.
            yield return MockServerSideValidation(product);

            m_ProcessingPurchasesCount--;
            UpdateUI();

            Debug.Log($"Confirming purchase of {product.definition.id}");

            //Once we have done the validation in our backend, we confirm the purchase.
            m_StoreController.ConfirmPendingPurchase(product);

            //We can now add the purchased product to the players inventory
            if (product.definition.id == goldProductId)
            {
                AddGold();
            }
        }

        YieldInstruction MockServerSideValidation(Product product)
        {
            const int waitSeconds = 3;
            Debug.Log($"Purchase Pending, Waiting for confirmation for {waitSeconds} seconds - Product: {product.definition.id}");
            return new WaitForSeconds(waitSeconds);
        }

        void AddGold()
        {
            m_GoldCount++;
            UpdateUI();
        }

        void UpdateUI()
        {
            GoldCountText.text = $"Your Gold: {m_GoldCount}";

            ProcessingPurchasesCountText.text = "";
            for (var i = 0; i < m_ProcessingPurchasesCount; i++)
            {
                ProcessingPurchasesCountText.text += "Purchase Processing...\n";
            }
        }
    }
}
