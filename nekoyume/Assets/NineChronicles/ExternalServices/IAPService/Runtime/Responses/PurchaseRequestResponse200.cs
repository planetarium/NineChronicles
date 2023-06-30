using System.Net;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Responses
{
    public class PurchaseRequestResponse200 : IResponse
    {
        public HttpStatusCode StatusCode => HttpStatusCode.OK;

        public ReceiptDetailSchema Content { get; set; }
    }
}
