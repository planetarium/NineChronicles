#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Libplanet;
using Debug = UnityEngine.Debug;

namespace NineChronicles.ExternalServices.IAPService.Runtime
{
    public class IAPServiceClient : IDisposable
    {
        internal static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            WriteIndented = false,
        };

        private readonly IAPServiceEndpoints _endpoints;
        private readonly HttpClient _client;
        private int _latencyCount;
        private float _latencyAverage;

        public IAPServiceClient(string url)
        {
            _endpoints = new IAPServiceEndpoints(url);
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(10);
            // _client.DefaultRequestHeaders.Add("User-Agent", "Nekoyume");
            // _client.DefaultRequestHeaders.Add("Accept", "application/json");
            // _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
            // _client.DefaultRequestHeaders.Add("Accept-Language", "en-US");
            // _client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            // _client.DefaultRequestHeaders.Add("Keep-Alive", "timeout=5");
            // _client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            // _client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
            // _client.DefaultRequestHeaders.Add("X-Unity-Version", "2021.3.5f1");
            // _client.DefaultRequestHeaders.Add("X-Unity-Platform", "OSXPlayer");
            // _client.DefaultRequestHeaders.Add("X-Unity-Device-Model", "Mac");
            // _client.DefaultRequestHeaders.Add("X-Unity-Device-Name", "Mac");
            // _client.DefaultRequestHeaders.Add("X-Unity-Device-Unique-Id", "00000000-0000-0000-0000-000000000000");
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
            ProductAsync(Address agentAddr)
        {
            var uriBuilder = new UriBuilder(_endpoints.Product);
            uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query)
                ? $"agent_addr={agentAddr.ToString()}"
                : uriBuilder.Query[1..] + $"&agent_addr={agentAddr.ToHex()}";
            using var res = await _client.GetAsync(uriBuilder.Uri);
            return await ProcessResponseAsync(res);
        }

        public async
            Task<(HttpStatusCode code, string? error, string? mediaType, string? content)>
            PurchaseAsync(
                string receipt,
                Address inventoryAddr)
        {
            var reqJson = new JsonObject
            {
                { "store", 0 },
                { "data", receipt },
                { "inventoryAddress", inventoryAddr.ToHex() }
            };
            var reqContent = new StringContent(
                reqJson.ToJsonString(JsonSerializerOptions),
                System.Text.Encoding.UTF8,
                "application/json");
            using var res = await _client.PostAsync(_endpoints.Purchase, reqContent);
            return await ProcessResponseAsync(res);
        }

        public async
            Task<(HttpStatusCode code, string? error, string? mediaType, string? content)>
            PollAsync(HashSet<string> uuids)
        {
            var reqJson = new JsonObject
            {
                { "uuids", JsonSerializer.Serialize(uuids) }
            };
            var reqContent = new StringContent(
                reqJson.ToJsonString(JsonSerializerOptions),
                System.Text.Encoding.UTF8,
                "application/json");
            using var res = await _client.PostAsync(_endpoints.Poll, reqContent);
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
