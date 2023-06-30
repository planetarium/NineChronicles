using System;

namespace NineChronicles.ExternalServices.IAPService.Runtime
{
    public struct IAPServiceEndpoints
    {
        public readonly string Url;
        public readonly Uri Ping;
        public readonly Uri Product;
        public readonly Uri Purchase;
        public readonly Uri PurchaseStatus;

        public IAPServiceEndpoints(string url)
        {
            Url = url;
            Ping = new Uri(Url + "/ping");
            Product = new Uri(Url + "/api/product");
            Purchase = new Uri(Url + "/api/purchase/request");
            PurchaseStatus = new Uri(Url + "/api/purchase/status");
        }
    }
}
