using System;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System.Threading.Tasks;

namespace Nekoyume.GraphQL
{
    public class NineChroniclesAPIClient
    {
        public bool IsInitialized => _client != null;

        private readonly GraphQLHttpClient _client = null;

        public NineChroniclesAPIClient(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                return;
            }

            _client = new GraphQLHttpClient(host, new NewtonsoftJsonSerializer());
        }

        public async Task<T> GetObjectAsync<T>(string query) where T : class
        {
            var graphQlRequest = new GraphQLHttpRequest(query);
            return await GetObjectAsync<T>(graphQlRequest);
        }

        public async Task<T> GetObjectAsync<T>(GraphQLRequest request) where T : class
        {
            if (_client == null)
            {
                NcDebug.LogError("This API client is not initialized.");
                return null;
            }
            
            try
            {
                var graphQlRequest = await _client.SendQueryAsync<T>(request);
                if (graphQlRequest.Errors is not { Length: > 0 })
                {
                    return graphQlRequest.Data;
                }
                
                foreach (var error in graphQlRequest.Errors)
                {
                    NcDebug.LogError(error.Message);
                }

                return null;
            }
            catch (Exception e)
            {
                NcDebug.LogError($"Failed to get object with GraphQL Request.\n{e}");
                return null;
            }
        }
    }
}
