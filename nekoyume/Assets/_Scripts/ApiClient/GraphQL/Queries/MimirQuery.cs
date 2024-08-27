using System;
using System.Text;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using Newtonsoft.Json.Linq;

namespace Nekoyume.GraphQL.Queries
{
    public static class MimirQuery
    {
        private static async Task<GraphQLResponse<JObject>> Query(
            GraphQLHttpClient mimirClient,
            string query)
        {
            try
            {
                var request = new GraphQLHttpRequest(query);
                var response = await mimirClient.SendQueryAsync<JObject>(request);
                if (response.Data is not null ||
                    response.Errors is not { Length: > 0 })
                {
                    return response;
                }

                var sb = new StringBuilder();
                sb.AppendLine("GraphQL response data is null and has errors:");
                foreach (var error in response.Errors)
                {
                    sb.AppendLine($"  {error.Message}");
                }

                NcDebug.LogError(sb.ToString());
                return null;
            }
            catch (Exception e)
            {
                NcDebug.LogError($"Failed to execute GraphQL query.\n{e}");
                return null;
            }
        }

        public static async Task<long?> GetMetadataBlockIndexAsync(
            GraphQLHttpClient mimirClient,
            string collectionName)
        {
            if (mimirClient is null)
            {
                return null;
            }

            var query = @$"query {{
  metadata(collectionName: ""{collectionName}"") {{
    latestBlockIndex
  }}
}}";
            var response = await Query(mimirClient, query);
            var data = response?.Data;
            if (data is null)
            {
                return null;
            }

            try
            {
                return data["metadata"]["latestBlockIndex"].Value<long>();
            }
            catch (Exception e)
            {
                NcDebug.LogError($"Failed to parse latestBlockIndex from metadata.\n{e}");
                return null;
            }
        }
    }
}
