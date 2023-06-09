using Cysharp.Threading.Tasks;
using Nekoyume.Pattern;
using Nekoyume.Services;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace Nekoyume
{
    public class IAPManager : MonoSingleton<IAPManager>, IDetailedStoreListener
    {
        private bool _initialized;
        private InitializationFailureReason? _initializationFailureReason;

        private IStoreController _controller;
        // private IExtensionProvider _extensions;

        public async
            UniTask<(bool initialized, InitializationFailureReason? initializationFailureReason)>
            InitializeAsync()
        {
            if (!InitializeUnityServices.Initialized)
            {
                await InitializeUnityServices.InitializeAsync();
                if (!InitializeUnityServices.Initialized)
                {
                    // return (
                }
            }

            Initialize();
            await UniTask.WaitUntil(() => _initialized || _initializationFailureReason.HasValue);
            return (_initialized, _initializationFailureReason);
        }

        private void Initialize()
        {
            _initialized = false;
            _initializationFailureReason = null;
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            builder.AddProduct(
                "consumable_test_product",
                ProductType.Consumable,
                new IDs
                {
                    { "consumable_test_product_id_for_android", GooglePlay.Name },
                    { "consumable_test_product_id_for_ios", AppleAppStore.Name },
                });
            builder.AddProduct(
                "non_consumable_test_product",
                ProductType.NonConsumable,
                new IDs
                {
                    { "non_consumable_test_product_id_for_android", GooglePlay.Name },
                    { "non_consumable_test_product_id_for_ios", AppleAppStore.Name },
                });
            builder.AddProduct(
                "subscription_test_product",
                ProductType.Subscription,
                new IDs
                {
                    { "subscription_test_product_id_for_android", GooglePlay.Name },
                    { "subscription_test_product_id_for_ios", AppleAppStore.Name },
                });
            UnityPurchasing.Initialize(this, builder);
        }

        void IStoreListener.OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.Log($"[{nameof(IAPManager)}] OnInitializeFailed: {error}");
        }

        void IStoreListener.OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.Log($"[{nameof(IAPManager)}] OnInitializeFailed: {error}, {message}");
        }

        void IStoreListener.OnInitialized(
            IStoreController controller,
            IExtensionProvider extensions)
        {
            Debug.Log($"[{nameof(IAPManager)}] OnInitialize");

            _initialized = true;
            _controller = controller;
            // _extensions = extensions;

            // var validator = new CrossPlatformValidator(GooglePlayTangle.Data(),
            //     AppleTangle.Data(), Application.identifier);
	           //
            // var products = _controller.products.all;
            // foreach(var product in products) {
            //     if (product.hasReceipt) {
            //         var result = validator.Validate(product.receipt);
	           //
            //         foreach (IPurchaseReceipt productReceipt in result) {
            //             //앱스토어의 경우 GooglePlayReceipt를 AppleInAppPurchaseReceipt로 바꾸면 됩니다.
            //             GooglePlayReceipt googlePlayReceipt = productReceipt as GooglePlayReceipt;
            //             if (null != googlePlayReceipt) {
            //                 Debug.Log($"Product ID : {googlePlayReceipt.productID}");
            //                 Debug.Log($"Purchase date : {googlePlayReceipt.purchaseDate.ToLocalTime()}");
            //                 Debug.Log($"Transaction ID : {googlePlayReceipt.transactionID}");
            //                 Debug.Log($"Purchase token : {googlePlayReceipt.purchaseToken}");
            //             }
            //         }
            //     }
            // }
        }

        public void Purchase(string productId)
        {
            if (!_initialized)
            {
                Debug.Log($"[{nameof(IAPManager)}] Not initialized");
                return;
            }

            var product = _controller.products.WithID(productId);
            if (product is null)
            {
                Debug.Log($"[{nameof(IAPManager)}] Not found product: {productId}");
                return;
            }

            if (!product.availableToPurchase)
            {
                Debug.Log($"[{nameof(IAPManager)}] Not available to purchase: {productId}");
                return;
            }

            _controller.InitiatePurchase(product);
            Debug.Log($"[{nameof(IAPManager)}] InitiatePurchase: {productId}");
        }

        PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            Debug.Log($"[{nameof(IAPManager)}] ProcessPurchase:" +
                      $" {purchaseEvent.purchasedProduct.definition.id}");

            // Handle product id.
            //

            var purchasedProduct = purchaseEvent.purchasedProduct;
            if (!purchasedProduct.hasReceipt)
            {
                Debug.LogError($"[{nameof(IAPManager)}] No receipt");
                return PurchaseProcessingResult.Pending; // or Complete
            }

            // Handle receipt.
            var receipt = purchasedProduct.receipt;
            Debug.Log($"[{nameof(IAPManager)}] Receipt: {receipt}");
            return PurchaseProcessingResult.Complete;
        }

        void IStoreListener.OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.Log(
                $"[{nameof(IAPManager)}] OnPurchaseFailed: {product.definition.id}, {failureReason}");

            // Handle failure with failureReason argument.
        }

        void IDetailedStoreListener.OnPurchaseFailed(
            Product product,
            PurchaseFailureDescription failureDescription)
        {
            Debug.Log(
                $"[{nameof(IAPManager)}] OnPurchaseFailed: {product.definition.id}, {failureDescription}");

            // Handle failure with failureDescription argument.
        }
    }
}
