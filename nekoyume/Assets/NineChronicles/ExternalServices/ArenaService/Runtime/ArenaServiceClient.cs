#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Libplanet;
using Libplanet.Crypto;
using NineChronicles.ExternalServices.ArenaService.Runtime.Models;
using Debug = UnityEngine.Debug;

namespace NineChronicles.ExternalServices.ArenaService.Runtime
{
    public class ArenaServiceClient : IDisposable
    {
        internal static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            WriteIndented = false,
        };

        private readonly ArenaServiceEndpoint _endpoints;
        private readonly HttpClient _client;

        public ArenaServiceClient(string url)
        {
            _endpoints = new ArenaServiceEndpoint(url);
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(10);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async
            Task<(HttpStatusCode code, string? error, string? mediaType, string? content)>
            PingAsync()
        {
            using var res = await _client.GetAsync(_endpoints.Ping);
            return await ProcessResponseAsync(res);
        }

        public async
            Task<(HttpStatusCode code, string? error, string? mediaType, string? content)>
            ArenaInfoAsync(int blockIndex,int championship, int arenaRound)
        {
            var uriBuilder = new UriBuilder(_endpoints.ArenaInfo);
            uriBuilder.Query = $"block_index={blockIndex}&championship={championship}&round={arenaRound}";
            using var res = await _client.GetAsync(uriBuilder.Uri);
            return await ProcessResponseAsync(res);
        }

        public async
            Task<(HttpStatusCode code, string? error, string? mediaType, string? content)>
            ArenaParticipantListAsync(int championship, int arenaRound, Address avatarAddr)
        {
            var uriBuilder = new UriBuilder(_endpoints.ArenaParticipantList);
            uriBuilder.Query = $"championship={championship}&round={arenaRound}&avatar_addr={avatarAddr.ToString()}";
            using var res = await _client.GetAsync(uriBuilder.Uri);
            return await ProcessResponseAsync(res);
        }

        public async
            Task<(HttpStatusCode code, string? error, string? mediaType, string? content)>
            DummyArenaInfoAsync(int championship, int arenaRound, Address avatarAddr)
        {
            var uriBuilder = new UriBuilder(_endpoints.DummyArenaMy);
            uriBuilder.Query = $"championship={championship}&arena_round={arenaRound}&avatar_addr={avatarAddr.ToString()}";
            using var res = await _client.GetAsync(uriBuilder.Uri);
            return await ProcessResponseAsync(res);
        }

        public async
            Task<(HttpStatusCode code, string? error, string? mediaType, string? content)>
            DummyArenaBoadDataAsync(int championship, int arenaRound, Address avatarAddr)
        {
            var uriBuilder = new UriBuilder(_endpoints.DummyArenaBoard);
            uriBuilder.Query= $"championship={championship}&arena_round={arenaRound}&avatar_addr={avatarAddr.ToString()}";
            using var res = await _client.GetAsync(uriBuilder.Uri);
            return await ProcessResponseAsync(res);
        }

        private static async
            Task<(HttpStatusCode code, string? error, string? mediaType, string? content)>
            ProcessResponseAsync(HttpResponseMessage res)
        {
            try
            {
                res.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                var msg = $"{e.Message}\n{e.StackTrace}";
                return (res.StatusCode, msg, null, null);
            }

            var resContentType = res.Content.Headers.ContentType.MediaType;
            var resContent = await res.Content.ReadAsStringAsync();
            return (res.StatusCode, "", resContentType, resContent);
        }
    }
}
