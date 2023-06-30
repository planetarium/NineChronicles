using System.Net;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Responses
{
    public class PurchaseResponse200 : IResponse
    {
        public HttpStatusCode StatusCode => HttpStatusCode.OK;

        public PurchaseProcessResultSchema Content { get; set; }
    }
}
