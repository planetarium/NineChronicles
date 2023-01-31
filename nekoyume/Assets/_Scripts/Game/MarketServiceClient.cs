using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using UnityEngine.Networking;

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

        public async Task<List<ItemProductModel>> GetProducts()
        {
            var url = $"{_url}/products/";
            var json = await _client.GetStringAsync(url);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var response = JsonSerializer.Deserialize<ProductResponse>(json, options);
            return response.ItemProducts.ToList();
        }
    }
}
