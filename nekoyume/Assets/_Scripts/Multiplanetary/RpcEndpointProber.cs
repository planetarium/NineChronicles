using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bencodex;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet.Types.Blocks;
using MagicOnion;
using MagicOnion.Client;
using MagicOnion.Unity;
using Nekoyume.Shared.Services;

namespace Nekoyume.Multiplanetary
{
    /// <summary>
    /// Picks healthy RPC endpoints from a candidate list (issue #7260).
    /// Probes the gRPC <c>GetTip</c> unary on the same channel gameplay uses, so
    /// hostname/port assumptions about a sibling GraphQL endpoint don't apply.
    /// </summary>
    public static class RpcEndpointProber
    {
        public const int StaleTipThreshold = 30;
        private const int BaseScore = 1000;
        private const int FailurePenalty = 100;
        private const int FreshnessPenaltyPerBlock = 10;
        private const double FailureDecayFactor = 0.9;
        private static readonly TimeSpan FailureDecayInterval = TimeSpan.FromSeconds(60);

        private static readonly ConcurrentDictionary<string, FailureRecord> FailureCounts = new();

        private sealed class FailureRecord
        {
            public double Count;
            public DateTime LastUpdated = DateTime.UtcNow;
        }

        public readonly struct ProbeResult
        {
            public Uri Uri { get; }
            public bool Healthy { get; }
            public long Tip { get; }
            public int LatencyMs { get; }

            public ProbeResult(Uri uri, bool healthy, long tip, int latencyMs)
            {
                Uri = uri;
                Healthy = healthy;
                Tip = tip;
                LatencyMs = latencyMs;
            }
        }

        public readonly struct ProbeReport
        {
            /// <summary>Picked URI, or <c>null</c> if no candidate passed the filters.</summary>
            public Uri Pick { get; }

            /// <summary>Raw probe results for every candidate (empty when probing was skipped).</summary>
            public IReadOnlyList<ProbeResult> Results { get; }

            /// <summary>Maximum tip observed across healthy probes (0 if none healthy).</summary>
            public long MaxTip { get; }

            public ProbeReport(Uri pick, IReadOnlyList<ProbeResult> results, long maxTip)
            {
                Pick = pick;
                Results = results;
                MaxTip = maxTip;
            }
        }

        /// <summary>
        /// Probes each candidate via gRPC <c>GetTip</c> and returns a <see cref="ProbeReport"/>
        /// containing the picked URI plus every raw result. <see cref="ProbeReport.Pick"/> is
        /// <c>null</c> if no candidate passed the filters so the caller can fall back to a random pick.
        /// </summary>
        public static async UniTask<ProbeReport> PickBestRpcAsync(IReadOnlyList<Uri> uris, int timeoutMs)
        {
            if (uris == null || uris.Count == 0)
            {
                return new ProbeReport(null, Array.Empty<ProbeResult>(), 0L);
            }

            if (uris.Count == 1)
            {
                return new ProbeReport(uris[0], Array.Empty<ProbeResult>(), 0L);
            }

            var probes = uris.Select(uri => ProbeAsync(uri, timeoutMs)).ToArray();
            var results = await UniTask.WhenAll(probes);

            var maxTip = results
                .Where(r => r.Healthy)
                .Select(r => r.Tip)
                .DefaultIfEmpty(0L)
                .Max();

            var pick = results
                .Where(r => r.Healthy && (maxTip - r.Tip) <= StaleTipThreshold)
                .Select(r => (Uri: r.Uri, Score: Score(r, maxTip)))
                .OrderByDescending(t => t.Score)
                .Select(t => t.Uri)
                .FirstOrDefault();

            return new ProbeReport(pick, results, maxTip);
        }

        public static int Score(in ProbeResult r, long maxTip)
        {
            var freshnessPenalty = (int)(Math.Max(0L, maxTip - r.Tip) * FreshnessPenaltyPerBlock);
            var failurePenalty = (int)(GetFailureCount(r.Uri.Host) * FailurePenalty);
            return BaseScore - r.LatencyMs - freshnessPenalty - failurePenalty;
        }

        /// <summary>
        /// Score for rotation paths that have no fresh probe data — relies on accumulated
        /// failure history. Lower failure count wins.
        /// </summary>
        public static int ScoreHostByHistory(string host)
        {
            return BaseScore - (int)(GetFailureCount(host) * FailurePenalty);
        }

        public static async UniTask<ProbeResult> ProbeAsync(Uri uri, int timeoutMs)
        {
            GrpcChannelx channel = null;
            var sw = Stopwatch.StartNew();
            try
            {
                channel = GrpcChannelx.ForTarget(new GrpcChannelTarget(uri.Host, uri.Port, true));
                var svc = MagicOnionClient.Create<IBlockChainService>(channel);

                var tipTask = svc.GetTip().ResponseAsync;
                using var delayCts = new CancellationTokenSource();
                var delayTask = Task.Delay(timeoutMs, delayCts.Token);
                var winner = await Task.WhenAny(tipTask, delayTask);
                sw.Stop();

                if (winner != tipTask)
                {
                    // Observe any later fault on the abandoned tipTask so it doesn't
                    // surface as an unobserved exception.
                    _ = tipTask.ContinueWith(_ => { }, TaskScheduler.Default);
                    RecordFailure(uri.Host);
                    return new ProbeResult(uri, false, 0, (int)sw.ElapsedMilliseconds);
                }

                delayCts.Cancel();
                var tipBytes = await tipTask;
                var tip = DecodeTipIndex(tipBytes);
                if (tip <= 0)
                {
                    RecordFailure(uri.Host);
                    return new ProbeResult(uri, false, 0, (int)sw.ElapsedMilliseconds);
                }
                return new ProbeResult(uri, true, tip, (int)sw.ElapsedMilliseconds);
            }
            catch (Exception)
            {
                sw.Stop();
                RecordFailure(uri.Host);
                return new ProbeResult(uri, false, 0, (int)sw.ElapsedMilliseconds);
            }
            finally
            {
                if (channel != null)
                {
                    try
                    {
                        await channel.ShutdownAsync();
                    }
                    catch
                    {
                        // Shutdown failure shouldn't taint the probe result.
                    }
                }
            }
        }

        public static void RecordFailure(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                return;
            }

            var record = FailureCounts.GetOrAdd(host, _ => new FailureRecord());
            lock (record)
            {
                Decay(record);
                record.Count += 1d;
            }
        }

        public static double GetFailureCount(string host)
        {
            if (string.IsNullOrEmpty(host) || !FailureCounts.TryGetValue(host, out var record))
            {
                return 0d;
            }

            lock (record)
            {
                Decay(record);
                return record.Count;
            }
        }

        private static long DecodeTipIndex(byte[] bytes)
        {
            try
            {
                var codec = new Codec();
                var dict = (Dictionary)codec.Decode(bytes);
                var block = BlockMarshaler.UnmarshalBlock(dict);
                return block.Index;
            }
            catch
            {
                return 0;
            }
        }

        internal static void ResetFailuresForTests()
        {
            FailureCounts.Clear();
        }

        internal static void RewindFailureClockForTests(string host, TimeSpan delta)
        {
            if (string.IsNullOrEmpty(host) || !FailureCounts.TryGetValue(host, out var record))
            {
                return;
            }

            lock (record)
            {
                record.LastUpdated -= delta;
            }
        }

        private static void Decay(FailureRecord record)
        {
            var now = DateTime.UtcNow;
            var elapsed = now - record.LastUpdated;
            if (elapsed < FailureDecayInterval)
            {
                return;
            }

            var intervals = (int)(elapsed.Ticks / FailureDecayInterval.Ticks);
            record.Count *= Math.Pow(FailureDecayFactor, intervals);
            if (record.Count < 0.01d)
            {
                record.Count = 0d;
            }

            record.LastUpdated = now;
        }
    }
}
