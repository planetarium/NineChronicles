using System.Net;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Responses
{
    public class Response422 : IResponse
    {
        public HttpStatusCode StatusCode => HttpStatusCode.UnprocessableEntity;

        public ValidationError[] Detail { get; set; }
    }
}
