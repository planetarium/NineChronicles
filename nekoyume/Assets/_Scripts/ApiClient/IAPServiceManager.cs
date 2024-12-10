#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Libplanet.Crypto;
using Nekoyume.L10n;
using UniRx;
using UnityEngine;

namespace Nekoyume.ApiClient
{
    public static class InAppPurchaseServiceExtentions
    {
        public static string Sku(this InAppPurchaseServiceClient.ProductSchema product)
        {
#if UNITY_ANDROID
            return product.GoogleSku;
#elif UNITY_IOS
            if (Game.LiveAsset.GameConfig.IsKoreanBuild)
            {
                return product.AppleSkuK;
            }
            else
            {
                return product.AppleSku;
            }
#else
            return product.GoogleSku;
#endif
        }
        public static bool CheckSku(this InAppPurchaseServiceClient.ProductSchema product, string sku)
        {
            return product.GoogleSku == sku || product.AppleSku == sku || product.AppleSkuK == sku;
        }
        public static string GetCode(this InAppPurchaseServiceClient.ProductSchema product)
        {
            var skuInfos = product.GoogleSku.Split("_");
            return skuInfos.Last();
        }
        public static string GetPopupTitleText(this InAppPurchaseServiceClient.ProductSchema product)
        {
            if (!DateTime.TryParse(product.OpenTimestamp, out var openDate))
            {
                NcDebug.LogError($"Invalid OpenTimestamp: {product.OpenTimestamp}");
                return L10nManager.Localize($"MOBILE_SHOP_PRODUCT_{product.GetCode()}_POPUP_TITLE");
            }

            if (!DateTime.TryParse(product.CloseTimestamp, out var closeDate))
            {
                NcDebug.LogError($"Invalid CloseTimestamp: {product.CloseTimestamp}");
                return L10nManager.Localize($"MOBILE_SHOP_PRODUCT_{product.GetCode()}_POPUP_TITLE");
            }
            return L10nManager.Localize($"MOBILE_SHOP_PRODUCT_{product.GetCode()}_POPUP_TITLE", openDate.ToString("MM/dd"),closeDate.ToString("MM/dd"));
        }
        public static string GetListImagePath(this InAppPurchaseServiceClient.ProductSchema product)
        {
            return $"shop/images/product/list/{product.GetCode()}.png";
        }
        public static string GetDetailImagePath(this InAppPurchaseServiceClient.ProductSchema product)
        {
            return $"shop/images/product/detail/{product.GetCode()}.png";
        }
    }

    public class IAPServiceManager : IDisposable
    {
        public static readonly TimeSpan DefaultProductsCacheLifetime =
            TimeSpan.FromMinutes(10);

        private readonly InAppPurchaseServiceClient? _client;

        private readonly InAppPurchaseServiceClient.Store _store;

        private readonly InAppPurchaseServiceClient.PackageName _packageName;

        public ReactiveProperty<int> CurrentMileage = new(0);

        public bool IsInitialized { get; private set; }
        public bool IsDisposed { get; private set; }

        public IAPServiceManager(string url, InAppPurchaseServiceClient.Store store)
        {
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogError($"[{nameof(IAPServiceManager)}] IAPServiceHost is null.");
                return;
            }

            _client = new InAppPurchaseServiceClient(url);
            _store = store;

            // 플렛폼에 따라 기본적으로 사용하는 패키지 이름이 다르기 때문에 그에 맞게 설정해준다.
#if UNITY_IOS || UNITY_ANDROID
            if (InAppPurchaseServiceClient.PackageNameTypeConverter.InvalidEnumMapping.TryGetValue(Application.identifier, out var packageName))
            {
                _packageName = Enum.Parse<InAppPurchaseServiceClient.PackageName>(packageName);
            }
            else
            {
                Debug.LogError($"[{nameof(IAPServiceManager)}] Invalid PackageName: {Application.identifier}");
                _packageName = InAppPurchaseServiceClient.PackageName.com_planetariumlabs_ninechroniclesmobile;
            }
#else
            _packageName = InAppPurchaseServiceClient.PackageName.com_planetariumlabs_ninechroniclesmobile;
#endif
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized || _client is null)
            {
                Debug.LogError("IAPServiceManager is already initialized.");
                return;
            }

            await _client.GetPingAsync(
                (success) =>
                {
                    IsInitialized = true;
                },
                (error) =>
                {
                    IsInitialized = false;
                    Debug.LogError($"Failed to initialize IAPServiceManager: {error}");
                });

            IsInitialized = true;
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                Debug.LogError("IAPServiceManager is already disposed.");
                return;
            }

            _client?.Dispose();
            IsDisposed = true;
        }

        public async Task CheckProductAvailable(string productSku, Address agentAddr, string planetId, System.Action success, System.Action failed)
        {
            var categoryList = await GetProductsAsync(agentAddr, planetId);
            if (categoryList == null)
            {
                failed();
                return;
            }

            InAppPurchaseServiceClient.ProductSchema? selectedProduct = null;
            foreach (var category in categoryList)
            {
                foreach (var product in category.ProductList)
                {
                    if (product.Sku() == productSku)
                    {
                        selectedProduct = product;
                        if (product.Active && product.Buyable)
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

        public async Task<IReadOnlyList<InAppPurchaseServiceClient.CategorySchema>?> GetProductsAsync(Address agentAddr, string planetId)
        {
            if (string.IsNullOrEmpty(planetId))
            {
                Debug.LogWarning("planetId is null or empty.");
                return null;
            }

            if (!IsInitialized || _client is null)
            {
                Debug.LogWarning("IAPServiceManager is not initialized.");
                return null;
            }
            List<InAppPurchaseServiceClient.CategorySchema> result = null;
            await _client.GetProductAsync(agentAddr.ToString(), planetId, _packageName,
            (sucess) =>
            {
                result = sucess.ToList();
            },
            (error) =>
            {
                Debug.LogError(error);
            });

            return result;
        }

        public async Task<InAppPurchaseServiceClient.ReceiptDetailSchema?> PurchaseRequestAsync(
            string receipt,
            string agentAddr,
            string avatarAddr,
            string planetId,
            string transactionId,
            string appleOriginalTransactionID)
        {
            if (!IsInitialized || _client is null)
            {
                Debug.LogWarning("IAPServiceManager is not initialized.");
                return null;
            }
            InAppPurchaseServiceClient.ReceiptDetailSchema? result = null;
            await _client.PostPurchaseRequestAsync(_packageName,
                new InAppPurchaseServiceClient.ReceiptSchema
                {
                    AgentAddress = agentAddr,
                    AvatarAddress = avatarAddr,
                    PlanetId = planetId,
                    Data = receipt
                },
                (success) =>
                {
                    result = success;
                    CurrentMileage.Value = success.MileageResult;
                },
                (error) =>
                {
                    Debug.LogError(error);
                });

            return result;
        }

        /// <summary>
        /// Tx정보만 남아 있는 경우 구매처리
        /// </summary>
        /// <param name="receipt"></param>
        /// <param name="transactionId"></param>
        /// <param name="appleOriginalTransactionID"></param>
        /// <returns></returns>
        public async Task<InAppPurchaseServiceClient.ReceiptDetailSchema?> PurchaseRetryAsync(
            string receipt,
            string transactionId,
            string appleOriginalTransactionID)
        {
            if (!IsInitialized || _client is null)
            {
                Debug.LogWarning("IAPServiceManager is not initialized.");
                return null;
            }

            InAppPurchaseServiceClient.ReceiptDetailSchema? result = null;
            await _client.PostPurchaseRetryAsync(_packageName,
                new InAppPurchaseServiceClient.SimpleReceiptSchema
                {
                    Store = _store,
                    Data = receipt
                },
                (success) =>
                {
                    result = success;
                    CurrentMileage.Value = success.MileageResult;
                },
                (error) =>
                {
                    Debug.LogError(error);
                });

            return result;
        }

        public async Task<InAppPurchaseServiceClient.ReceiptDetailSchema?> PurchaseFreeAsync(
            string agentAddr,
            string avatarAddr,
            string planetId,
            string sku)
        {
            if (!IsInitialized || _client is null)
            {
                Debug.LogWarning("IAPServiceManager is not initialized.");
                return null;
            }

            InAppPurchaseServiceClient.ReceiptDetailSchema? result = null;
            await _client.PostPurchaseFreeAsync(_packageName,
                new InAppPurchaseServiceClient.FreeReceiptSchema
                {
                    Store = _store,
                    AgentAddress = agentAddr,
                    AvatarAddress = avatarAddr,
                    PlanetId = planetId,
                    Sku = sku
                },
                (success) =>
                {
                    result = success;
                    CurrentMileage.Value = success.MileageResult;
                },
                (error) =>
                {
                    Debug.LogError(error);
                });
            return result;
        }

        public async Task<InAppPurchaseServiceClient.ReceiptDetailSchema?> PurchaseMileageAsync(
            string agentAddr,
            string avatarAddr,
            string planetId,
            string sku)
        {
            if (!IsInitialized || _client is null)
            {
                Debug.LogWarning("IAPServiceManager is not initialized.");
                return null;
            }

            InAppPurchaseServiceClient.ReceiptDetailSchema? result = null;
            await _client.PostPurchaseMileageAsync(_packageName,
                new InAppPurchaseServiceClient.FreeReceiptSchema
                {
                    Store = _store,
                    AgentAddress = agentAddr,
                    AvatarAddress = avatarAddr,
                    PlanetId = planetId,
                    Sku = sku
                },
                (success) =>
                {
                    result = success;
                    CurrentMileage.Value = success.MileageResult;
                },
                (error) =>
                {
                    Debug.LogError(error);
                });
            return result;
        }

        public async Task<Dictionary<string, InAppPurchaseServiceClient.ReceiptDetailSchema?>?> PurchaseStatusAsync(
            HashSet<string> uuids)
        {
            if (!IsInitialized || _client is null)
            {
                Debug.LogWarning("IAPServiceManager is not initialized.");
                return null;
            }

            string content = string.Empty;
            await _client.GetPurchaseStatusAsync(string.Join("?uuid=", uuids),
                (success) =>
                {
                    content = success;
                },
                (error) =>
                {
                    Debug.LogError(error);
                });
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, InAppPurchaseServiceClient.ReceiptDetailSchema?>>(
                    content);
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
            if (!IsInitialized || _client is null)
            {
                Debug.LogWarning("IAPServiceManager is not initialized.");
                return null;
            }
            string result = string.Empty;
            await _client.GetPurchaseLogAsync(planetId, agentAddr, avatarAddr, productId, orderId, data,
                (success) =>
                {
                    result = success;
                },
                (error) =>
                {
                    Debug.LogError(error);
                });
            return result;
        }

        public async Task<InAppPurchaseServiceClient.L10NSchema?> L10NAsync()
        {
            if (!IsInitialized || _client is null)
            {
                Debug.LogWarning("IAPServiceManager is not initialized.");
                return null;
            }
            InAppPurchaseServiceClient.L10NSchema? result = null;
            await _client.GetL10nAsync(_packageName,
                (success) =>
                {
                    result = success;
                },
                (error) =>
                {
                    Debug.LogError(error);
                });
            return result;
        }

        public async Task<InAppPurchaseServiceClient.MileageSchema> RefreshMileageAsync()
        {
            if (!IsInitialized || _client is null)
            {
                Debug.LogWarning("IAPServiceManager is not initialized.");
                return null;
            }
            InAppPurchaseServiceClient.MileageSchema? result = null;
            var states = State.States.Instance;
            var agent = states?.AgentState?.address.ToHex();
            await _client.GetMileageAsync(agent, Game.Game.instance?.CurrentPlanetId?.ToString(),
                (success) =>
                {
                    result = success;
                    CurrentMileage.Value = success.Mileage;
                },
                (error) =>
                {
                    Debug.LogError(error);
                });
            return result;
        }
    }
}
