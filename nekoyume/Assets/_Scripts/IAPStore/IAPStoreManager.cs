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
            var states = States.Instance;

            var result = ParsePayloadJson(e.purchasedProduct.receipt);
            if (!string.IsNullOrEmpty(result))
            {
                var popup = Widget.Find<IconAndButtonSystem>();

                Game.Game.instance.IAPServiceManager
                    .PurchaseRequestAsync(
                        e.purchasedProduct.receipt,
                        states.AgentState.address,
                        states.CurrentAvatarState.address)
                    .ContinueWith(
                        task =>
                        {
                            if (task.Result is null)
                            {
                                popup.Show(
                                    L10nManager.Localize("UI_ERROR"),
                                    "IAP Service Purchasing failed. result is not HTTPSCODE:200\n" +
                                    $"receipt: {e.purchasedProduct.receipt}",
                                    L10nManager.Localize("UI_OK"),
                                    false);
                            }
                            else
                            {
                                popup.Show(
                                    L10nManager.Localize("UI_COMPLETED"),
                                    "IAP Service Purchasing completed.\n" +
                                    $"receipt: {e.purchasedProduct.receipt}",
                                    L10nManager.Localize("UI_OK"),
                                    false,
                                    IconAndButtonSystem.SystemType.Information);
                                _controller.ConfirmPendingPurchase(e.purchasedProduct);
                            }
                        });
                return PurchaseProcessingResult.Pending;
            }

            Debug.Log("Invalid receipt, not unlocking content");
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

        private static string ParsePayloadJson(string unityIAPReceipt)
        {
            try
            {
                var wrapper = (Dictionary<string, object>) MiniJson.JsonDecode(unityIAPReceipt);
                if (wrapper == null)
                {
                    Debug.LogError($"receipt is invalid.: {unityIAPReceipt}");
                    return string.Empty;
                }

                var store = (string)wrapper["Store"];
                var payload = (string)wrapper["Payload"];

                if (store == "GooglePlay")
                {
                    var details = (Dictionary<string, object>) MiniJson.JsonDecode(payload);
                    var json = (string)details["json"];
                    return json;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Cannot validate due to unhandled exception. (" + ex + ")");
                return string.Empty;
            }

            return string.Empty;
        }
    }
}
