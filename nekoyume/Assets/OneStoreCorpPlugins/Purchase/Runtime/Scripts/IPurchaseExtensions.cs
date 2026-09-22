#if UNITY_ANDROID || !UNITY_EDITOR

using System.Collections.ObjectModel;
using OneStore.Purchasing.Internal;

namespace OneStore.Purchasing
{
    public interface IPurchaseExtensions
    {
        void Initialize(IPurchaseCallback callback);

        void EndConnection();

        void QueryProductDetails(ReadOnlyCollection<string> productIds, ProductType type);

        void QueryPurchases(ProductType type);

        void Purchase(PurchaseFlowParams purchaseFlowParams);

        void UpdateSubscription(PurchaseFlowParams purchaseFlowParams);

        void ConsumePurchase(PurchaseData purchaseData);

        void AcknowledgePurchase(PurchaseData purchaseData, ProductType type);

        void ManageRecurringProduct(PurchaseData purchaseData, RecurringAction action);

        void LaunchManageSubscription(PurchaseData purchaseData);
    }
}

#endif
