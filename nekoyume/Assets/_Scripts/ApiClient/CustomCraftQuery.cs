using System.Collections.Generic;
using System.Threading.Tasks;
using Nekoyume.GraphQL;

namespace Nekoyume.ApiClient
{
    public class CustomEquipmentCraftIconCountResponse
    {
        public string itemSubType;
        public int iconId;
        public long count;
    }

    public class CustomEquipmentCraftIconCount
    {
        public List<CustomEquipmentCraftIconCountResponse> customEquipmentCraftIconCount;
    }

    public static class CustomCraftQuery
    {
        public static async Task<CustomEquipmentCraftIconCount> GetCustomEquipmentCraftIconCountAsync(NineChroniclesAPIClient apiClient)
        {
            var query = $@"query {{
  customEquipmentCraftIconCount(itemSubType:null) {{
  	itemSubType
  	iconId
  	count
	}}
}}
";
            var response = await apiClient.GetObjectAsync<CustomEquipmentCraftIconCount>(query);
            return response;
        }
    }
}
