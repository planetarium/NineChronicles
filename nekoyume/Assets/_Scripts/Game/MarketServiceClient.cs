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
using UnityEngine;

namespace Nekoyume.Game
{
    public class MarketServiceClient
    {
        private string _url;
        private HttpClient _client;

        public MarketServiceClient(string url)
        {
            _url = url;
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<(List<ItemProductResponseModel>, int)> GetBuyProducts(
            ItemSubType itemSubType,
            int offset,
            int limit,
            MarketOrderType order,
            StatType statType,
            int[] itemIds)
        {
            var url = $"{_url}/Market/products/items/{(int)itemSubType}?limit={limit}&offset={offset}&order={order}&stat={statType.ToString()}";
            if (itemIds is not null && itemIds.Any())
            {
                url = url + "&itemIds=" + string.Join("&itemIds=", itemIds);
            }

            string json;
            try
            {
                json = await _client.GetStringAsync(url);
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
                json = await _client.GetStringAsync(url);
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
                json = await _client.GetStringAsync(url);
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
                json = await _client.GetStringAsync(url);
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
                json = await _client.GetStringAsync(url);
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
                var itemSheet = Game.instance.TableSheets.ItemSheet;
                itemSheet.TryGetValue(itemProduct.ItemId, out var row);
                return (row.GetLocalizedName(hasColor, useElementalIcon), itemProduct, null);
            }

            return (string.Empty, null, null);
        }
    }
}
