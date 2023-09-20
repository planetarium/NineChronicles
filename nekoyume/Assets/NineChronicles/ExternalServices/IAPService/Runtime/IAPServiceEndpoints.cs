using System;

namespace NineChronicles.ExternalServices.IAPService.Runtime
{
    public struct IAPServiceEndpoints
    {
        public readonly string Url;
        public readonly Uri Ping;
        public readonly Uri Product;
        public readonly Uri PurchaseRequest;
        public readonly Uri PurchaseStatus;
        public readonly Uri L10N;

        public IAPServiceEndpoints(string url)
        {
            Url = url;
            Ping = new Uri(Url + "/ping");
            Product = new Uri(Url + "/api/product");
            PurchaseRequest = new Uri(Url + "/api/purchase/request");
            PurchaseStatus = new Uri(Url + "/api/purchase/status");
            L10N = new Uri(Url + "/api/l10n");
        }
    }
}
