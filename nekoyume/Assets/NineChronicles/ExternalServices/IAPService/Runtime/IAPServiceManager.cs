#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Libplanet.Crypto;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using UnityEngine;

namespace NineChronicles.ExternalServices.IAPService.Runtime
{
    public class IAPServiceManager : IDisposable
    {
        public static readonly TimeSpan DefaultProductsCacheLifetime =
            TimeSpan.FromMinutes(10);

        private readonly IAPServiceClient _client;

        // NOTE: Enable this code if you want to use poller.
        // private readonly IAPServicePoller _poller;
        // NOTE: Enable this code if you want to use cache.
        // private readonly IAPServiceCache _cache;
        private readonly Store _store;

        public bool IsInitialized { get; private set; }
        public bool IsDisposed { get; private set; }

        public IAPServiceManager(string url, Store store)
        {
            _client = new IAPServiceClient(url);
            // NOTE: Enable this code if you want to use poller.
            // _poller = new IAPServicePoller(_client);
            // _poller.OnPoll += OnPoll;
            // NOTE: Enable this code if you want to use cache.
            // _cache = new IAPServiceCache(DefaultProductsCacheLifetime);
            _store = store;
        }

        public async Task InitializeAsync()
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

            // NOTE: Enable this code if you want to use cache.
            // if (productsCacheLifetime is not null)
            // {
            //     _cache.SetOptions(productsCacheLifetime);
            // }

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
            // _poller.OnPoll -= OnPoll;
            // _poller.Clear();
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

        public async Task CheckProductAvailable(string productSku, Address agentAddr, string planetId, Action success, Action failed)
        {
            var categoryList = await GetProductsAsync(agentAddr, planetId);
            if(categoryList == null)
            {
                failed();
                return;
            }

            ProductSchema? selectedProduct = null;
            foreach (var category in categoryList)
            {
                foreach (var product in category.ProductList)
                {
                    if (product.Sku == productSku)
                    {
                        selectedProduct = product;
                        if(product.Active && product.Buyable)
                        {
                            success();
                            return;
                        }
                    }
                }
            }

            if (selectedProduct != null)
            {
                Debug.LogError($"CheckProductAvailable Fail {productSku} Active:{selectedProduct.Active} Buyable:{selectedProduct.Buyable}");
            }
            else
            {
                Debug.LogError($"CheckProductAvailable Fail can't find {productSku}");
            }
            failed();

            return;
        }

        public async Task<IReadOnlyList<CategorySchema>?> GetProductsAsync(Address agentAddr, string planetId)
        {
            if (string.IsNullOrEmpty(planetId))
            {
                Debug.LogWarning("planetId is null or empty.");
                return null;
            }

            if (!IsInitialized)
            {
                Debug.LogWarning("IAPServiceManager is not initialized.");
                return null;
            }

            // NOTE: Enable this code if you want to use cache.
            // if (!force && _cache.Products is not null)
            // {
            //     return _cache.Products;
            // }

            var (code, error, mediaType, content) = await _client.ProductAsync(agentAddr, planetId);
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
                return JsonSerializer.Deserialize<CategorySchema[]>(
                    content!,
                    IAPServiceClient.JsonSerializerOptions)!;
                // NOTE: Enable this code if you want to use cache.
                // _cache.Products = JsonSerializer.Deserialize<ProductSchema[]>(
                //     content!,
                //     IAPServiceClient.JsonSerializerOptions)!;
                // return _cache.Products;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        public async Task<ReceiptDetailSchema?> PurchaseRequestAsync(
            string receipt,
            string agentAddr,
            string avatarAddr,
            string planetId,
            string transactionId,
            string appleOriginalTransactionID)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("IAPServiceManager is not initialized.");
                return null;
            }

            var (code, error, mediaType, content) =
                await _client.PurchaseRequestAsync(
                    _store,
                    receipt,
                    agentAddr,
                    avatarAddr,
                    planetId,
                    transactionId,
                    appleOriginalTransactionID);
            if (code != HttpStatusCode.OK ||
                !string.IsNullOrEmpty(error))
            {
                Debug.LogError(
                    $"Purchase failed: {code}, {receipt}, {error}, {mediaType}, {content}");
                return null;
            }

            if (mediaType != "application/json")
            {
                Debug.LogError(
                    $"Unexpected media type: {code}, {receipt}, {error}, {mediaType}, {content}");
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
                var result = JsonSerializer.Deserialize<ReceiptDetailSchema>(
                    content!,
                    IAPServiceClient.JsonSerializerOptions)!;
                // NOTE: Enable this code if you want to use cache.
                // _cache.PurchaseProcessResults[result.Uuid] = result;
                if (result.Status == ReceiptStatus.Invalid ||
                    result.Status == ReceiptStatus.Unknown)
                {
                    UnregisterAndCache(result);
                }
                // NOTE: Enable this code if you want to use poller.
                // else
                // {
                //     _poller.Register(result.Uuid);
                // }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        public async Task<ReceiptDetailSchema?> PurchaseFreeAsync(
            string agentAddr,
            string avatarAddr,
            string planetId,
            string sku)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("IAPServiceManager is not initialized.");
                return null;
            }

            var (code, error, mediaType, content) =
                await _client.PurchaseFreeAsync(
                    _store,
                    agentAddr,
                    avatarAddr,
                    planetId,
                    sku);
            if (code != HttpStatusCode.OK ||
                !string.IsNullOrEmpty(error))
            {
                Debug.LogError(
                    $"Purchase Free failed: {code}, {sku}, {error}, {mediaType}, {content}");
                return null;
            }

            if (mediaType != "application/json")
            {
                Debug.LogError(
                    $"Unexpected media type: {code}, {sku}, {error}, {mediaType}, {content}");
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
                var result = JsonSerializer.Deserialize<ReceiptDetailSchema>(
                    content!,
                    IAPServiceClient.JsonSerializerOptions)!;
                // NOTE: Enable this code if you want to use cache.
                // _cache.PurchaseProcessResults[result.Uuid] = result;
                if (result.Status == ReceiptStatus.Invalid ||
                    result.Status == ReceiptStatus.Unknown)
                {
                    UnregisterAndCache(result);
                }
                // NOTE: Enable this code if you want to use poller.
                // else
                // {
                //     _poller.Register(result.Uuid);
                // }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        public async Task<Dictionary<string, ReceiptDetailSchema?>?> PurchaseStatusAsync(
            HashSet<string> uuids)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("IAPServiceManager is not initialized.");
                return null;
            }

            var (code, error, mediaType, content) = await _client.PurchaseStatusAsync(uuids);
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
                return JsonSerializer.Deserialize<Dictionary<string, ReceiptDetailSchema?>>(
                    content!,
                    IAPServiceClient.JsonSerializerOptions)!;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        public async Task<string?> PurchaseLogAsync(
            string agentAddr,
            string avatarAddr,
            string planetId,
            string productId,
            string orderId,
            string data)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("IAPServiceManager is not initialized.");
                return null;
            }

            var (code, error, mediaType, content) =
                await _client.PurchaseLogAsync(
                    agentAddr,
                    avatarAddr,
                    planetId,
                    productId,
                    orderId,
                    data);
            if (code != HttpStatusCode.OK ||
                !string.IsNullOrEmpty(error))
            {
                Debug.LogError(
                    $"Purchase failed: {code}, {productId}, {error}, {mediaType}, {content}");
                return null;
            }

            if (mediaType != "application/json")
            {
                Debug.LogError(
                    $"Unexpected media type: {code}, {productId}, {error}, {mediaType}, {content}");
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
                return content;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        public async Task<L10NSchema?> L10NAsync()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("IAPServiceManager is not initialized.");
                return null;
            }

            var (code, error, mediaType, content) = await _client.L10NAsync();
            if (code != HttpStatusCode.OK ||
                !string.IsNullOrEmpty(error))
            {
                Debug.LogError(
                    $"L10N failed: {code}, {error}, {mediaType}, {content}");
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
                return JsonSerializer.Deserialize<L10NSchema?>(
                    content!,
                    IAPServiceClient.JsonSerializerOptions)!;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        private void UnregisterAndCache(ReceiptDetailSchema result)
        {
            // NOTE: Enable this code if you want to use poller.
            // _poller.Unregister(result.Uuid);
            // NOTE: Enable this code if you want to use cache.
            // _cache.PurchaseProcessResults[result.Uuid] = result;
        }

        private void OnPoll(Dictionary<string, ReceiptDetailSchema?> result)
        {
            foreach (var pair in result)
            {
                var (uuid, receiptDetailSchema) = pair;
                if (receiptDetailSchema is null)
                {
                    continue;
                }

                switch (receiptDetailSchema.Status)
                {
                    case ReceiptStatus.Init:
                    case ReceiptStatus.ValidationRequest:
                        continue;
                    case ReceiptStatus.Valid:
                        break;
                    case ReceiptStatus.Invalid:
                        Debug.LogWarning($"Invalid receipt: {uuid}");
                        UnregisterAndCache(receiptDetailSchema);
                        continue;
                    case ReceiptStatus.Unknown:
                        Debug.LogWarning($"Unknown receipt: {uuid}");
                        UnregisterAndCache(receiptDetailSchema);
                        continue;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                switch (receiptDetailSchema.TxStatus)
                {
                    case TxStatus.Created:
                    case TxStatus.Staged:
                        continue;
                    case TxStatus.Success:
                        Debug.LogWarning($"Tx success: {uuid}");
                        UnregisterAndCache(receiptDetailSchema);
                        continue;
                    case TxStatus.Failure:
                        Debug.LogWarning($"Tx failure: {uuid}");
                        UnregisterAndCache(receiptDetailSchema);
                        continue;
                    case TxStatus.Invalid:
                        Debug.LogWarning($"Tx invalid: {uuid}");
                        UnregisterAndCache(receiptDetailSchema);
                        continue;
                    case TxStatus.NotFound:
                        Debug.LogWarning($"Tx not found: {uuid}");
                        UnregisterAndCache(receiptDetailSchema);
                        continue;
                    case TxStatus.FailToCreate:
                        Debug.LogWarning($"Tx failed to create: {uuid}");
                        UnregisterAndCache(receiptDetailSchema);
                        continue;
                    case TxStatus.Unknown:
                        Debug.LogWarning($"Tx unknown: {uuid}");
                        UnregisterAndCache(receiptDetailSchema);
                        continue;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
