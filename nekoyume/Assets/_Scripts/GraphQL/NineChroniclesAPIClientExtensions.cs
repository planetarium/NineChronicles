using System.Threading.Tasks;
using Libplanet.Types.Blocks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Nekoyume.GraphQL
{
    public static class NineChroniclesAPIClientExtensions
    {
        public static async Task<BlockHash?> GetBlockHashAsync(
            this NineChroniclesAPIClient client,
            long blockIndex)
        {
            var query = $@"
query {{
    chainQuery {{
        blockQuery {{
            blocks(offset: {blockIndex}, limit: 1) {{
                hash
            }}
        }}
    }}
}}";
            var response = await client.GetObjectAsync<JObject>(query);
            if (response is null)
            {
                Debug.LogError($"Failed getting response : {nameof(JObject)}");
                return null;
            }

            try
            {
                var hash = response["chainQuery"]["blockQuery"]["blocks"][0]["hash"].ToString();
                return BlockHash.FromString(hash);
            }
            catch
            {
                Debug.LogError(response.ToString());
                return null;
            }
        }
    }
}
