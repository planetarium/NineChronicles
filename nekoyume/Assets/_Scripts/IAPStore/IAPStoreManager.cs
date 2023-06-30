using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.State;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using ProductType = UnityEngine.Purchasing.ProductType;

namespace Nekoyume.IAPStore
{
    public class IAPStoreManager : MonoBehaviour, IDetailedStoreListener
    {
        private IStoreController _controller;
        private IExtensionProvider _extensions;

        public bool IsInitialized { get; private set; }
        public IReadOnlyList<ProductSchema> Products { get; private set; }

        private async void Awake()
        {
            try
            {
                var initializationOptions = new InitializationOptions()
                    .SetEnvironmentName("dev");
                await UnityServices.InitializeAsync(initializationOptions);
            }
            catch (Exception exception)
            {
                // An error occurred during services initialization.
                Debug.LogException(exception);
            }

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            Products = await Game.Game.instance.IAPServiceManager.GetProductsAsync(
                    States.Instance.AgentState.address);
            if (Products is null)
            {
                // TODO: not initialized case handling
                Debug.LogError(
                    $"IAPServiceManager.GetProductsAsync({States.Instance.AgentState.address}): Products is null.");
                return;
            }

            foreach (var schema in Products.Where(s => s.Active))
            {
                builder.AddProduct(schema.GoogleSku,
                    schema.FavList.Length > 0
                        ? ProductType.Consumable
                        : ProductType.NonConsumable);
            }

            UnityPurchasing.Initialize(this, builder);
        }

        /// <summary>
        /// Called when Unity IAP is ready to make purchases.
        /// </summary>
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _controller = controller;
            _extensions = extensions;
            Debug.Log("IAP Store Manager Initialized successfully!");
            foreach (var product in _controller.products.all)
            {
                Debug.Log(product.definition.id);
            }

            IsInitialized = true;

            // void OnSuccess()
            // {
            //     Debug.Log("Fetched successfully!");
            //     // The additional products are added to the set of
            //     // previously retrieved products and are browseable
            //     // and purchasable.
            //     foreach (var product in _controller.products.all)
            //     {
            //         Debug.Log(product.definition.id);
            //     }
            //
            //     IsInitialized = true;
            // }
            //
            // void OnFailure(InitializationFailureReason error, string message)
            // {
            //     Debug.LogWarning($"Fetching failed for the specified reason: {error}\n{message}");
            // }
            //
            // var additional = new HashSet<ProductDefinition>
            // {
            //     new("g_pkg_daily01", ProductType.Consumable),
            //     new("g_pkg_weekly01", ProductType.Consumable)
            // };
            // _controller.FetchAdditionalProducts(additional, OnSuccess, OnFailure);
        }

        /// <summary>
        /// Called when Unity IAP encounters an unrecoverable initialization error.
        ///
        /// Note that this will not be called if Internet is unavailable; Unity IAP
        /// will attempt initialization until it becomes available.
        /// </summary>
        void IStoreListener.OnInitializeFailed(InitializationFailureReason error)
        {
            ((IStoreListener)this).OnInitializeFailed(error, string.Empty);
        }

        void IStoreListener.OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"Initializing failed for the specified reason: {error}\n{message}");
        }

        public void OnPurchaseClicked(string productId)
        {
            _controller.InitiatePurchase(productId);
        }

        /// <summary>
        /// Called when a purchase completes.
        /// May be called at any time after OnInitialized().
        /// </summary>
        PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs e)
        {
            var states = States.Instance;
            var inventoryAddress =
                Addresses.GetInventoryAddress(states.AgentState.address, states.CurrentAvatarKey);
            Game.Game.instance.IAPServiceManager
                .PurchaseAsync(e.purchasedProduct.receipt, inventoryAddress)
                .ContinueWith(
                    task =>
                    {
                        if (task.Result is null)
                        {
                            Debug.LogError(
                                "IAP Service Purchasing failed. result is not HTTPSCODE:200" +
                                $"\nreceipt: {e.purchasedProduct.receipt}");
                        }
                        else
                        {
                            _controller.ConfirmPendingPurchase(e.purchasedProduct);
                        }
                    });
            return PurchaseProcessingResult.Pending;
        }

        /// <summary>
        /// Called when a purchase fails.
        /// IStoreListener.OnPurchaseFailed is deprecated,
        /// use IDetailedStoreListener.OnPurchaseFailed instead.
        /// </summary>
        void IStoreListener.OnPurchaseFailed(Product i, PurchaseFailureReason p)
        {
            Debug.LogError($"PurchaseFail. reason: {p}, Product: {i}");
            if (p == PurchaseFailureReason.PurchasingUnavailable)
            {
                // IAP may be disabled in device settings.
            }
        }

        /// <summary>
        /// Called when a purchase fails.
        /// </summary>
        void IDetailedStoreListener.OnPurchaseFailed(Product i, PurchaseFailureDescription p)
        {
            Debug.LogError($"PurchaseFail. reason: {p}, Product: {i}");
            if (p.reason == PurchaseFailureReason.PurchasingUnavailable)
            {
                // IAP may be disabled in device settings.
            }
        }
    }
}
