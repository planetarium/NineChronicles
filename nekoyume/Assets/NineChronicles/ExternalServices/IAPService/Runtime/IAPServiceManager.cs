#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Libplanet;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using NineChronicles.ExternalServices.IAPService.Runtime.Responses;
using UnityEngine;

namespace NineChronicles.ExternalServices.IAPService.Runtime
{
    public class IAPServiceManager : IDisposable
    {
        public static readonly TimeSpan DefaultProductsCacheLifetime =
            TimeSpan.FromMinutes(10);

        private readonly IAPServiceClient _client;
        private readonly IAPServicePoller _poller;
        private readonly IAPServiceCache _cache;

        public bool IsInitialized { get; private set; }
        public bool IsDisposed { get; private set; }

        public IAPServiceManager(string url)
        {
            _client = new IAPServiceClient(url);
            _poller = new IAPServicePoller(_client);
            _poller.OnPoll += OnPoll;
            _cache = new IAPServiceCache(DefaultProductsCacheLifetime);
        }

        public async Task InitializeAsync(TimeSpan? productsCacheLifetime = null)
        {
            if (IsInitialized)
            {
                Debug.LogError("IAPServiceManager is already initialized.");
                return;
            }

            var (code, error, _, _) = await _client.PingAsync();
            if (code != HttpStatusCode.OK ||
                !string.IsNullOrEmpty(error))
            {
                Debug.LogError(
                    $"Failed to initialize IAPServiceManager: {code}, {error}");
                return;
            }

            if (productsCacheLifetime is not null)
            {
                _cache.SetOptions(productsCacheLifetime);
            }

            IsInitialized = true;
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                Debug.LogError("IAPServiceManager is already disposed.");
                return;
            }

            _client.Dispose();
            _poller.OnPoll -= OnPoll;
            _poller.Clear();
            IsDisposed = true;
        }

        // public async Task CheckReceiptsAsync()
        // {
        //     if (Application.platform == RuntimePlatform.Android)
        //     {
        //     }
        //     // get all receipts from stores.
        //
        //     // get all receipts from IAPService.
        //
        //     //request to purchase to IAPService if there are missing receipts.
        // }

        public async Task<IReadOnlyList<ProductSchema>?> GetProductsAsync(
            Address agentAddr,
            bool force = false)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("IAPServiceManager is not initialized.");
                return null;
            }

            if (!force && _cache.Products is not null)
            {
                return _cache.Products;
            }

            var (code, error, mediaType, content) = await _client.ProductAsync(agentAddr);
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
                _cache.Products = JsonSerializer.Deserialize<ProductSchema[]>(
                    content!,
                    IAPServiceClient.JsonSerializerOptions)!;
                return _cache.Products;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        public async Task<PurchaseResponse200?> PurchaseAsync(
            string receipt,
            Address agentAddr,
            Address inventoryAddr)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("IAPServiceManager is not initialized.");
                return null;
            }

            var (code, error, mediaType, content) =
                await _client.PurchaseAsync(receipt, agentAddr, inventoryAddr);
            if (code != HttpStatusCode.OK ||
                !string.IsNullOrEmpty(error))
            {
                Debug.LogError(
                    $"Purchase failed: {code}, {error}, {mediaType}, {content}");
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
                var result = JsonSerializer.Deserialize<PurchaseProcessResultSchema>(
                    content!,
                    IAPServiceClient.JsonSerializerOptions)!;
                _cache.PurchaseProcessResults[result.Uuid] = result;
                if (result.Status != ReceiptStatus.Valid)
                {
                    Poll(result.Uuid);
                }

                return new PurchaseResponse200 { Content = result };
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        private void Poll(string uuid)
        {
            _poller.Register(uuid);
        }

        private void OnPoll(PurchaseProcessResultSchema[] results)
        {
            // FIXME:
            // Actually, we should check the result of the tx.
            // ReceiptStatus.Valid just means that the receipt is valid.
            foreach (var result in results)
            {
                if (result.Status != ReceiptStatus.Valid)
                {
                    return;
                }

                _poller.Unregister(result.Uuid);
                _cache.PurchaseProcessResults[result.Uuid] = result;
            }
        }
    }
}
