using System;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace Nekoyume.IAPStore
{
    public class IAPStoreManager : MonoBehaviour, IDetailedStoreListener
    {
        private IStoreController _controller;
        private IExtensionProvider _extensions;

        public bool IsInitialized { get; private set; }

        private async void Awake()
        {
            try
            {
                var initializationOptions = new InitializationOptions()
                    .SetEnvironmentName("dev");
                await UnityServices.InitializeAsync(initializationOptions);
                var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance(AppStore.GooglePlay));
                builder.AddProduct("g_single_ap01", ProductType.Consumable);
                builder.AddProduct("g_single_hourglass01", ProductType.Consumable);
                UnityPurchasing.Initialize(this, builder);
            }
            catch (Exception exception)
            {
                // An error occurred during services initialization.
                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// Called when Unity IAP is ready to make purchases.
        /// </summary>
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _controller = controller;
            _extensions = extensions;
            var additional = new HashSet<ProductDefinition>
            {
                new("g_pkg_daily01", ProductType.Consumable),
                new("g_pkg_weekly01", ProductType.Consumable)
            };

            void OnSuccess()
            {
                Debug.Log("Fetched successfully!");
                // The additional products are added to the set of
                // previously retrieved products and are browseable
                // and purchasable.
                foreach (var product in _controller.products.all)
                {
                    Debug.Log(product.definition.id);
                }

                IsInitialized = true;
            }

            void OnFailure(InitializationFailureReason error, string message)
            {
                Debug.LogWarning($"Fetching failed for the specified reason: {error}\n{message}");
            }

            _controller.FetchAdditionalProducts(additional, OnSuccess, OnFailure);
        }

        /// <summary>
        /// Called when Unity IAP encounters an unrecoverable initialization error.
        ///
        /// Note that this will not be called if Internet is unavailable; Unity IAP
        /// will attempt initialization until it becomes available.
        /// </summary>
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            OnInitializeFailed(error, string.Empty);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"Initializing failed for the specified reason: {error}\n{message}");
        }

        public void OnPurchaseClicked(string productId) {
            _controller.InitiatePurchase(productId);
        }

        /// <summary>
        /// Called when a purchase completes.
        ///
        /// May be called at any time after OnInitialized().
        /// </summary>
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            return PurchaseProcessingResult.Complete;
        }

        /// <summary>
        /// Called when a purchase fails.
        /// IStoreListener.OnPurchaseFailed is deprecated,
        /// use IDetailedStoreListener.OnPurchaseFailed instead.
        /// </summary>
        public void OnPurchaseFailed(Product i, PurchaseFailureReason p)
        {
            Debug.LogError($"PurchaseFail. reason: {p}, Product: {i}");
        }

        /// <summary>
        /// Called when a purchase fails.
        /// </summary>
        public void OnPurchaseFailed(Product i, PurchaseFailureDescription p)
        {
            Debug.LogError($"PurchaseFail. reason: {p}, Product: {i}");
        }
    }
}
