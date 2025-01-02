using System;
using System.Text;
using Newtonsoft.Json;
using Libplanet.Common;
using Libplanet.Crypto;
using System.Security.Cryptography;

namespace Nekoyume.ApiClient
{
    public class ArenaServiceManager : IDisposable
    {
        public ArenaServiceClient Client { get; private set; }

        public bool IsInitialized => Client != null;

        public ArenaServiceManager(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                NcDebug.LogError($"ArenaServiceManager Initialized Fail url is Null");
                return;
            }
            Client = new ArenaServiceClient(url);
        }

        public void Dispose()
        {
            Client?.Dispose();
            Client = null;
        }

        public static string CreateJwt(PrivateKey privateKey, string address)
        {
            var payload = new
            {
                aud = "arena-service",
                avt_adr = address,
                sub = privateKey.PublicKey.ToHex(true),
                iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                exp = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds()
            };
            string payloadJson = JsonConvert.SerializeObject(payload);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
            byte[] hash = HashDigest<SHA256>.DeriveFrom(payloadBytes).ToByteArray();
            byte[] signature = privateKey.Sign(hash);
            string signatureBase64 = Convert.ToBase64String(signature);
            string headerJson = JsonConvert.SerializeObject(new { alg = "ES256K", typ = "JWT" });
            string headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerJson));
            string payloadBase64 = Convert.ToBase64String(payloadBytes);
            return $"{headerBase64}.{payloadBase64}.{signatureBase64}";
        }
    }
}
