using System.Security.Cryptography;
using System.Text;
using Libplanet.Common;
using Libplanet.Crypto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nekoyume.Telemetry
{
    public static class TelemetrySigner
    {
        public static byte[] BuildMessage(string payloadJson)
        {
            byte[] hash;
            using (var sha = SHA256.Create())
            {
                hash = sha.ComputeHash(Encoding.UTF8.GetBytes(payloadJson));
            }

            return Encoding.UTF8.GetBytes(
                "NC-TELEMETRY/v1\n" + ByteUtil.Hex(hash).ToLowerInvariant());
        }

        public static (string publicKeyHex, string signatureHex) Sign(
            PrivateKey key,
            string payloadJson)
        {
            var sig = key.Sign(BuildMessage(payloadJson));
            return (
                ByteUtil.Hex(key.PublicKey.Format(true)).ToLowerInvariant(),
                ByteUtil.Hex(sig).ToLowerInvariant());
        }

        public static string Envelope(string payloadJson, string publicKeyHex, string signatureHex)
        {
            var o = new JObject
            {
                ["payload"] = payloadJson,
                ["publicKey"] = publicKeyHex,
                ["signature"] = signatureHex,
            };
            return o.ToString(Formatting.None);
        }
    }
}
