#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Libplanet;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;
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
            PurchaseRequestAsync(
                Store store,
                string receipt,
                Address agentAddr,
                Address avatarAddr)
        {
            var receiptJson = JsonNode.Parse(receipt);
            var reqJson = new JsonObject
            {
                { "store", (int)store },
                { "data", receiptJson },
                { "agentAddress", agentAddr.ToHex() },
                { "avatarAddress", avatarAddr.ToHex() }
            };
            var reqContent = new StringContent(
                reqJson.ToJsonString(JsonSerializerOptions),
                System.Text.Encoding.UTF8,
                "application/json");
            using var res = await _client.PostAsync(_endpoints.PurchaseRequest, reqContent);
            return await ProcessResponseAsync(res);
        }

        public async
            Task<(HttpStatusCode code, string? error, string? mediaType, string? content)>
            PurchaseStatusAsync(HashSet<string> uuids)
        {
            var jsonArray = new JsonArray();
            foreach (var uuid in uuids)
            {
                jsonArray.Add(uuid);
            }

            var reqContent = new StringContent(
                jsonArray.ToJsonString(),
                System.Text.Encoding.UTF8,
                "application/json");
            var reqMsg = new HttpRequestMessage(
                HttpMethod.Get,
                _endpoints.PurchaseStatus)
            {
                Content = reqContent,
            };
            using var res = await _client.SendAsync(reqMsg);
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
