using System;

namespace OneStore.Purchasing.Internal
{
    internal static class Constants
    {
        public const string Version = "21.04.00";

        public static readonly TimeSpan AsyncTimeout = TimeSpan.FromMilliseconds(30000);

        public const string BuildMethod = "build";

        public const string PurchaseClient = "com.gaa.sdk.iap.PurchaseClientImpl";
        public const string PurchasseClientSetupMethod = "initialize";
        public const string PurchaseClientStartConnectionMethod = "startConnection";
        public const string PurchaseClientLaunchPurchaseFlowMethod = "launchPurchaseFlow";
        public const string PurchaseClientQueryPurchasesMethod = "queryPurchasesAsync";
        public const string PurchaseClientQueryProductDetailsMethod = "queryProductDetailsAsync";
        public const string PurchaseClientConsumePurchaseMethod = "consumeAsync";
        public const string PurchaseClientAcknowledgePurchaseMethod = "acknowledgeAsync";
        public const string PurchaseClientManageRecurringProductMethod = "manageRecurringProductAsync";
        public const string PurchaseClientEndConnectionMethod = "endConnection";
        public const string PurchaseClientGetStoreInfoMethod = "getStoreInfoAsync";
        public const string PurchaseClientLaunchLoginFlowMethod = "launchLoginFlowAsync";
        public const string PurchaseClientLaunchUpdateOrInstallMethod = "launchUpdateOrInstallFlow";
        public const string PurchaseClientLaunchManageSubscriptionMethod = "launchManageSubscription";

        public const string PurchaseClientStateListener = "com.gaa.sdk.iap.PurchaseClientStateListener";
        public const string PurchasesUpdatedListener = "com.gaa.sdk.iap.PurchasesUpdatedListener";
        public const string QueryPurchasesListener = "com.gaa.sdk.iap.QueryPurchasesListener";
        public const string ProductDetailsListener = "com.gaa.sdk.iap.ProductDetailsListener";
        public const string ConsumeListener = "com.gaa.sdk.iap.ConsumeListener";
        public const string AcknowledgeListener = "com.gaa.sdk.iap.AcknowledgeListener";
        public const string RecurringProductListener = "com.gaa.sdk.iap.RecurringProductListener";
        public const string StoreInfoListener = "com.gaa.sdk.iap.StoreInfoListener";
        public const string IapResultListener = "com.gaa.sdk.iap.IapResultListener";

        public const string AcknowledgeParamsBuilder = "com.gaa.sdk.iap.AcknowledgeParams$Builder";
        public const string AcknowledgeParamsBuilderSetPurchaseDataMethod = "setPurchaseData";

        public const string ConsumeParamsBuilder = "com.gaa.sdk.iap.ConsumeParams$Builder";
        public const string ConsumeParamsBuilderSetPurchaseDataMethod = "setPurchaseData";

        public const string PurchaseFlowParamsBuilder = "com.gaa.sdk.iap.PurchaseFlowParams$Builder";
        public const string PurchaseFlowParamsBuilderSetProductIdMethod = "setProductId";
        public const string PurchaseFlowParamsBuilderSetProductNameMethod = "setProductName";
        public const string PurchaseFlowParamsBuilderSetProductTypeMethod = "setProductType";
        public const string PurchaseFlowParamsBuilderSetDeveloperPayloadMethod = "setDeveloperPayload";
        public const string PurchaseFlowParamsBuilderSetGameUserIdMethod = "setGameUserId";
        public const string PurchaseFlowParamsBuilderSetPromotionApplicableMethod = "setPromotionApplicable";
        public const string PurchaseFlowParamsBuilderSetQuantityMethod = "setQuantity";
        public const string PurchaseFlowParamsBuilderSetSubscriptionUpdateParamsMethod = "setSubscriptionUpdateParams";
        public const string SubscriptionUpdateParamsBuilder = "com.gaa.sdk.iap.PurchaseFlowParams$SubscriptionUpdateParams$Builder";
        public const string SubscriptionUpdateParamsBuilderSetProrationModeMehtod = "setProrationMode";
        public const string SubscriptionUpdateParamsBuilderSetOldPurchaseTokenMethod = "setOldPurchaseToken";
        
        public const string PurchaseDataClass = "com.gaa.sdk.iap.PurchaseData";
        public const string PurchaseDataGetOriginalJsonMethod = "getOriginalJson";
        public const string PurchaseDataGetSignatureMethod = "getSignature";

        public const string PurchasesResultGetIapResultMethod = "getIapResult";
        public const string PurchasesResultGetPurchasesDataListMethod = "getPurchaseDataList";
        
        public const string ProductDetailClass = "com.gaa.sdk.iap.ProductDetail";
        public const string ProductDetailGetOriginalJson = "getOriginalJson";

        public const string ProductDetailsParamBuilder = "com.gaa.sdk.iap.ProductDetailsParams$Builder";
        public const string ProductDetailsParamBuilderSetProductIdListMethod = "setProductIdList";
        public const string ProductDetailParamBuilderSetProductTypeMethod = "setProductType";
        
        public const string RecurringProductParamsBuilder = "com.gaa.sdk.iap.RecurringProductParams$Builder";
        public const string RecurringProductParamsBuilderSetPurchaseDataMethod = "setPurchaseData";
        public const string RecurringProductParamsBuilderSetRecurringActionMethod = "setRecurringAction";

        public const string SubscriptionParamsBuilder = "com.gaa.sdk.iap.SubscriptionParams$Builder";
        public const string SubscriptionParamsBuilderSetPurchaseDataMethod = "setPurchaseData";
    }
}