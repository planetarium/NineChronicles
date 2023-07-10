using System.Text.Json;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Common
{
    public static class JsonExceptionFactory
    {
        public static JsonException TokenType(
            JsonTokenType[] expected,
            JsonTokenType actual)
        {
            return new JsonException(
                $"Expected token type to be {string.Join(" or ", expected)} but got {actual}");
        }

        public static JsonException PropName(JsonTokenType actual)
        {
            return new JsonException(
                $"Expected PropertyName but got {actual}");
        }

        public static JsonException PropValue(
            string propName,
            JsonTokenType[] expected,
            JsonTokenType actual)
        {
            return new JsonException(
                $"Expected {propName} value to be {string.Join(" or ", expected)} but got {actual}");
        }
    }
}
