using System.Net;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Responses
{
    public class ProductResponse200 : IResponse
    {
        public HttpStatusCode StatusCode => HttpStatusCode.OK;

        public ProductSchema[] Content { get; set; }
    }
}
