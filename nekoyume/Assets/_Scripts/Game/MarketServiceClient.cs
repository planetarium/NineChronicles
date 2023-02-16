using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Libplanet;
using MarketService.Response;
using Nekoyume.EnumType;
using Nekoyume.Model.Item;

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
            MarketOrderType order)
        {
            var url = $"{_url}/Market/products/items/{(int)itemSubType}?limit={limit}&offset={offset}&order={order}";
            var json = await _client.GetStringAsync(url);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var response = JsonSerializer.Deserialize<MarketProductResponse>(json, options);
            return (response.ItemProducts.Where(p => p.Exist).ToList(), response.TotalCount);
        }

        public async Task<List<ItemProductResponseModel>> GetProducts(Address address)
        {
            var url = $"{_url}/Market/products/{address}";
            var json = await _client.GetStringAsync(url);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var response = JsonSerializer.Deserialize<MarketProductResponse>(json, options);
            return response.ItemProducts.Where(p => p.Exist).ToList();
        }
    }
}
