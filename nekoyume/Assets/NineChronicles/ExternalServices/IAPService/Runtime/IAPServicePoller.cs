#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using UnityEngine;

namespace NineChronicles.ExternalServices.IAPService.Runtime
{
    public class IAPServicePoller
    {
        private readonly IAPServiceClient _client;
        private CancellationTokenSource? _cts;
        private readonly HashSet<string> _uuids = new();

        public event Action<PurchaseProcessResultSchema[]> OnPoll = delegate { };

        public IAPServicePoller(IAPServiceClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _cts = null;
        }

        public void Register(string uuid)
        {
            _uuids.Add(uuid);
            if (_cts is null)
            {
                Start();
            }
        }

        public void Unregister(string uuid)
        {
            _uuids.Remove(uuid);
            if (_uuids.Count == 0)
            {
                Stop();
            }
        }

        public void Clear()
        {
            _uuids.Clear();
            Stop();
        }

        private void Start()
        {
            Stop();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            Task.Run(() => PollingContinuouslyAsync(token), token);
        }

        private void Stop()
        {
            if (_cts is not null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        private async Task PollingContinuouslyAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            while (!ct.IsCancellationRequested)
            {
                if (_uuids.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
                    continue;
                }

                var (code, error, mediaType, content) = await _client!.PollAsync(_uuids);
                if (code != HttpStatusCode.OK ||
                    !string.IsNullOrEmpty(error))
                {
                    Debug.LogError(
                        $"Poll failed: {code}, {error}, {mediaType}, {content}");
                }

                if (mediaType != "application/json")
                {
                    Debug.LogError(
                        $"Poll failed: {code}, {error}, {mediaType}, {content}");
                }

                if (string.IsNullOrEmpty(content))
                {
                    Debug.LogError(
                        $"Content is empty: {code}, {error}, {mediaType}, {content}");
                    continue;
                }

                try
                {
                    var results = JsonSerializer.Deserialize<PurchaseProcessResultSchema[]>(
                        content!,
                        IAPServiceClient.JsonSerializerOptions);
                    OnPoll.Invoke(results!);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), ct);
            }

            ct.ThrowIfCancellationRequested();
        }
    }
}
