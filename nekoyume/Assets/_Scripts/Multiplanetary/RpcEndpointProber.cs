#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Nekoyume.Multiplanetary
{
    /// <summary>
    /// Picks healthy RPC endpoints from a candidate list (issue #7260).
    /// Replaces random selection in <see cref="PlanetSelector"/> and
    /// <see cref="Nekoyume.Blockchain.RPCAgent"/>.
    /// </summary>
    public static class RpcEndpointProber
    {
        private const int StaleTipThreshold = 30;
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
            public bool PreloadEnded { get; }
            public long Tip { get; }
            public int LatencyMs { get; }

            public ProbeResult(Uri uri, bool healthy, bool preloadEnded, long tip, int latencyMs)
            {
                Uri = uri;
                Healthy = healthy;
                PreloadEnded = preloadEnded;
                Tip = tip;
                LatencyMs = latencyMs;
            }
        }

        /// <summary>
        /// Probes each candidate via the headless GraphQL <c>nodeStatus</c> endpoint and
        /// returns the highest-scoring healthy URI. Returns <c>null</c> if every probe fails
        /// so the caller can fall back to a random pick.
        /// </summary>
        public static async UniTask<Uri?> PickBestRpcAsync(IReadOnlyList<Uri> uris, int timeoutMs)
        {
            if (uris == null || uris.Count == 0)
            {
                return null;
            }

            if (uris.Count == 1)
            {
                return uris[0];
            }

            var probes = uris.Select(uri => ProbeAsync(uri, timeoutMs)).ToArray();
            var results = await UniTask.WhenAll(probes);

            var maxTip = results
                .Where(r => r.Healthy)
                .Select(r => r.Tip)
                .DefaultIfEmpty(0L)
                .Max();

            return results
                .Where(r => r.Healthy && r.PreloadEnded && (maxTip - r.Tip) <= StaleTipThreshold)
                .Select(r => (Uri: r.Uri, Score: Score(r, maxTip)))
                .OrderByDescending(t => t.Score)
                .Select(t => (Uri?)t.Uri)
                .FirstOrDefault();
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
            // Headless exposes GraphQL on https at the same hostname as the gRPC URI.
            var probeUrl = $"https://{uri.Host}/graphql";
            const string body = "{\"query\":\"{nodeStatus{tip{index} preloadEnded}}\"}";

            using var cts = new CancellationTokenSource(timeoutMs);
            using var http = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(timeoutMs),
            };

            var sw = Stopwatch.StartNew();
            try
            {
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                using var response = await http.PostAsync(probeUrl, content, cts.Token);
                sw.Stop();

                if (!response.IsSuccessStatusCode)
                {
                    RecordFailure(uri.Host);
                    return new ProbeResult(uri, false, false, 0, (int)sw.ElapsedMilliseconds);
                }

                var text = await response.Content.ReadAsStringAsync();
                if (TryParseProbe(text, out var tip, out var preloadEnded))
                {
                    return new ProbeResult(uri, true, preloadEnded, tip, (int)sw.ElapsedMilliseconds);
                }

                RecordFailure(uri.Host);
                return new ProbeResult(uri, false, false, 0, (int)sw.ElapsedMilliseconds);
            }
            catch (Exception)
            {
                sw.Stop();
                RecordFailure(uri.Host);
                return new ProbeResult(uri, false, false, 0, (int)sw.ElapsedMilliseconds);
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

        private static bool TryParseProbe(string body, out long tip, out bool preloadEnded)
        {
            tip = 0;
            // Default to true so headless versions that don't expose preloadEnded still pass.
            preloadEnded = true;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (!doc.RootElement.TryGetProperty("data", out var data) ||
                    !data.TryGetProperty("nodeStatus", out var nodeStatus) ||
                    nodeStatus.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                if (nodeStatus.TryGetProperty("tip", out var tipEl) &&
                    tipEl.ValueKind == JsonValueKind.Object &&
                    tipEl.TryGetProperty("index", out var indexEl) &&
                    indexEl.TryGetInt64(out var tipIndex))
                {
                    tip = tipIndex;
                }

                if (nodeStatus.TryGetProperty("preloadEnded", out var pe) &&
                    (pe.ValueKind == JsonValueKind.True || pe.ValueKind == JsonValueKind.False))
                {
                    preloadEnded = pe.GetBoolean();
                }

                return tip > 0;
            }
            catch
            {
                return false;
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
