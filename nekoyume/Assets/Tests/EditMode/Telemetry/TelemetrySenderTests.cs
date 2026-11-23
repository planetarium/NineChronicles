using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Libplanet.Common;
using Libplanet.Crypto;
using Newtonsoft.Json.Linq;
using Nekoyume.Telemetry;
using NUnit.Framework;

namespace Nekoyume.Tests.Telemetry
{
    public class TelemetrySenderTests
    {
        private class FakePost
        {
            public readonly List<string> Bodies = new List<string>();
            public Func<(int status, DateTimeOffset? date)> Respond = () => (204, null);

            public Task<(int status, DateTimeOffset? date)> Post(string batchId, string body)
            {
                Bodies.Add(body);
                return Task.FromResult(Respond());
            }
        }

        private static readonly Func<(string planet, string platform, string version, string agentAddress)>
            Context = () => ("0x000000000000", "android", "510.0.1", null);

        private static TelemetrySender NewSender(
            FakePost post,
            TelemetryQueue queue,
            Func<PrivateKey> keyProvider,
            TelemetryClock clock = null,
            string persistPath = null)
        {
            return new TelemetrySender(
                post.Post,
                queue,
                clock ?? new TelemetryClock(),
                keyProvider,
                Context,
                persistPath);
        }

        private static Dictionary<string, string> ErrorProps()
        {
            return new Dictionary<string, string> { { "code", "27" }, { "key", "ERROR_NETWORK" } };
        }

        [Test]
        public async Task Flush_SignedBatch_WhenKeyAvailable()
        {
            var key = new PrivateKey();
            var post = new FakePost();
            var queue = new TelemetryQueue();
            var sender = NewSender(post, queue, () => key);
            sender.Enqueue(TelemetryEvents.UnityError, ErrorProps(), "odin-rpc-1.nine-chronicles.com", null);
            sender.Enqueue(TelemetryEvents.RpcDisconnected, null, "odin-rpc-1.nine-chronicles.com", "0xabc");

            await sender.FlushAsync();

            Assert.AreEqual(1, post.Bodies.Count);
            var env = JObject.Parse(post.Bodies[0]);
            var payloadJson = (string)env["payload"];
            var pubHex = (string)env["publicKey"];
            var sigHex = (string)env["signature"];
            Assert.AreEqual(ByteUtil.Hex(key.PublicKey.Format(true)).ToLowerInvariant(), pubHex);
            Assert.IsTrue(key.PublicKey.Verify(
                TelemetrySigner.BuildMessage(payloadJson),
                ByteUtil.ParseHex(sigHex)));

            var payload = JObject.Parse(payloadJson);
            Assert.AreEqual(2, ((JArray)payload["events"]).Count);
            Assert.AreEqual(
                "0x" + key.Address.ToHex().ToLowerInvariant(),
                (string)payload["agentAddress"]);
            Assert.AreEqual(32, ((string)payload["batchId"]).Length);
            Assert.AreEqual("android", (string)payload["client"]["platform"]);
            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public async Task Flush_UnsignedOnlyInitializeFailed()
        {
            var post = new FakePost();
            var queue = new TelemetryQueue();
            var sender = NewSender(post, queue, () => null);
            sender.Enqueue(TelemetryEvents.UnityError, ErrorProps(), "rpc", null);
            sender.Enqueue(TelemetryEvents.AgentInitializeFailed, null, "rpc", null);

            await sender.FlushAsync();

            Assert.AreEqual(1, post.Bodies.Count);
            var env = JObject.Parse(post.Bodies[0]);
            Assert.AreEqual(JTokenType.Null, env["publicKey"].Type);
            Assert.AreEqual(JTokenType.Null, env["signature"].Type);
            var payload = JObject.Parse((string)env["payload"]);
            Assert.AreEqual(JTokenType.Null, payload["agentAddress"].Type);
            var events = (JArray)payload["events"];
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(TelemetryEvents.AgentInitializeFailed, (string)events[0]["name"]);

            Assert.AreEqual(1, queue.Count);
            Assert.AreEqual(TelemetryEvents.UnityError, queue.TakeBatch()[0].Name);
        }

        [Test]
        public async Task Flush_NoEvents_NoRequest()
        {
            var post = new FakePost();
            var sender = NewSender(post, new TelemetryQueue(), () => new PrivateKey());

            await sender.FlushAsync();

            Assert.AreEqual(0, post.Bodies.Count);
        }

        [Test]
        public async Task Flush_NoKey_OnlySignedEvents_NoRequest()
        {
            var post = new FakePost();
            var queue = new TelemetryQueue();
            var sender = NewSender(post, queue, () => null);
            sender.Enqueue(TelemetryEvents.UnityError, ErrorProps(), "rpc", null);

            await sender.FlushAsync();

            Assert.AreEqual(0, post.Bodies.Count);
            Assert.AreEqual(1, queue.Count);
        }

        [TestCase(400)]
        [TestCase(409)]
        [TestCase(413)]
        public async Task Flush_4xx_DropsBatch(int status)
        {
            var post = new FakePost { Respond = () => (status, null) };
            var queue = new TelemetryQueue();
            var sender = NewSender(post, queue, () => new PrivateKey());
            sender.Enqueue(TelemetryEvents.UnityError, ErrorProps(), "rpc", null);

            await sender.FlushAsync();
            Assert.AreEqual(1, post.Bodies.Count);
            Assert.AreEqual(0, queue.Count);
            Assert.AreEqual(0, sender.Failures);

            await sender.FlushAsync();
            Assert.AreEqual(1, post.Bodies.Count, "poison batch must not be retried");
        }

        [TestCase(429)]
        [TestCase(503)]
        [TestCase(0)]
        public async Task Flush_RetryableStatus_RequeuesAndCountsFailure(int status)
        {
            var post = new FakePost { Respond = () => (status, null) };
            var queue = new TelemetryQueue();
            var sender = NewSender(post, queue, () => new PrivateKey());
            sender.Enqueue(TelemetryEvents.UnityError, ErrorProps(), "rpc", null);
            sender.Enqueue(TelemetryEvents.UnityError, ErrorProps(), "rpc", null);

            await sender.FlushAsync();
            Assert.AreEqual(2, queue.Count);
            Assert.AreEqual(1, sender.Failures);
            Assert.Greater(sender.NextFlushDelay, TimeSpan.FromSeconds(59));

            await sender.FlushAsync();
            Assert.AreEqual(2, queue.Count);
            Assert.AreEqual(2, sender.Failures);
            Assert.Greater(sender.NextFlushDelay, TimeSpan.FromSeconds(119));

            post.Respond = () => (204, null);
            await sender.FlushAsync();
            Assert.AreEqual(0, queue.Count);
            Assert.AreEqual(0, sender.Failures);
            Assert.AreEqual(TimeSpan.FromSeconds(60), sender.NextFlushDelay);
        }

        [Test]
        public async Task Flush_PostThrows_RequeuesAndCountsFailure()
        {
            var post = new FakePost { Respond = () => throw new InvalidOperationException("network") };
            var queue = new TelemetryQueue();
            var sender = NewSender(post, queue, () => new PrivateKey());
            sender.Enqueue(TelemetryEvents.UnityError, ErrorProps(), "rpc", null);

            await sender.FlushAsync();

            Assert.AreEqual(1, queue.Count);
            Assert.AreEqual(1, sender.Failures);
        }

        [Test]
        public async Task Flush_ObservesServerDate()
        {
            var serverDate = DateTimeOffset.UtcNow.AddHours(1);
            var post = new FakePost { Respond = () => (400, serverDate) };
            var clock = new TelemetryClock();
            var sender = NewSender(post, new TelemetryQueue(), () => new PrivateKey(), clock);
            sender.Enqueue(TelemetryEvents.UnityError, ErrorProps(), "rpc", null);

            await sender.FlushAsync();

            var skew = clock.Now() - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Assert.That(skew, Is.InRange(3600000L - 5000L, 3600000L + 5000L));
        }

        [Test]
        public void Enqueue_NotTracked_Ignored()
        {
            var queue = new TelemetryQueue();
            var sender = NewSender(new FakePost(), queue, () => null);

            sender.Enqueue("Unity/Started", null, "rpc", null);

            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public void Enqueue_FiltersPropsAndMapsProductId()
        {
            var queue = new TelemetryQueue();
            var clock = new TelemetryClock();
            var sender = NewSender(new FakePost(), queue, () => null, clock);

            sender.Enqueue(
                TelemetryEvents.ProcessPurchaseError,
                new Dictionary<string, string>
                {
                    { "product-id", "g_pkg_1" },
                    { "transaction-id", "GPA.1234" },
                    { "error", "boom" },
                },
                "odin-rpc-1.nine-chronicles.com",
                "0xavatar");

            var e = queue.TakeBatch()[0];
            Assert.AreEqual(TelemetryEvents.ProcessPurchaseError, e.Name);
            Assert.AreEqual("odin-rpc-1.nine-chronicles.com", e.Rpc);
            Assert.AreEqual("0xavatar", e.AvatarAddress);
            Assert.That(e.Ts, Is.InRange(clock.Now() - 5000, clock.Now() + 5000));
            CollectionAssert.AreEquivalent(
                new Dictionary<string, string> { { "productId", "g_pkg_1" }, { "error", "boom" } },
                e.Props);
        }

        [Test]
        public async Task PauseOrQuit_PersistsQueue_ThenRestores()
        {
            var path = Path.Combine(Path.GetTempPath(), "telemetry-queue-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var post = new FakePost { Respond = () => (503, null) };
                var sender = NewSender(post, new TelemetryQueue(), () => new PrivateKey(), persistPath: path);
                sender.Enqueue(TelemetryEvents.UnityError, ErrorProps(), "rpc", null);
                sender.Enqueue(TelemetryEvents.RpcDisconnected, null, "rpc", null);

                await sender.OnPauseOrQuitAsync();

                Assert.AreEqual(1, post.Bodies.Count, "one send attempt on pause/quit");
                Assert.IsTrue(File.Exists(path));

                var restoredQueue = new TelemetryQueue();
                var restored = NewSender(new FakePost(), restoredQueue, () => null, persistPath: path);
                restored.LoadPersisted();
                Assert.AreEqual(2, restoredQueue.Count);
                Assert.IsFalse(File.Exists(path), "persisted file is consumed on load");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public async Task PauseOrQuit_SuccessfulSend_LeavesNothingToRestore()
        {
            var path = Path.Combine(Path.GetTempPath(), "telemetry-queue-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var sender = NewSender(new FakePost(), new TelemetryQueue(), () => new PrivateKey(), persistPath: path);
                sender.Enqueue(TelemetryEvents.UnityError, ErrorProps(), "rpc", null);

                await sender.OnPauseOrQuitAsync();

                var restoredQueue = new TelemetryQueue();
                NewSender(new FakePost(), restoredQueue, () => null, persistPath: path).LoadPersisted();
                Assert.AreEqual(0, restoredQueue.Count);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void LoadPersisted_OversizedFile_Discarded()
        {
            var path = Path.Combine(Path.GetTempPath(), "telemetry-queue-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var big = new TelemetryQueue(100000);
                var pad = new string('x', 200);
                for (var i = 0; i < 2000; i++)
                {
                    big.Enqueue(new TelemetryEvent
                    {
                        Name = TelemetryEvents.UnityError,
                        Ts = i,
                        Rpc = pad,
                    });
                }

                File.WriteAllText(path, big.ToJson());
                Assert.Greater(new FileInfo(path).Length, 200 * 1024);

                var queue = new TelemetryQueue();
                NewSender(new FakePost(), queue, () => null, persistPath: path).LoadPersisted();

                Assert.AreEqual(0, queue.Count);
                Assert.IsFalse(File.Exists(path));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void HostOnly_StripsSchemeAndPort()
        {
            Assert.AreEqual("odin-rpc-1.nine-chronicles.com", TelemetrySender.HostOnly("odin-rpc-1.nine-chronicles.com"));
            Assert.AreEqual("odin-rpc-1.nine-chronicles.com", TelemetrySender.HostOnly("odin-rpc-1.nine-chronicles.com:31238"));
            Assert.AreEqual("odin-rpc-1.nine-chronicles.com", TelemetrySender.HostOnly("http://odin-rpc-1.nine-chronicles.com:31238/"));
            Assert.IsNull(TelemetrySender.HostOnly(""));
            Assert.IsNull(TelemetrySender.HostOnly(null));
        }

        [Test]
        public void PlatformName_MapsKnownPlatforms()
        {
            Assert.AreEqual("android", TelemetrySender.PlatformName(UnityEngine.RuntimePlatform.Android));
            Assert.AreEqual("ios", TelemetrySender.PlatformName(UnityEngine.RuntimePlatform.IPhonePlayer));
            Assert.AreEqual("windows", TelemetrySender.PlatformName(UnityEngine.RuntimePlatform.WindowsPlayer));
            Assert.AreEqual("macos", TelemetrySender.PlatformName(UnityEngine.RuntimePlatform.OSXPlayer));
            Assert.AreEqual("linux", TelemetrySender.PlatformName(UnityEngine.RuntimePlatform.LinuxPlayer));
        }

        private class CaptureHandler : HttpMessageHandler
        {
            public HttpMethod Method;
            public string MediaType;
            public List<string> ContentEncoding;
            public byte[] Body;

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Method = request.Method;
                MediaType = request.Content.Headers.ContentType?.MediaType;
                ContentEncoding = new List<string>(request.Content.Headers.ContentEncoding);
                Body = await request.Content.ReadAsByteArrayAsync();
                var response = new HttpResponseMessage((HttpStatusCode)204);
                response.Headers.Date = new DateTimeOffset(2026, 11, 23, 0, 0, 0, TimeSpan.Zero);
                return response;
            }
        }

        [Test]
        public async Task Http_SmallBody_PlainJsonAndReturnsStatusAndDate()
        {
            var handler = new CaptureHandler();
            var client = TelemetryHttp.CreateClient(handler);

            var (status, date) = await TelemetryHttp.PostAsync(client, "https://telemetry.example/v1/events", "{\"a\":1}");

            Assert.AreEqual(204, status);
            Assert.AreEqual(new DateTimeOffset(2026, 11, 23, 0, 0, 0, TimeSpan.Zero), date);
            Assert.AreEqual(HttpMethod.Post, handler.Method);
            Assert.AreEqual("application/json", handler.MediaType);
            CollectionAssert.IsEmpty(handler.ContentEncoding);
            Assert.AreEqual("{\"a\":1}", Encoding.UTF8.GetString(handler.Body));
            Assert.AreEqual(TimeSpan.FromSeconds(10), client.Timeout);
        }

        [Test]
        public async Task Http_LargeBody_Gzipped()
        {
            var handler = new CaptureHandler();
            var body = "{\"x\":\"" + new string('a', 5000) + "\"}";

            await TelemetryHttp.PostAsync(TelemetryHttp.CreateClient(handler), "https://telemetry.example/v1/events", body);

            CollectionAssert.AreEqual(new[] { "gzip" }, handler.ContentEncoding);
            using (var gz = new GZipStream(new MemoryStream(handler.Body), CompressionMode.Decompress))
            using (var reader = new StreamReader(gz, Encoding.UTF8))
            {
                Assert.AreEqual(body, reader.ReadToEnd());
            }
        }
    }
}
