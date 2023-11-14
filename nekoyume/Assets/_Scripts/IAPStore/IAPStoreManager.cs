#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#define RUN_ON_MOBILE
#define ENABLE_FIREBASE
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI;
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

        public IEnumerable<Product> IAPProducts => _controller.products.all;
        public bool IsInitialized { get; private set; }

        private Dictionary<string, ProductSchema> _initailizedProductSchema = new Dictionary<string, ProductSchema>();

        public Dictionary<string, ProductSchema> SeasonPassProduct = new Dictionary<string, ProductSchema>();

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

            var categorys = await Game.Game.instance.IAPServiceManager.GetProductsAsync(
                States.Instance.AgentState.address);
            if (categorys is null)
            {
                // TODO: not initialized case handling
                Debug.LogError(
                    $"IAPServiceManager.GetProductsAsync({States.Instance.AgentState.address}): Product Catagorys is null.");
                return;
            }

            foreach (var category in categorys)
            {
                foreach (var product in category.ProductList)
                {
                    _initailizedProductSchema.TryAdd(product.Sku, product);
                }
                if(category.Name == "NoShow")
                {
                    foreach (var product in category.ProductList)
                    {
                        SeasonPassProduct.Add(product.Name, product);
                    }
                }
            }

#if UNITY_EDITOR || RUN_ON_MOBILE
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var schema in _initailizedProductSchema.Where(s => s.Value.Active))
            {
                builder.AddProduct(schema.Value.Sku, ProductType.Consumable);
            }

            UnityPurchasing.Initialize(this, builder);
#endif
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

            Widget.Find<ShopListPopup>().PurchaseButtonLoadingEnd();
            Widget.Find<SeasonPassPremiumPopup>().PurchaseButtonLoadingEnd();

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
            Analyzer.Instance.Track(
                "Unity/Shop/IAP/PurchaseResult",
                ("product-id", p.productId),
                ("result", p.reason.ToString()));
            if (p.reason == PurchaseFailureReason.PurchasingUnavailable)
            {
                // IAP may be disabled in device settings.
            }
        }

        private async void OnPurchaseRequestAsync(PurchaseEventArgs e)
        {
            var popup = Widget.Find<IconAndButtonSystem>();
            var states = States.Instance;

            try
            {
                var result = await Game.Game.instance.IAPServiceManager
                    .PurchaseRequestAsync(
                        e.purchasedProduct.receipt,
                        states.AgentState.address,
                        states.CurrentAvatarState.address);

                Widget.Find<ShopListPopup>().PurchaseButtonLoadingEnd();
                Widget.Find<SeasonPassPremiumPopup>().PurchaseButtonLoadingEnd();

                if (result is null)
                {
                    popup.Show(
                        "UI_ERROR",
                        "UI_IAP_PURCHASE_FAILED",
                        "UI_OK",
                        true);
                }
                else
                {
                    Analyzer.Instance.Track(
                        "Unity/Shop/IAP/PurchaseResult",
                        ("product-id", e.purchasedProduct.definition.id),
                        ("result", "Complete"),
                        ("transaction-id", e.purchasedProduct.transactionID));
                    popup.Show(
                        "UI_COMPLETED",
                        "UI_IAP_PURCHASE_COMPLETE",
                        "UI_OK",
                        true,
                        IconAndButtonSystem.SystemType.Information);
                    popup.ConfirmCallback = () =>
                    {
                        if (LoginSystem.GetPassPhrase(states.AgentState.address.ToString()).Equals(string.Empty))
                        {
                            Widget.Find<LoginSystem>().ShowResetPassword();
                        }
                    };
                    Widget.Find<MobileShop>().PurchaseComplete(e.purchasedProduct.definition.id);
                    Widget.Find<MobileShop>().RefreshGrid();
                    Widget.Find<ShopListPopup>().Close();
                    _controller.ConfirmPendingPurchase(e.purchasedProduct);
                }
            }
            catch (Exception exc)
            {
                Widget.Find<SeasonPassPremiumPopup>().PurchaseButtonLoadingEnd();
                Widget.Find<ShopListPopup>().PurchaseButtonLoadingEnd();
                Widget.Find<IconAndButtonSystem>().Show("UI_ERROR", exc.Message, localize: false);
            }
        }
    }
}
