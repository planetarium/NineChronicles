using System;
using Nekoyume.Multiplanetary;
using NUnit.Framework;

namespace Tests.EditMode.Planet
{
    public class RpcEndpointProberTests
    {
        [SetUp]
        public void SetUp()
        {
            RpcEndpointProber.ResetFailuresForTests();
        }

        private static RpcEndpointProber.ProbeResult Result(
            string host, bool healthy, long tip, int latencyMs)
        {
            return new RpcEndpointProber.ProbeResult(
                new Uri($"http://{host}:31238"), healthy, tip, latencyMs);
        }

        [Test]
        public void Score_BaseCase_NoPenalties_ReturnsBaseScore()
        {
            const long tip = 1000L;
            var r = Result("score-base", healthy: true, tip: tip, latencyMs: 0);
            Assert.AreEqual(1000, RpcEndpointProber.Score(r, tip));
        }

        [Test]
        public void Score_LatencyPenalty_SubtractsMillisecondsOneForOne()
        {
            const long tip = 1000L;
            var r = Result("score-latency", healthy: true, tip: tip, latencyMs: 250);
            Assert.AreEqual(750, RpcEndpointProber.Score(r, tip));
        }

        [Test]
        public void Score_FreshnessPenalty_TenPerBlockBehind()
        {
            const long maxTip = 1000L;
            var r = Result("score-freshness", healthy: true, tip: maxTip - 5, latencyMs: 0);
            Assert.AreEqual(950, RpcEndpointProber.Score(r, maxTip));
        }

        [Test]
        public void Score_FailurePenalty_OneHundredPerFailure()
        {
            const long tip = 1000L;
            const string host = "score-failure";
            RpcEndpointProber.RecordFailure(host);
            RpcEndpointProber.RecordFailure(host);

            var r = Result(host, healthy: true, tip: tip, latencyMs: 0);
            Assert.AreEqual(800, RpcEndpointProber.Score(r, tip));
        }

        [Test]
        public void Score_CombinedPenalties_StackAdditively()
        {
            const long maxTip = 1000L;
            const string host = "score-combined";
            RpcEndpointProber.RecordFailure(host);
            RpcEndpointProber.RecordFailure(host);

            var r = Result(host, healthy: true, tip: maxTip - 5, latencyMs: 100);
            // 1000 - 100 (latency) - 50 (5 blocks * 10) - 200 (2 failures * 100) = 650
            Assert.AreEqual(650, RpcEndpointProber.Score(r, maxTip));
        }

        [Test]
        public void Score_TipAboveMaxTip_NoNegativeFreshnessPenalty()
        {
            // Local tip can briefly exceed maxTip on a probe with a fast tip-advance;
            // freshness penalty should clamp to zero rather than reward.
            const long maxTip = 1000L;
            var r = Result("score-future", healthy: true, tip: maxTip + 5, latencyMs: 50);
            Assert.AreEqual(950, RpcEndpointProber.Score(r, maxTip));
        }

        [Test]
        public void ScoreHostByHistory_UnknownHost_ReturnsBaseScore()
        {
            Assert.AreEqual(1000, RpcEndpointProber.ScoreHostByHistory("unknown-host"));
        }

        [Test]
        public void ScoreHostByHistory_FailuresLowerScore()
        {
            const string host = "history-host";
            RpcEndpointProber.RecordFailure(host);
            RpcEndpointProber.RecordFailure(host);
            RpcEndpointProber.RecordFailure(host);
            Assert.AreEqual(700, RpcEndpointProber.ScoreHostByHistory(host));
        }

        [Test]
        public void RecordFailure_GetFailureCount_AccumulatesWithinDecayWindow()
        {
            const string host = "accumulate-host";
            Assert.AreEqual(0d, RpcEndpointProber.GetFailureCount(host));
            RpcEndpointProber.RecordFailure(host);
            RpcEndpointProber.RecordFailure(host);
            RpcEndpointProber.RecordFailure(host);
            Assert.AreEqual(3d, RpcEndpointProber.GetFailureCount(host), 1e-9);
        }

        [Test]
        public void RecordFailure_NullOrEmpty_NoOp()
        {
            Assert.DoesNotThrow(() => RpcEndpointProber.RecordFailure(null));
            Assert.DoesNotThrow(() => RpcEndpointProber.RecordFailure(string.Empty));
        }

        [Test]
        public void Decay_AfterOneInterval_AppliesFactorOnce()
        {
            const string host = "decay-once";
            RpcEndpointProber.RecordFailure(host);
            RpcEndpointProber.RewindFailureClockForTests(host, TimeSpan.FromSeconds(60));

            // One full interval elapsed → 1.0 * 0.9 = 0.9
            Assert.AreEqual(0.9d, RpcEndpointProber.GetFailureCount(host), 1e-9);
        }

        [Test]
        public void Decay_AfterTwoIntervals_AppliesFactorTwice()
        {
            const string host = "decay-twice";
            RpcEndpointProber.RecordFailure(host);
            RpcEndpointProber.RewindFailureClockForTests(host, TimeSpan.FromSeconds(120));

            // 0.9^2 = 0.81
            Assert.AreEqual(0.81d, RpcEndpointProber.GetFailureCount(host), 1e-9);
        }

        [Test]
        public void Decay_BelowCutoff_ClampsToZero()
        {
            const string host = "decay-cutoff";
            RpcEndpointProber.RecordFailure(host);
            // 0.9^60 ≈ 1.79e-3, well below the 0.01 cutoff.
            RpcEndpointProber.RewindFailureClockForTests(host, TimeSpan.FromSeconds(60 * 60));

            Assert.AreEqual(0d, RpcEndpointProber.GetFailureCount(host));
        }

        [Test]
        public void Decay_RecordAfterDecay_RestartsClock()
        {
            const string host = "decay-restart";
            RpcEndpointProber.RecordFailure(host);
            RpcEndpointProber.RewindFailureClockForTests(host, TimeSpan.FromSeconds(60));
            // count = 0.9 after one interval

            RpcEndpointProber.RecordFailure(host);
            // 0.9 + 1.0 = 1.9
            Assert.AreEqual(1.9d, RpcEndpointProber.GetFailureCount(host), 1e-9);
        }

        [Test]
        public void Decay_WithinInterval_NoChange()
        {
            const string host = "decay-noop";
            RpcEndpointProber.RecordFailure(host);
            RpcEndpointProber.RewindFailureClockForTests(host, TimeSpan.FromSeconds(30));

            Assert.AreEqual(1d, RpcEndpointProber.GetFailureCount(host), 1e-9);
        }
    }
}
