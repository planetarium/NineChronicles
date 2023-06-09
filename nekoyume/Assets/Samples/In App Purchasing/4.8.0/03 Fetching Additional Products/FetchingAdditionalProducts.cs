using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Samples.Purchasing.Core.FetchingAdditionalProducts
{
    public class FetchingAdditionalProducts : MonoBehaviour, IStoreListener
    {
        IStoreController m_StoreController;

        public string goldProductId = "com.mycompany.mygame.gold1";
        public ProductType goldType = ProductType.Consumable;

        //This product will only be fetched once the FetchAdditionalProducts button is clicked
        public string diamondProductId = "com.mycompany.mygame.diamond1";
        public ProductType diamondType = ProductType.Consumable;

        public Text GoldCountText;
        public Text DiamondCountText;

        public GameObject additionalProductsPanel;
        public Button fetchAdditionalProductsButton;

        int m_GoldCount;
        int m_DiamondCount;

        void Start()
        {
            additionalProductsPanel.SetActive(false);

            InitializePurchasing();

            UpdateUI();
        }

        void InitializePurchasing()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            //Add product that will be purchasable and indicate its type.
            //This product will be fetched on initialize.
            builder.AddProduct(goldProductId, goldType);

            UnityPurchasing.Initialize(this, builder);
        }

        public void FetchAdditionalProducts()
        {
            var additionalProductsToFetch = new HashSet<ProductDefinition>
            {
                new ProductDefinition(diamondProductId, diamondType)
            };

            Debug.Log($"Fetching additional products in progress");

            m_StoreController.FetchAdditionalProducts(additionalProductsToFetch,
                () =>
                {
                    //Additional products fetched, they can now be purchased.
                    Debug.Log($"Successfully fetched additional products");

                    //We active the UI associated with the fetched product.
                    additionalProductsPanel.SetActive(true);

                    fetchAdditionalProductsButton.interactable = false;
                },
                (reason, message) =>
                {
                    var errorMessage = $"Fetching additional products failed: {reason.ToString()}.";

                    if (message != null)
                    {
                        errorMessage += $" More details: {message}";
                    }

                    Debug.LogError(errorMessage);
                });
        }

        public void BuyGold()
        {
            m_StoreController.InitiatePurchase(goldProductId);
        }

        public void BuyDiamond()
        {
            m_StoreController.InitiatePurchase(diamondProductId);
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

            //Add the purchased product to the players inventory
            if (product.definition.id == goldProductId)
            {
                AddGold();
            }
            else if (product.definition.id == diamondProductId)
            {
                AddDiamond();
            }

            Debug.Log($"Purchase Complete - Product: {product.definition.id}");

            //We return Complete, informing IAP that the processing on our side is done and the transaction can be closed.
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.Log($"Purchase failed - Product: '{product.definition.id}', PurchaseFailureReason: {failureReason}");
        }

        void AddGold()
        {
            m_GoldCount++;
            UpdateUI();
        }

        void AddDiamond()
        {
            m_DiamondCount++;
            UpdateUI();
        }

        void UpdateUI()
        {
            GoldCountText.text = $"Your Gold: {m_GoldCount}";
            DiamondCountText.text = $"Your Diamonds: {m_DiamondCount}";
        }
    }
}
