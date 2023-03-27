using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Libplanet;
using Libplanet.Assets;
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
        }

        public async Task<(List<ItemProductResponseModel>, int)> GetBuyProducts(
            ItemSubType itemSubType,
            int offset,
            int limit,
            MarketOrderType order,
            StatType statType)
        {
            var url = $"{_url}/Market/products/items/{(int)itemSubType}?limit={limit}&offset={offset}&order={order}&stat={statType.ToString()}";
            var json = await _client.GetStringAsync(url);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var response = JsonSerializer.Deserialize<MarketProductResponse>(json, options);
            return (response.ItemProducts.ToList(), response.TotalCount);
        }

        public async Task<(List<FungibleAssetValueProductResponseModel>, int)> GetBuyFungibleAssetProducts(
            string ticker,
            int offset,
            int limit,
            MarketOrderType order)
        {
            var url = $"{_url}/Market/products/fav/{ticker}?limit={limit}&offset={offset}&order={order}";
            var json = await _client.GetStringAsync(url);
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
            var json = await _client.GetStringAsync(url);
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
            var json = await _client.GetStringAsync(url);
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
            var json = await _client.GetStringAsync(url);
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
