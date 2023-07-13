using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI;
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

        public IEnumerable<Product> IAPProducts => _controller.products.all;
        public bool IsInitialized { get; private set; }

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
            var products = await Game.Game.instance.IAPServiceManager.GetProductsAsync(
                States.Instance.AgentState.address);
            if (products is null)
            {
                // TODO: not initialized case handling
                Debug.LogError(
                    $"IAPServiceManager.GetProductsAsync({States.Instance.AgentState.address}): Products is null.");
                return;
            }

            foreach (var schema in products.Where(s => s.Active))
            {
                builder.AddProduct(schema.GoogleSku, ProductType.Consumable);
            }

            UnityPurchasing.Initialize(this, builder);
        }

        public void OnPurchaseClicked(string productId)
        {
            _controller.InitiatePurchase(productId);
        }

        /// <summary>
        /// Called when Unity IAP is ready to make purchases.
        /// </summary>
        void IStoreListener.OnInitialized(IStoreController controller,
            IExtensionProvider extensions)
        {
            _controller = controller;
            _extensions = extensions;
            Debug.Log("IAP Store Manager Initialized successfully!");
            foreach (var product in _controller.products.all)
            {
                Debug.Log(
                    $"{product.definition.id}: {product.metadata.localizedTitle}, {product.metadata.localizedDescription}, {product.metadata.localizedPriceString}");
            }

            IsInitialized = true;
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

        /// <summary>
        /// Called when a purchase completes.
        /// May be called at any time after OnInitialized().
        /// </summary>
        PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs e)
        {
            _controller.ConfirmPendingPurchase(e.purchasedProduct);
            if (e.purchasedProduct.availableToPurchase)
            {
                OnPurchaseRequestAsync(e);
                return PurchaseProcessingResult.Pending;
            }

            Debug.LogWarning($"not availableToPurchase. e.purchasedProduct.availableToPurchase: {e.purchasedProduct.availableToPurchase}");
            return PurchaseProcessingResult.Complete;
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

        private async void OnPurchaseRequestAsync(PurchaseEventArgs e)
        {
            var popup = Widget.Find<IconAndButtonSystem>();
            var states = States.Instance;
            Debug.unityLogger.Log($"IAP Receipt: {e.purchasedProduct.receipt}");

            try
            {
                var result = await Game.Game.instance.IAPServiceManager
                    .PurchaseRequestAsync(
                        e.purchasedProduct.receipt,
                        states.AgentState.address,
                        states.CurrentAvatarState.address);
                if (result is null)
                {
                    popup.Show(
                        L10nManager.Localize("UI_ERROR"),
                        "IAP Service Processing failed.",
                        L10nManager.Localize("UI_OK"),
                        false);
                }
                else
                {
                    popup.Show(
                        L10nManager.Localize("UI_COMPLETED"),
                        "IAP Service Processing completed.",
                        L10nManager.Localize("UI_OK"),
                        false,
                        IconAndButtonSystem.SystemType.Information);
                    _controller.ConfirmPendingPurchase(e.purchasedProduct);
                }
            }
            catch (Exception exc)
            {
                Widget.Find<IconAndButtonSystem>().Show("UI_ERROR", exc.Message, localize: false);
            }
        }
    }
}
