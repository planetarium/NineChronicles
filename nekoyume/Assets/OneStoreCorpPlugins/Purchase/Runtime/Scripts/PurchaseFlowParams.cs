
namespace OneStore.Purchasing
{
    /// <summary>
    /// It is a parameter for purchasing requests.
    /// Public documentation can be found at
    /// https://onestore-dev.gitbook.io/dev/v/eng/tools/tools/v21/references/en-classes/en-purchaseflowparams
    /// </summary>
    public class PurchaseFlowParams
    {
        public string ProductId { get; private set; }
        public string ProductType { get; private set; }
        public string ProductName { get; private set; }
        public string DeveloperPayload { get; private set; }
        public string GameUserId { get; private set; }
        public bool PromotinApplicable { get; private set; }
        public int Quantity { get; private set; }
        public string OldPurchaseToken { get; private set; }
        public int ProrationMode { get; private set; }

        public PurchaseFlowParams(Builder builder)
        {
            ProductId = builder.ProductId;
            ProductType = builder.ProductType;
            ProductName = builder.ProductName;
            DeveloperPayload = builder.DeveloperPayload;
            GameUserId = builder.GameUserId;
            PromotinApplicable = builder.PromotinApplicable;
            Quantity = builder.Quantity;
            OldPurchaseToken = builder.OldPurchaseToken;
            ProrationMode = builder.ProrationMode;
        }

        /// <summary>
        /// Builder for creating PurchaseFlowParams.
        /// Public documentation can be found at
        /// https://onestore-dev.gitbook.io/dev/v/eng/tools/tools/v21/references/en-classes/en-purchaseflowparams.builder
        /// </summary>
        public class Builder
        {
            public string ProductId { get; private set; }
            public string ProductType { get; private set; }
            public string ProductName { get; private set; }
            public string DeveloperPayload { get; private set; }
            public string GameUserId { get; private set; }
            public bool PromotinApplicable { get; private set; }
            public int Quantity { get; private set; }
            public string OldPurchaseToken { get; private set; }
            public int ProrationMode { get; private set; }

            public Builder() { }

            public Builder SetProductId(string id)
            {
                ProductId = id;
                return this;
            }

            public Builder SetProductType(ProductType type)
            {
                ProductType = type.ToString();
                return this;
            }

            public Builder SetProductName(string name)
            {
                ProductName = name;
                return this;
            }

            public Builder SetDeveloperPayload(string payload)
            {
                DeveloperPayload = payload;
                return this;
            }

            public Builder SetGameUserId(string userId)
            {
                GameUserId = userId;
                return this;
            }

            public Builder SetPromotionApplicable(bool isPromotion)
            {
                PromotinApplicable = isPromotion;
                return this;
            }

            public Builder SetQuantity(int quantity)
            {
                Quantity = quantity;
                return this;
            }

            public Builder SetOldPurchaseToken(string oldPurchaseToken)
            {
                OldPurchaseToken = oldPurchaseToken;
                return this;
            }

            public Builder SetProrationMode(OneStoreProrationMode mode)
            {
                ProrationMode = (int) mode;
                return this;
            }

            public PurchaseFlowParams Build()
            {
                return new PurchaseFlowParams(this);
            }
        }
    }
}
