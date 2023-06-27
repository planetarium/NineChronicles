using System.Net;
using System.Text.Json.Serialization;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Responses
{
    public interface IResponse
    {
        [JsonIgnore]
        HttpStatusCode StatusCode { get; }
    }
}
