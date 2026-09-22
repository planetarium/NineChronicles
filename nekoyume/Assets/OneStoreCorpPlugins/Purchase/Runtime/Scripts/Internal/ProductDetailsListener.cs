using System;
using UnityEngine;

namespace OneStore.Purchasing.Internal
{
    /// <summary>
    /// Callback when a fetch Product details operation has finished
    /// Public documentation can be found at
    /// https://onestore-dev.gitbook.io/dev/v/eng/tools/tools/v21/references/en-interfaces/en-productdetailslistener
    /// </summary>
    public class ProductDetailsListener : AndroidJavaProxy
    {
        private ProductType _type;
        public event Action<ProductType, AndroidJavaObject, AndroidJavaObject> OnProductDetailsResponse = delegate { };
        public ProductDetailsListener(ProductType type) : base(Constants.ProductDetailsListener)
        {
            _type = type;
        }

        void onProductDetailsResponse(AndroidJavaObject iapResult, AndroidJavaObject productDetailList)
        {
            OnProductDetailsResponse.Invoke(_type, iapResult, productDetailList);
        }
    }
}
