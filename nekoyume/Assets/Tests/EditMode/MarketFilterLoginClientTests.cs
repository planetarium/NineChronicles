using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nekoyume.ApiClient;
using NUnit.Framework;

namespace Nekoyume.Tests.EditMode
{
    public class MarketFilterLoginClientTests
    {
        // Public, synthetic test identity. No wallet files or real keys are used.
        private const string Agent = "0x7e5f4552091a69125d5dfcb7b8c2659029395bdf";
        private const string Avatar = "0x7dd7cec8b67e102b2746d400d2ca0e5e3bb56ba1";
        private const string PublicKey = "0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798";
        private const string Code = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        private const long Now = 1760000000;
        private static MarketFilterLoginClient.Identity Identity =>
            new MarketFilterLoginClient.Identity("odin", Agent, Avatar, PublicKey);
        // Shape-only signature for the fake HTTP boundary; real crypto is independently tested.
        private static byte[] Sign(byte[] _) => new byte[] { 0x30, 6, 2, 1, 1, 2, 1, 1 };
        private const string Challenge = "{\"challengeId\":\"00000000-0000-4000-8000-000000000001\","
            + "\"nonce\":\"" + Code + "\",\"audience\":\"https://api.9capi.com/market-filters\","
            + "\"purpose\":\"market-filters-login\",\"chain\":\"odin\",\"agentAddress\":\"" + Agent + "\","
            + "\"avatarAddress\":\"" + Avatar + "\",\"issuedAt\":1759999999,\"expiresAt\":1760000059}";
        private const string Handoff = "{\"code\":\"" + Code + "\",\"expiresIn\":30}";

        [Test]
        public async Task LoginUsesPinnedOriginAndReturnsOnlyOpaqueFragment()
        {
            var requests = 0;
            using var client = new MarketFilterLoginClient(new Handler(request =>
            {
                Assert.That(request.RequestUri.Host, Is.EqualTo("api.9capi.com"));
                Assert.That(request.RequestUri.Scheme, Is.EqualTo("https"));
                requests++;
                Assert.That(request.RequestUri.AbsolutePath, Is.EqualTo(requests == 1
                    ? "/market-filters/api/auth/challenge" : "/market-filters/api/auth/handoff"));
                if (requests == 1) Assert.That(request.Headers.Authorization, Is.Null);
                else
                {
                    Assert.That(request.Headers.Authorization.Scheme, Is.EqualTo("Bearer"));
                    Assert.That(request.Headers.Authorization.Parameter.Split('.').Length, Is.EqualTo(3));
                    Assert.That(request.Content, Is.Null);
                }
                return Task.FromResult(Reply(requests == 1 ? Challenge : Handoff));
            }), () => Now);
            var url = await client.LoginAsync(Identity, () => Identity, Sign);
            Assert.That(url, Is.EqualTo("https://api.9capi.com/market-filters/#handoff=" + Code));
            Assert.That(requests, Is.EqualTo(2));
        }

        [TestCase("https://api.9capi.com/market-filters", "https://untrusted.invalid")]
        [TestCase("https://api.9capi.com/market-filters", "https://api.9capi.com/market-filters-signed")]
        [TestCase("market-filters-login", "other-purpose")]
        [TestCase("\"chain\":\"odin\"", "\"chain\":\"heimdall\"")]
        [TestCase("1759999999", "1760000100")]
        [TestCase("1760000059", "1760000200")]
        public void HostileChallengeIsRejectedBeforeSigning(string before, string after)
        {
            var signed = false;
            using var client = new MarketFilterLoginClient(new Handler(_ =>
                Task.FromResult(Reply(Challenge.Replace(before, after)))), () => Now);
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await client.LoginAsync(Identity, () => Identity, bytes => { signed = true; return Sign(bytes); }));
            Assert.That(signed, Is.False);
        }

        [Test]
        public void RedirectIsNotAnApprovedHandoff()
        {
            using var client = new MarketFilterLoginClient(new Handler(_ => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.Redirect)
                { Headers = { Location = new Uri("https://untrusted.invalid") } })), () => Now);
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await client.LoginAsync(Identity, () => Identity, Sign));
        }

        [Test]
        public void ChangedIdentityIsRejectedBeforeSigning()
        {
            var current = Identity;
            var signed = false;
            using var client = new MarketFilterLoginClient(new Handler(_ =>
            {
                current = new MarketFilterLoginClient.Identity("heimdall", Agent, Avatar, PublicKey);
                return Task.FromResult(Reply(Challenge));
            }), () => Now);
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await client.LoginAsync(Identity, () => current, bytes => { signed = true; return Sign(bytes); }));
            Assert.That(signed, Is.False);
        }

        [Test]
        public void DerConversionPadsTwoJoseIntegers()
        {
            var signature = MarketFilterLoginClient.DerToJose(Sign(Array.Empty<byte>()));
            Assert.That(signature.Length, Is.EqualTo(64));
            Assert.That(signature[31], Is.EqualTo(1));
            Assert.That(signature[63], Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => MarketFilterLoginClient.DerToJose(new byte[] { 0x30, 0 }));
        }

        [TestCase("https://b.9capi.com/marketProviderOdin", "odin", true)]
        [TestCase("https://api.9capi.com/marketProviderHeimdall/", "heimdall", true)]
        [TestCase("https://b.9capi.com/marketProviderOdin", "heimdall", false)]
        [TestCase("https://api.9capi.com/cc", "odin", false)]
        [TestCase("https://market.9capi.com/marketProviderOdin", "odin", false)]
        [TestCase("http://b.9capi.com/marketProviderOdin", "odin", false)]
        [TestCase("https://b.9capi.com.evil.invalid/marketProviderOdin", "odin", false)]
        public void ProviderEligibilityIsExactAndChainBound(string url, string chain, bool allowed)
        {
            Assert.That(new MarketServiceClient(url).SupportsSignedMarketFiltersForChain(chain), Is.EqualTo(allowed));
        }

        private static HttpResponseMessage Reply(string json) => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        private sealed class Handler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _send;
            public Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) => _send = send;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => _send(request);
        }
    }
}
