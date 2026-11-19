using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using MarketService.Response;
using Nekoyume.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;

namespace Nekoyume.ApiClient
{
    public class MarketServiceClient
    {
        private string _url;
        private HttpClient _client;

        public MarketServiceClient(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                _url = string.Empty;
                NcDebug.Log($"[{nameof(MarketServiceClient)}] initialized with empty host url because of no MarketServiceHost");
                return;
            }

            _url = url;
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(30);
            NcDebug.Log($"[{nameof(MarketServiceClient)}] initialized host: {url}");
        }

        // Market reads may use other hosts, but authentication is only offered for
        // these exact official provider paths. Authentication itself uses a pinned origin.
        public bool SupportsSignedMarketFilters =>
            SupportsSignedMarketFiltersForChain("odin") ||
            SupportsSignedMarketFiltersForChain("heimdall");

        public bool SupportsSignedMarketFiltersForChain(string chain)
        {
            var path = chain == "odin" ? "/marketProviderOdin" :
                chain == "heimdall" ? "/marketProviderHeimdall" : null;
            if (path == null || !Uri.TryCreate(_url, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort ||
                uri.UserInfo.Length != 0 || _url.IndexOfAny(new[] { '?', '#' }) >= 0 ||
                !(uri.Host.Equals("api.9capi.com", StringComparison.OrdinalIgnoreCase) ||
                  uri.Host.Equals("b.9capi.com", StringComparison.OrdinalIgnoreCase)))
                return false;

            // Compare the original path too: Uri normalizes /cc/../ and escaped
            // segments; neither should authorize an unrelated configured endpoint.
            var authorityEnd = _url.IndexOf('/', _url.IndexOf("://", StringComparison.Ordinal) + 3);
            var rawPath = authorityEnd < 0 ? "" : _url.Substring(authorityEnd);
            return (rawPath == path || rawPath == path + "/") &&
                   (uri.AbsolutePath == path || uri.AbsolutePath == path + "/");
        }

        private async Task<string> GetMarketResponseAsync(string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Capture active identity per request, not on the shared HttpClient.
            var states = Game.Game.instance?.States;
            var agentAddress = states?.AgentState?.address;
            var avatarAddress = states?.CurrentAvatarState?.address;
            if (agentAddress.HasValue)
            {
                request.Headers.Add("X-Agent-Address", agentAddress.Value.ToString());
            }

            if (avatarAddress.HasValue)
            {
                request.Headers.Add("X-Avatar-Address", avatarAddress.Value.ToString());
            }

            using var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<(List<ItemProductResponseModel>, int)> GetBuyProducts(
            ItemSubType itemSubType,
            int offset,
            int limit,
            MarketOrderType order,
            StatType statType,
            int[] ids,
            bool isCustom = false)
        {
            var url = $"{_url}/Market/products/items/{(int)itemSubType}?limit={limit}&offset={offset}&order={order}&stat={statType.ToString()}&isCustom={isCustom}";
            if (ids is not null && ids.Any())
            {
                var idParamName = itemSubType.IsEquipment() ? "&iconIds=" : "&itemIds=";
                url = url + idParamName + string.Join(idParamName, ids);
            }

            string json;
            try
            {
                json = await GetMarketResponseAsync(url);
            }
            catch (Exception e)
            {
                NcDebug.LogException(e);
                return (new List<ItemProductResponseModel>(), 0);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var response = JsonSerializer.Deserialize<MarketProductResponse>(json, options);
            return (response.ItemProducts.ToList(), response.TotalCount);
        }

        public async Task<(List<FungibleAssetValueProductResponseModel>, int)> GetBuyFungibleAssetProducts(
            string[] tickers,
            int offset,
            int limit,
            MarketOrderType order)
        {
            var url = $"{_url}/Market/products/fav?limit={limit}&offset={offset}&order={order}";

            if (tickers is not null && tickers.Any())
            {
                url = url + "&tickers=" + string.Join("&tickers=", tickers);
            }

            string json;
            try
            {
                json = await GetMarketResponseAsync(url);
            }
            catch (Exception e)
            {
                NcDebug.LogException(e);
                return (new List<FungibleAssetValueProductResponseModel>(), 0);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var response = JsonSerializer.Deserialize<MarketProductResponse>(json, options);
            return (response.FungibleAssetValueProducts.ToList(), response.TotalCount);
        }

        public async Task<(List<FungibleAssetValueProductResponseModel>, List<ItemProductResponseModel>)>
            GetProducts(Address address)
        {
            var url = $"{_url}/Market/products/{address}";
            string json;
            try
            {
                json = await GetMarketResponseAsync(url);
            }
            catch (Exception e)
            {
                NcDebug.LogException(e);
                return (
                    new List<FungibleAssetValueProductResponseModel>(),
                    new List<ItemProductResponseModel>());
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var response = JsonSerializer.Deserialize<MarketProductResponse>(json, options);
            var fungibleAssets = response.FungibleAssetValueProducts.ToList();
            var items = response.ItemProducts.ToList();
            return (fungibleAssets, items);
        }

        public async Task<(List<FungibleAssetValueProductResponseModel>, List<ItemProductResponseModel>)> GetProducts(Guid productId)
        {
            var url = $"{_url}/Market/products?productIds={productId}";
            string json;
            try
            {
                json = await GetMarketResponseAsync(url);
            }
            catch (Exception e)
            {
                NcDebug.LogException(e);
                return (
                    new List<FungibleAssetValueProductResponseModel>(),
                    new List<ItemProductResponseModel>());
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var response = JsonSerializer.Deserialize<MarketProductResponse>(json, options);
            var fungibleAssets = response.FungibleAssetValueProducts.ToList();
            var items = response.ItemProducts.ToList();
            return (fungibleAssets, items);
        }

        public async Task<(
            string,
            ItemProductResponseModel,
            FungibleAssetValueProductResponseModel)> GetProductInfo(
            Guid productId,
            bool hasColor = true,
            bool useElementalIcon = true)
        {
            var url = $"{_url}/Market/products?productIds={productId}";
            string json;
            try
            {
                json = await GetMarketResponseAsync(url);
            }
            catch (Exception e)
            {
                NcDebug.LogException(e);
                return (string.Empty, null, null);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var response = JsonSerializer.Deserialize<MarketProductResponse>(json, options);
            var fungibleAssetProduct = response.FungibleAssetValueProducts.FirstOrDefault();
            if (fungibleAssetProduct != null)
            {
                var currency = Currency.Legacy(fungibleAssetProduct.Ticker, 0, null);
                var fav = new FungibleAssetValue(currency, 0, 0);
                return (fav.GetLocalizedName(), null, fungibleAssetProduct);
            }

            var itemProduct = response.ItemProducts.FirstOrDefault();
            if (itemProduct != null)
            {
                var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
                itemSheet.TryGetValue(itemProduct.ItemId, out var row);
                return (row.GetLocalizedName(hasColor, useElementalIcon), itemProduct, null);
            }

            return (string.Empty, null, null);
        }
    }
}
