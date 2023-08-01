#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Libplanet.Crypto;
using NineChronicles.ExternalServices.ArenaService.Runtime.Models;
using UnityEngine;

namespace NineChronicles.ExternalServices.ArenaService.Runtime
{
    public class ArenaServiceManager : IDisposable
    {
        public static readonly TimeSpan DefaultProductsCacheLifetime =
            TimeSpan.FromMinutes(10);

        private readonly ArenaServiceClient _client;

        public bool IsInitialized { get; private set; }
        public bool IsDisposed { get; private set; }

        public ArenaServiceManager(string url)
        {
            _client = new ArenaServiceClient(url);
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                Debug.LogError("ArenaServiceManager is already initialized.");
                return;
            }

            var (code, error, _, _) = await _client.PingAsync();
            if (code != HttpStatusCode.OK ||
                !string.IsNullOrEmpty(error))
            {
                Debug.LogError(
                    $"Failed to initialize ArenaServiceManager: {code}, {error}");
                return;
            }

            IsInitialized = true;
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                Debug.LogError("ArenaServiceManager is already disposed.");
                return;
            }

            _client.Dispose();
            IsDisposed = true;
        }

        public async Task<ArenaInfoSchema?> GetDummyArenaInfoAsync(Address agentAddr, int championshipId, int round)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("ArenaServiceManager is not initialized.");
                return null;
            }
            var (code, error, mediaType, content) = await _client.DummyArenaInfoAsync(championshipId, round, agentAddr);
            if (code != HttpStatusCode.OK ||
                !string.IsNullOrEmpty(error))
            {
                Debug.LogError(
                    $"FetchProducts failed: {code}, {error}, {mediaType}, {content}");
                return null;
            }

            if (mediaType != "application/json")
            {
                Debug.LogError(
                    $"Unexpected media type: {code}, {error}, {mediaType}, {content}");
                return null;
            }

            if (string.IsNullOrEmpty(content))
            {
                Debug.LogError(
                    $"Content is empty: {code}, {error}, {mediaType}, {content}");
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<ArenaInfoSchema>(
                    content!,
                    ArenaServiceClient.JsonSerializerOptions)!;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        public async Task<IReadOnlyList<ArenaBoardDataSchema>?> GetDummyArenaBoadDatasAsync(int championship, int arenaRound, Address agentAddr)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("ArenaServiceManager is not initialized.");
                return null;
            }
            var (code, error, mediaType, content) = await _client.DummyArenaBoadDataAsync(championship, championship, agentAddr);
            if (code != HttpStatusCode.OK ||
                !string.IsNullOrEmpty(error))
            {
                Debug.LogError(
                    $"FetchProducts failed: {code}, {error}, {mediaType}, {content}");
                return null;
            }

            if (mediaType != "application/json")
            {
                Debug.LogError(
                    $"Unexpected media type: {code}, {error}, {mediaType}, {content}");
                return null;
            }

            if (string.IsNullOrEmpty(content))
            {
                Debug.LogError(
                    $"Content is empty: {code}, {error}, {mediaType}, {content}");
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<ArenaBoardDataSchema[]>(
                    content!,
                    ArenaServiceClient.JsonSerializerOptions)!;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }
    }
}
