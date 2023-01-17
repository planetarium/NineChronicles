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
    public class ShopServiceClient
    {
        private string _url;
        private HttpClient _client;

        public ShopServiceClient(string url)
        {
            _url = url;
            _client = new HttpClient();
        }

        public async Task<List<ShopProductModel>> GetProducts(ItemSubType itemSubType)
        {
            var url = $"{_url}/products/{(int)itemSubType}";
            var json = await _client.GetStringAsync(url);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var response = JsonSerializer.Deserialize<ProductResponse>(json, options);
            return response.Products.ToList();
        }
    }
}
