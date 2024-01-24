using System;

namespace NineChronicles.ExternalServices.IAPService.Runtime
{
    public struct IAPServiceEndpoints
    {
        public readonly string Url;
        public readonly Uri Ping;
        public readonly Uri Product;
        public readonly Uri PurchaseRequest;
        public readonly Uri PurchaseFree;
        public readonly Uri PurchaseStatus;
        public readonly Uri PurchaseLog;
        public readonly Uri L10N;

        public IAPServiceEndpoints(string url)
        {
            Url = url;
            Ping = new Uri(Url + "/ping");
            Product = new Uri(Url + "/api/product");
            PurchaseRequest = new Uri(Url + "/api/purchase/request");
            PurchaseFree = new Uri(Url + "/api/purchase/free");
            PurchaseStatus = new Uri(Url + "/api/purchase/status");
            PurchaseLog = new Uri(Url + "/api/purchase/log");
            L10N = new Uri(Url + "/api/l10n");
        }
    }
}
