#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Libplanet.Crypto;
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
            ProductAsync(Address agentAddr, string planetId)
        {
            var uriBuilder = new UriBuilder(_endpoints.Product);
            uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query)
                ? $"agent_addr={agentAddr.ToString()}&planet_id={planetId}"
                : uriBuilder.Query[1..] + $"&agent_addr={agentAddr.ToString()}&planet_id={planetId}";
            using var res = await _client.GetAsync(uriBuilder.Uri);
            return await ProcessResponseAsync(res);
        }

        public async
            Task<(HttpStatusCode code, string? error, string? mediaType, string? content)>
            PurchaseRequestAsync(
                Store store,
                string receipt,
                string agentAddr,
                string avatarAddr,
                string planetId,
                string transactionId,
                string appleOriginalTransactionID)
        {
            var receiptJson = JsonNode.Parse(receipt);
            
            var reqJson = new JsonObject
            {
                { "store", (int)store },
                { "agentAddress", agentAddr },
                { "avatarAddress", avatarAddr},
                { "planetId", planetId},
                { "data", receiptJson },
            };

            Debug.Log($"PurchaseRequestAsync : {reqJson}");

            var reqContent = new StringContent(
                reqJson.ToJsonString(JsonSerializerOptions),
                System.Text.Encoding.UTF8,
                "application/json");

            reqContent.Headers.Add("agentAddress", agentAddr);
            reqContent.Headers.Add("orderId", transactionId);
            if (!string.IsNullOrEmpty(appleOriginalTransactionID))
            {
                reqContent.Headers.Add("appleOriginalTransactionID", appleOriginalTransactionID);
            }

            using var res = await _client.PostAsync(_endpoints.PurchaseRequest, reqContent);
            return await ProcessResponseAsync(res);
        }

        public async
            Task<(HttpStatusCode code, string? error, string? mediaType, string? content)>
            PurchaseStatusAsync(HashSet<string> uuids)
        {
            var sb = new StringBuilder();
            foreach (var uuid in uuids)
            {
                sb.Append($"&uuid={uuid}");
            }

            var query = sb.ToString()[1..];
            var uriBuilder = new UriBuilder(_endpoints.PurchaseStatus);
            uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query)
                ? query
                : uriBuilder.Query[1..] + query;
            using var res = await _client.GetAsync(uriBuilder.Uri);
            return await ProcessResponseAsync(res);
        }

        public async
            Task<(HttpStatusCode code, string? error, string? mediaType, string? content)>
            PurchaseLogAsync(
                string agentAddr,
                string avatarAddr,
                string planetId,
                string productId,
                string orderId)
        {
            Debug.Log($"PurchaseLogAsync");

            var query = $"planet_id={planetId}&agent_address={agentAddr}&avatar_address={avatarAddr}&product_id={productId}&order_id={orderId}";
            var uriBuilder = new UriBuilder(_endpoints.PurchaseStatus);
            uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query)
                ? query
                : uriBuilder.Query[1..] + query;

            using var res = await _client.GetAsync(uriBuilder.Uri);
            return await ProcessResponseAsync(res);
        }

        public async
            Task<(HttpStatusCode code, string? error, string? mediaType, string? content)>
            L10NAsync()
        {
            using var res = await _client.GetAsync(_endpoints.L10N);
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
