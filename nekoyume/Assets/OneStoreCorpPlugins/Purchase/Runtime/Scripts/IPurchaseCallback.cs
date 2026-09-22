#if UNITY_ANDROID || !UNITY_EDITOR

using System.Collections.Generic;
using OneStore.Purchasing.Internal;

namespace OneStore.Purchasing
{
    public interface IPurchaseCallback
    {
        /// <summary>
        /// Called when any error occurs when using the API.
        /// </summary>
        /// <param name="iapResult"></param>
        void OnSetupFailed(IapResult iapResult);

        /// <summary>
        /// Called when QueryProductDetails() successfully retrieves product details.
        /// </summary>
        /// <param name="productDetails"></param>
        void OnProductDetailsSucceeded(List<ProductDetail> productDetails);
        
        /// <summary>
        /// Called when QueryProductDetails() fails to retrieve product details.
        /// </summary>
        /// <param name="iapResult"></param>
        void OnProductDetailsFailed(IapResult iapResult);
        
        /// <summary>
        /// Called when a purchase is successful or
        /// there are purchases that have not been consumed through the QueryPurchases() method.
        /// </summary>
        /// <param name="purchases"></param>
        void OnPurchaseSucceeded(List<PurchaseData> purchases);

        /// <summary>
        /// Called when a purchase has failed.
        /// </summary>
        /// <param name="iapResult"></param>
        void OnPurchaseFailed(IapResult iapResult);

        /// <summary>
        /// Called when consumption is successful.
        /// </summary>
        /// <param name="productId"></param>
        void OnConsumeSucceeded(PurchaseData purchase);
        
        /// <summary>
        /// Called when consumption fails.
        /// </summary>
        /// <param name="iapResult"></param>
        void OnConsumeFailed(IapResult iapResult);

        /// <summary>
        /// Called when the acknowledgment successful.
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="type"></param>
        void OnAcknowledgeSucceeded(PurchaseData purchase, ProductType type);

        /// <summary>
        /// Called when the acknowledgment fails.
        /// </summary>
        /// <param name="iapResult"></param>
        void OnAcknowledgeFailed(IapResult iapResult);

        /// <summary>
        /// Called when the status of the recurring product is changed.
        /// </summary>
        /// <param name="iapResult"></param>
        /// <param name="productId"></param>
        /// <param name="action"></param>
        void OnManageRecurringProduct(IapResult iapResult, PurchaseData purchase, RecurringAction action);
        
        /// <summary>
        /// Called when an update of the service is required.
        /// If this method is called, it should call [PurchaseClientImpl.LaunchUpdateOfInstallFlow()].
        /// This is because the minimum version of the service to use the SDK is not met.
        /// </summary>
        void OnNeedUpdate();

        /// <summary>
        /// Called when a user's token login is required.
        /// If you are logged in to the service and the login token has not expired, you do not need to invoke it.
        /// </summary>
        void OnNeedLogin();
    }
}

#endif
