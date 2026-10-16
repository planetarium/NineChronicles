using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Nekoyume.ApiClient
{
    // No Unity or key-store dependency: caller supplies the already-unlocked sign operation and
    // a fresh identity snapshot. The only URL returned contains an opaque, short-lived code.
    public sealed class MarketFilterLoginClient : IDisposable
    {
        public const string BrowserBaseUrl = "https://api.9capi.com/market-filters/#handoff=";
        private const string Origin = "https://api.9capi.com";
        private const string Audience = "https://api.9capi.com/market-filters";
        private readonly HttpClient _http;
        private readonly Func<long> _now;
        private int _busy;

        public MarketFilterLoginClient(HttpMessageHandler handler = null, Func<long> now = null)
        {
            _http = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }, true)
            {
                Timeout = TimeSpan.FromSeconds(12)
            };
            _now = now ?? (() => DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        public sealed class Identity
        {
            public string Chain { get; }
            public string AgentAddress { get; }
            public string AvatarAddress { get; }
            public string PublicKey { get; }

            public Identity(string chain, string agentAddress, string avatarAddress, string publicKey)
            {
                if (chain != "odin" && chain != "heimdall") throw new ArgumentException("Unsupported chain");
                Chain = chain;
                AgentAddress = NormalizeAddress(agentAddress);
                AvatarAddress = NormalizeAddress(avatarAddress);
                if (publicKey == null || publicKey.Length != 66 ||
                    (publicKey.Substring(0, 2) != "02" && publicKey.Substring(0, 2) != "03") ||
                    !IsHex(publicKey)) throw new ArgumentException("Invalid public key");
                PublicKey = publicKey.ToLowerInvariant();
            }

            public bool SameAs(Identity other) => other != null && Chain == other.Chain &&
                AgentAddress == other.AgentAddress && AvatarAddress == other.AvatarAddress &&
                PublicKey == other.PublicKey;
        }

        public static string NormalizeAddress(string address)
        {
            if (address == null || address.Length != 42 || !address.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
                !IsHex(address.Substring(2))) throw new ArgumentException("Invalid address");
            return "0x" + address.Substring(2).ToLowerInvariant();
        }

        private static bool IsHex(string text)
        {
            foreach (var c in text)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return false;
            return true;
        }

        private static bool IsBase64Url43(string text)
        {
            if (text == null || text.Length != 43) return false;
            foreach (var c in text)
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                      (c >= '0' && c <= '9') || c == '-' || c == '_')) return false;
            // 32 bytes have 2 unused low bits in their final base64 character.
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
            return (alphabet.IndexOf(text[42]) & 3) == 0;
        }

        private static string B64(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        private static string B64(string text) => B64(Encoding.UTF8.GetBytes(text));

        private static JsonElement Fields(string json, params string[] names)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw new InvalidOperationException();
            var count = 0;
            foreach (var property in root.EnumerateObject())
            {
                if (Array.IndexOf(names, property.Name) < 0) throw new InvalidOperationException();
                count++;
            }
            if (count != names.Length) throw new InvalidOperationException();
            foreach (var name in names)
                if (!root.TryGetProperty(name, out _)) throw new InvalidOperationException();
            return root.Clone();
        }

        private static string String(JsonElement root, string name) => root.GetProperty(name).GetString() ?? throw new InvalidOperationException();
        private static long Number(JsonElement root, string name) => root.GetProperty(name).GetInt64();

        public static byte[] DerToJose(byte[] der)
        {
            if (der == null || der.Length < 8 || der.Length > 72 || der[0] != 0x30 || der[1] != der.Length - 2)
                throw new InvalidOperationException();
            var result = new byte[64];
            var offset = 2;
            for (var i = 0; i < 2; i++)
            {
                if (offset + 2 > der.Length || der[offset++] != 2) throw new InvalidOperationException();
                var size = der[offset++];
                if (size < 1 || size > 33 || offset + size > der.Length ||
                    (der[offset] & 0x80) != 0 ||
                    (size > 1 && der[offset] == 0 && (der[offset + 1] & 0x80) == 0)) throw new InvalidOperationException();
                var skip = size == 33 ? 1 : 0;
                if (skip == 1 && der[offset] != 0) throw new InvalidOperationException();
                Buffer.BlockCopy(der, offset + skip, result, i * 32 + 32 - size + skip, size - skip);
                offset += size;
            }
            if (offset != der.Length) throw new InvalidOperationException();
            return result;
        }

        private static void CheckIdentity(Identity expected, Func<Identity> current)
        {
            if (!expected.SameAs(current())) throw new InvalidOperationException();
        }

        private static string Proof(JsonElement challenge, Identity id, Func<byte[], byte[]> sign, long now)
        {
            var issued = Number(challenge, "issuedAt");
            var expires = Number(challenge, "expiresAt");
            var challengeId = String(challenge, "challengeId");
            if (!Guid.TryParseExact(challengeId, "D", out _) ||
                !IsBase64Url43(String(challenge, "nonce")) ||
                String(challenge, "audience") != Audience || String(challenge, "purpose") != "market-filters-login" ||
                String(challenge, "chain") != id.Chain || String(challenge, "agentAddress") != id.AgentAddress ||
                String(challenge, "avatarAddress") != id.AvatarAddress ||
                issued > now + 5 || issued < now - 60 || expires != issued + 60 || expires <= now)
                throw new InvalidOperationException();
            var header = B64("{\"alg\":\"ES256K\",\"typ\":\"JWT\"}");
            var payload = JsonSerializer.Serialize(new
            {
                iss = "ninechronicles", aud = Audience, purpose = "market-filters-login",
                sub = id.PublicKey, jti = challengeId, nonce = String(challenge, "nonce"),
                chain = id.Chain, agent_address = id.AgentAddress, avatar_address = id.AvatarAddress,
                iat = issued, exp = expires
            });
            var input = header + "." + B64(payload);
            return input + "." + B64(DerToJose(sign(Encoding.ASCII.GetBytes(input))));
        }

        private async Task<string> Post(HttpRequestMessage request)
        {
            using (request)
            using (var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(12)))
            using (var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token))
            {
                if (response.StatusCode != HttpStatusCode.OK || response.Content == null ||
                    response.Content.Headers.ContentLength > 4096) throw new InvalidOperationException();
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var memory = new MemoryStream())
                {
                    var buffer = new byte[1024];
                    int read;
                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, deadline.Token)) != 0)
                    {
                        if (memory.Length + read > 4096) throw new InvalidOperationException();
                        memory.Write(buffer, 0, read);
                    }
                    return new UTF8Encoding(false, true).GetString(memory.ToArray());
                }
            }
        }

        public async Task<string> LoginAsync(Identity identity, Func<Identity> current, Func<byte[], byte[]> sign)
        {
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) throw new InvalidOperationException();
            try
            {
                if (identity == null || current == null || sign == null) throw new InvalidOperationException();
                CheckIdentity(identity, current);
                var request = new HttpRequestMessage(HttpMethod.Post, Origin + "/market-filters/api/auth/challenge");
                request.Content = new StringContent(JsonSerializer.Serialize(new
                {
                    chain = identity.Chain, agentAddress = identity.AgentAddress, avatarAddress = identity.AvatarAddress
                }), Encoding.UTF8, "application/json");
                var challenge = Fields(await Post(request), "challengeId", "nonce", "audience", "purpose",
                    "chain", "agentAddress", "avatarAddress", "issuedAt", "expiresAt");
                // Check selection before invoking the unlocked agent signer, not only before network access.
                CheckIdentity(identity, current);
                var proof = Proof(challenge, identity, bytes =>
                {
                    CheckIdentity(identity, current);
                    return sign(bytes);
                }, _now());
                if (_now() >= Number(challenge, "expiresAt")) throw new InvalidOperationException();
                var handoff = new HttpRequestMessage(HttpMethod.Post, Origin + "/market-filters/api/auth/handoff");
                handoff.Headers.Authorization = new AuthenticationHeaderValue("Bearer", proof);
                handoff.Headers.Add("X-Agent-Address", identity.AgentAddress);
                handoff.Headers.Add("X-Avatar-Address", identity.AvatarAddress);
                var result = Fields(await Post(handoff), "code", "expiresIn");
                var code = String(result, "code");
                if (!IsBase64Url43(code) || Number(result, "expiresIn") != 30) throw new InvalidOperationException();
                CheckIdentity(identity, current);
                return BrowserBaseUrl + code;
            }
            finally
            {
                Interlocked.Exchange(ref _busy, 0);
            }
        }

        public void Dispose() => _http.Dispose();
    }
}
