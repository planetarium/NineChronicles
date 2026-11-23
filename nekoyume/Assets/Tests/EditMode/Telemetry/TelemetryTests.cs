using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Libplanet.Common;
using Libplanet.Crypto;
using Newtonsoft.Json.Linq;
using Nekoyume.Telemetry;
using NUnit.Framework;

namespace Nekoyume.Tests.Telemetry
{
    public class TelemetryTests
    {
        private const string VectorPayload =
            "{\"v\":1,\"batchId\":\"00000000000000000000000000000001\",\"ts\":1795400000000,\"agentAddress\":\"0x0fce078fbc2e9fd808f8983c250c753a0130067b\",\"client\":{\"planet\":\"0x000000000000\",\"platform\":\"android\",\"version\":\"510.0.1\"},\"events\":[]}";

        private const string VectorPublicKey =
            "02e4f53b45a3773df42a2970f824b2c9ec34753662b986b4b2e43f87bda2265f7b";

        private const string VectorSignature =
            "30440220016339fa9a1959ce8a3c90efd7ce848489d46ee69cb795bf0dd5cc7540067a0a0220185ec5b0614d7b9594e4c67ba952ccf59d5024992232115e689600bbd512a5eb";

        private const string VectorAddress = "0x0fce078fbc2e9fd808f8983c250c753a0130067b";

        private static TelemetryEvent Ev(string name, long ts = 1)
        {
            return new TelemetryEvent
            {
                Name = name,
                Ts = ts,
                Rpc = "rpc",
                AvatarAddress = null,
                Props = new Dictionary<string, string> { { "code", "27" } },
            };
        }

        [Test]
        public void Signer_VerifiesAgainstFixedVector()
        {
            var pub = new PublicKey(ByteUtil.ParseHex(VectorPublicKey));
            var ok = pub.Verify(
                TelemetrySigner.BuildMessage(VectorPayload),
                ByteUtil.ParseHex(VectorSignature));
            Assert.IsTrue(ok);
            Assert.AreEqual(VectorAddress, "0x" + new Address(pub).ToHex().ToLowerInvariant());
        }

        [Test]
        public void Signer_MessageFormat()
        {
            var hash = ByteUtil.Hex(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes("x")));
            var expected = Encoding.UTF8.GetBytes("NC-TELEMETRY/v1\n" + hash.ToLowerInvariant());
            Assert.AreEqual(expected, TelemetrySigner.BuildMessage("x"));
        }

        [Test]
        public void Signer_SignThenVerify()
        {
            var key = new PrivateKey();
            var (pubHex, sigHex) = TelemetrySigner.Sign(key, "{\"a\":1}");
            Assert.AreEqual(66, pubHex.Length);
            var pub = new PublicKey(ByteUtil.ParseHex(pubHex));
            Assert.AreEqual(key.PublicKey, pub);
            Assert.IsTrue(pub.Verify(
                TelemetrySigner.BuildMessage("{\"a\":1}"),
                ByteUtil.ParseHex(sigHex)));
            var env = JObject.Parse(TelemetrySigner.Envelope("{\"a\":1}", pubHex, sigHex));
            Assert.AreEqual("{\"a\":1}", (string)env["payload"]);
            Assert.AreEqual(pubHex, (string)env["publicKey"]);
            Assert.AreEqual(sigHex, (string)env["signature"]);
        }

        [Test]
        public void Envelope_Unsigned_HasNulls()
        {
            var env = JObject.Parse(TelemetrySigner.Envelope("{}", null, null));
            Assert.AreEqual(JTokenType.Null, env["publicKey"].Type);
            Assert.AreEqual(JTokenType.Null, env["signature"].Type);
        }

        [Test]
        public void Payload_KeysMatchWireFormat()
        {
            var json = TelemetryPayload.Serialize(
                "abc", 5, "0xaa", "0x000000000000", "android", "510.0.1",
                new List<TelemetryEvent> { Ev("Unity/Error", 7) });
            var o = JObject.Parse(json);
            Assert.AreEqual(1, (int)o["v"]);
            Assert.AreEqual("abc", (string)o["batchId"]);
            Assert.AreEqual(5L, (long)o["ts"]);
            Assert.AreEqual("0xaa", (string)o["agentAddress"]);
            Assert.AreEqual("0x000000000000", (string)o["client"]["planet"]);
            Assert.AreEqual("android", (string)o["client"]["platform"]);
            Assert.AreEqual("510.0.1", (string)o["client"]["version"]);
            var e = o["events"][0];
            Assert.AreEqual("Unity/Error", (string)e["name"]);
            Assert.AreEqual(7L, (long)e["ts"]);
            Assert.AreEqual("rpc", (string)e["rpc"]);
            Assert.IsNotNull(e["avatarAddress"]);
            Assert.AreEqual("27", (string)e["props"]["code"]);
        }

        [Test]
        public void FilterProps_DropsUnknownKeys()
        {
            var r = TelemetryEvents.FilterProps("Unity/Error", new Dictionary<string, string>
            {
                { "code", "1" }, { "key", "K" }, { "secret", "s" },
            });
            Assert.AreEqual(2, r.Count);
            Assert.IsFalse(r.ContainsKey("secret"));
            Assert.AreEqual(0, TelemetryEvents.FilterProps(
                "Unity/RPC Disconnected", new Dictionary<string, string> { { "a", "b" } }).Count);
            CollectionAssert.AreEqual(new[] { "Unity/Intro/Start/AgentInitializeFailed" }, new List<string>(TelemetryEvents.Unsigned));
            Assert.AreEqual(6, TelemetryEvents.Tracked.Count);
        }

        [Test]
        public void Queue_OverCap_DropsOldest()
        {
            var q = new TelemetryQueue(3);
            for (var i = 1; i <= 5; i++) q.Enqueue(Ev("Unity/Error", i));
            Assert.AreEqual(3, q.Count);
            var b = q.TakeBatch();
            Assert.AreEqual(new long[] { 3, 4, 5 }, new[] { b[0].Ts, b[1].Ts, b[2].Ts });
        }

        [Test]
        public void Queue_TakeBatchAndRequeue_PreservesOrder()
        {
            var q = new TelemetryQueue();
            for (var i = 1; i <= 4; i++) q.Enqueue(Ev("Unity/Error", i));
            var b = q.TakeBatch(2);
            Assert.AreEqual(2, q.Count);
            q.Requeue(b);
            Assert.AreEqual(4, q.Count);
            var all = q.TakeBatch();
            Assert.AreEqual(new long[] { 1, 2, 3, 4 }, new[] { all[0].Ts, all[1].Ts, all[2].Ts, all[3].Ts });
        }

        [Test]
        public void Queue_RoundTripsJson()
        {
            var q = new TelemetryQueue();
            q.Enqueue(Ev("Unity/Error", 1));
            q.Enqueue(Ev("Unity/RPC Disconnected", 2));
            var q2 = new TelemetryQueue();
            q2.FromJson(q.ToJson());
            Assert.AreEqual(2, q2.Count);
            var b = q2.TakeBatch();
            Assert.AreEqual("Unity/RPC Disconnected", b[1].Name);
            Assert.AreEqual("27", b[0].Props["code"]);
        }

        [Test]
        public void Queue_FromJson_Garbage_IsEmpty()
        {
            var q = new TelemetryQueue();
            q.FromJson("not json");
            Assert.AreEqual(0, q.Count);
        }

        [Test]
        public void Backoff_GrowsAndCapsWithJitter()
        {
            var rng = new Random(1);
            for (var i = 0; i < 50; i++)
            {
                AssertBetween(TelemetryBackoff.NextDelay(0, rng), 60, 90);
                AssertBetween(TelemetryBackoff.NextDelay(1, rng), 60, 90);
                AssertBetween(TelemetryBackoff.NextDelay(2, rng), 120, 150);
                AssertBetween(TelemetryBackoff.NextDelay(10, rng), 1800, 1830);
                AssertBetween(TelemetryBackoff.NextDelay(100, rng), 1800, 1830);
            }
        }

        private static void AssertBetween(TimeSpan d, double lo, double hi)
        {
            Assert.GreaterOrEqual(d.TotalSeconds, lo);
            Assert.LessOrEqual(d.TotalSeconds, hi);
        }

        [Test]
        public void ClockOffset_FromServerDate_CorrectsTimestamp()
        {
            var clock = new TelemetryClock();
            var local = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Assert.That(clock.Now(), Is.EqualTo(local).Within(1000));
            var server = DateTimeOffset.UtcNow.AddMinutes(10);
            clock.Observe(server);
            Assert.That(clock.Now(), Is.EqualTo(server.ToUnixTimeMilliseconds()).Within(1000));
        }
    }
}
