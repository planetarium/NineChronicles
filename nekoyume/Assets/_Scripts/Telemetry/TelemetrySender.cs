using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Libplanet.Crypto;
using UnityEngine;

namespace Nekoyume.Telemetry
{
    /// <summary>
    /// Collects tracked failure events and sends them to the telemetry endpoint in signed batches.
    /// Telemetry must never block or break the game: every failure here is swallowed.
    /// </summary>
    public class TelemetrySender
    {
        public const int MaxBatchEvents = 500;
        public const long MaxPersistedBytes = 200 * 1024;
        public const int MaxErrorLength = 200;
        public static readonly TimeSpan SizeFlushMinInterval = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(60);

        private readonly Func<string, string, Task<(int status, DateTimeOffset? date)>> _post;
        private readonly TelemetryQueue _queue;
        private readonly TelemetryClock _clock;
        private readonly Func<PrivateKey> _keyProvider;
        private readonly Func<(string planet, string platform, string version, string agentAddress)> _context;
        private readonly string _persistPath;
        private readonly System.Random _rng = new System.Random();
        private readonly object _fileLock = new object();

        private int _failures;
        private int _flushing;
        private long _lastSizeFlushTimestamp = long.MinValue;

        /// <param name="post">
        /// Sends one envelope. Arguments are (batchId, envelope JSON); returns the HTTP status
        /// (0 for "no response") and the response <c>Date</c> header. May throw on network errors.
        /// </param>
        /// <param name="persistPath">Where the queue is saved on pause/quit. Null disables persistence.</param>
        public TelemetrySender(
            Func<string, string, Task<(int status, DateTimeOffset? date)>> post,
            TelemetryQueue queue,
            TelemetryClock clock,
            Func<PrivateKey> keyProvider,
            Func<(string planet, string platform, string version, string agentAddress)> context,
            string persistPath = null)
        {
            _post = post ?? throw new ArgumentNullException(nameof(post));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _keyProvider = keyProvider;
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _persistPath = persistPath;
        }

        /// <summary>Consecutive retryable failures (429, 5xx, network error).</summary>
        public int Failures => Volatile.Read(ref _failures);

        /// <summary>How long the caller's timer should wait before the next <see cref="FlushAsync"/>.</summary>
        public TimeSpan NextFlushDelay
        {
            get
            {
                var failures = Failures;
                if (failures <= 0)
                {
                    return FlushInterval;
                }

                lock (_rng)
                {
                    return TelemetryBackoff.NextDelay(failures, _rng);
                }
            }
        }

        public void Enqueue(
            string name,
            IDictionary<string, string> props,
            string rpc,
            string avatarAddress)
        {
            if (name == null || !TelemetryEvents.Tracked.Contains(name))
            {
                return;
            }

            _queue.Enqueue(new TelemetryEvent
            {
                Name = name,
                Ts = _clock.Now(),
                Rpc = HostOnly(rpc),
                AvatarAddress = avatarAddress,
                Props = TruncateError(TelemetryEvents.FilterProps(name, NormalizeProps(props))),
            });

            if (_queue.Count >= MaxBatchEvents)
            {
                TryStartSizeFlush();
            }
        }

        /// <summary>
        /// Starts a background flush when a full batch is waiting, without waiting for the timer.
        /// Skipped while a flush is running, while backing off after failures, and at most once per
        /// <see cref="SizeFlushMinInterval"/> (a full queue of signed events before login must not
        /// spin a flush on every enqueue).
        /// </summary>
        private void TryStartSizeFlush()
        {
            if (Volatile.Read(ref _flushing) != 0 || Failures > 0)
            {
                return;
            }

            var now = System.Diagnostics.Stopwatch.GetTimestamp();
            var last = Interlocked.Read(ref _lastSizeFlushTimestamp);
            var minTicks = (long)(SizeFlushMinInterval.TotalSeconds * System.Diagnostics.Stopwatch.Frequency);
            if (last != long.MinValue && now - last < minTicks)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _lastSizeFlushTimestamp, now, last) != last)
            {
                return;
            }

            _ = Task.Run(() => FlushAsync());
        }

        private static Dictionary<string, string> TruncateError(Dictionary<string, string> props)
        {
            if (props == null || !props.TryGetValue("error", out var error) || error.Length <= MaxErrorLength)
            {
                return props;
            }

            var length = MaxErrorLength;
            if (char.IsHighSurrogate(error[length - 1]))
            {
                length--;
            }

            props["error"] = error.Substring(0, length);
            return props;
        }

        /// <summary>
        /// Sends at most one batch. The key and context are read synchronously before the first
        /// await, so callers on the main thread may read Unity state inside those delegates.
        /// </summary>
        public async Task FlushAsync()
        {
            if (Interlocked.CompareExchange(ref _flushing, 1, 0) != 0)
            {
                return;
            }

            try
            {
                await FlushOnceAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // Never surface the envelope or key material; the type is enough.
                Debug.LogWarning($"[Telemetry] flush failed: {e.GetType().Name}");
            }
            finally
            {
                Interlocked.Exchange(ref _flushing, 0);
            }
        }

        /// <summary>
        /// Saves the queue to disk first (synchronously, so it survives the process being killed),
        /// then makes one send attempt and re-saves what is left.
        /// </summary>
        public async Task OnPauseOrQuitAsync()
        {
            Persist();
            await FlushAsync().ConfigureAwait(false);
            Persist();
        }

        /// <summary>Restores a queue saved by <see cref="OnPauseOrQuitAsync"/>. Call once at startup.</summary>
        public void LoadPersisted()
        {
            if (string.IsNullOrEmpty(_persistPath))
            {
                return;
            }

            try
            {
                lock (_fileLock)
                {
                    var info = new FileInfo(_persistPath);
                    if (!info.Exists)
                    {
                        return;
                    }

                    if (info.Length <= MaxPersistedBytes)
                    {
                        _queue.FromJson(File.ReadAllText(_persistPath));
                    }

                    // Consume the file so already-sent events are not restored again after a crash.
                    File.Delete(_persistPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Telemetry] load persisted queue failed: {e.GetType().Name}");
            }
        }

        public static string HostOnly(string rpc)
        {
            if (string.IsNullOrWhiteSpace(rpc))
            {
                return null;
            }

            var value = rpc.Trim();
            if (value.Contains("://") && Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                return uri.Host;
            }

            var slash = value.IndexOf('/');
            if (slash >= 0)
            {
                value = value.Substring(0, slash);
            }

            var colon = value.LastIndexOf(':');
            if (colon > 0 && value.IndexOf(':') == colon)
            {
                value = value.Substring(0, colon);
            }

            return value.Length == 0 ? null : value;
        }

        public static string PlatformName(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.Android:
                    return "android";
                case RuntimePlatform.IPhonePlayer:
                    return "ios";
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "windows";
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "macos";
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    return "linux";
                default:
                    return platform.ToString().ToLowerInvariant();
            }
        }

        private static IDictionary<string, string> NormalizeProps(IDictionary<string, string> props)
        {
            if (props == null)
            {
                return null;
            }

            var result = new Dictionary<string, string>();
            foreach (var kv in props)
            {
                if (kv.Key == null || kv.Value == null)
                {
                    continue;
                }

                // Existing Analyzer call sites name the product id "product-id".
                var key = kv.Key == "product-id" ? "productId" : kv.Key;
                if (!result.ContainsKey(key) || kv.Key == key)
                {
                    result[key] = kv.Value;
                }
            }

            return result;
        }

        private async Task FlushOnceAsync()
        {
            PrivateKey key = null;
            try
            {
                key = _keyProvider?.Invoke();
            }
            catch
            {
                // Key not available yet (before login); fall through as unsigned.
            }

            var ctx = _context();

            List<TelemetryEvent> batch;
            if (key is not null)
            {
                batch = _queue.TakeBatch(MaxBatchEvents);
            }
            else
            {
                // Unsigned batches may only carry the events the server accepts without a signature.
                var all = _queue.TakeBatch(int.MaxValue);
                batch = new List<TelemetryEvent>();
                var keep = new List<TelemetryEvent>();
                foreach (var e in all)
                {
                    if (batch.Count < MaxBatchEvents && TelemetryEvents.Unsigned.Contains(e.Name))
                    {
                        batch.Add(e);
                    }
                    else
                    {
                        keep.Add(e);
                    }
                }

                if (keep.Count > 0)
                {
                    _queue.Requeue(keep);
                }
            }

            if (batch.Count == 0)
            {
                return;
            }

            var batchId = Guid.NewGuid().ToString("N");
            var agentAddress = key is null
                ? null
                : "0x" + key.Address.ToHex().ToLowerInvariant();
            var payload = TelemetryPayload.Serialize(
                batchId,
                _clock.Now(),
                agentAddress,
                ctx.planet,
                ctx.platform,
                ctx.version,
                batch);

            string envelope;
            if (key is null)
            {
                envelope = TelemetrySigner.Envelope(payload, null, null);
            }
            else
            {
                var (publicKeyHex, signatureHex) = TelemetrySigner.Sign(key, payload);
                envelope = TelemetrySigner.Envelope(payload, publicKeyHex, signatureHex);
            }

            int status;
            DateTimeOffset? date;
            try
            {
                (status, date) = await _post(batchId, envelope).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _queue.Requeue(batch);
                Interlocked.Increment(ref _failures);
                Debug.LogWarning($"[Telemetry] send failed: {e.GetType().Name}");
                return;
            }

            if (date.HasValue)
            {
                _clock.Observe(date.Value);
            }

            if (status >= 200 && status < 300)
            {
                Interlocked.Exchange(ref _failures, 0);
                return;
            }

            if (status == 0 || status == 429 || status >= 500)
            {
                _queue.Requeue(batch);
                Interlocked.Increment(ref _failures);
                return;
            }

            // 400 (format/stale/signature/unsigned_event), 409 duplicate, 413 and any other 4xx:
            // retrying the same batch can never succeed, so drop it.
            Interlocked.Exchange(ref _failures, 0);
            Debug.LogWarning($"[Telemetry] batch {batchId} rejected with {status}; dropped {batch.Count} events");
        }

        private void Persist()
        {
            if (string.IsNullOrEmpty(_persistPath))
            {
                return;
            }

            try
            {
                lock (_fileLock)
                {
                    if (_queue.Count == 0)
                    {
                        if (File.Exists(_persistPath))
                        {
                            File.Delete(_persistPath);
                        }

                        return;
                    }

                    var tmp = _persistPath + ".tmp";
                    File.WriteAllText(tmp, _queue.ToJson());
                    if (File.Exists(_persistPath))
                    {
                        File.Delete(_persistPath);
                    }

                    File.Move(tmp, _persistPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Telemetry] persist queue failed: {e.GetType().Name}");
            }
        }
    }

    /// <summary>
    /// The real transport for <see cref="TelemetrySender"/>. Run it off the main thread
    /// (e.g. inside <c>Task.Run</c>): DNS and gzip may block the calling thread.
    /// </summary>
    public static class TelemetryHttp
    {
        public const int GzipThresholdBytes = 4 * 1024;
        public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

        public static HttpClient CreateClient(HttpMessageHandler handler = null)
        {
            return new HttpClient(handler ?? new HttpClientHandler(), true)
            {
                Timeout = RequestTimeout,
            };
        }

        /// <param name="compress">Gzip implementation; tests replace it. If it throws, the body is sent uncompressed.</param>
        public static async Task<(int status, DateTimeOffset? date)> PostAsync(
            HttpClient client,
            string endpoint,
            string body,
            Func<byte[], byte[]> compress = null)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            var gzip = false;
            if (bytes.Length > GzipThresholdBytes)
            {
                try
                {
                    bytes = (compress ?? Gzip)(bytes);
                    gzip = true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Telemetry] gzip failed, sending uncompressed: {e.GetType().Name}");
                }
            }

            using (var cts = new CancellationTokenSource(RequestTimeout))
            using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                var content = new ByteArrayContent(bytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                if (gzip)
                {
                    content.Headers.ContentEncoding.Add("gzip");
                }

                request.Content = content;
                using (var response = await client
                           .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                           .ConfigureAwait(false))
                {
                    return ((int)response.StatusCode, response.Headers.Date);
                }
            }
        }

        public static byte[] Gzip(byte[] bytes)
        {
            using (var buffer = new MemoryStream())
            {
                using (var gz = new GZipStream(buffer, System.IO.Compression.CompressionLevel.Fastest, true))
                {
                    gz.Write(bytes, 0, bytes.Length);
                }

                return buffer.ToArray();
            }
        }
    }
}
