using System.Text.Json.Serialization;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    public class HTTPValidationError
    {
        [JsonPropertyName("detail")]
        public ValidationError[] Detail { get; set; }
    }
}
