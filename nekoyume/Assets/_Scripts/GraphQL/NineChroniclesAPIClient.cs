using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System.Threading.Tasks;
using UnityEngine;

namespace Nekoyume.GraphQL
{
    public class NineChroniclesAPIClient
    {
        public static NineChroniclesAPIClient Instance
        {
            get
            {
                if (instance is null)
                {
                    instance = new NineChroniclesAPIClient();
                }

                return instance;
            }
        }

        private static NineChroniclesAPIClient instance = null;

        private GraphQLHttpClient _client = null;

        private readonly string endpointURL = "https://api.9c.gg/graphql";

        private NineChroniclesAPIClient()
        {
            _client = new GraphQLHttpClient(endpointURL, new NewtonsoftJsonSerializer());
        }

        public async Task<T> GetObjectAsync<T>(string query) where T : class
        {
            var graphQLRequest = new GraphQLHttpRequest(query);
            return await GetObjectAsync<T>(graphQLRequest);
        }

        public async Task<T> GetObjectAsync<T>(GraphQLRequest request) where T : class
        {
            var graphQLResponse = await _client.SendQueryAsync<T>(request);
            if (graphQLResponse.Errors != null && graphQLResponse.Errors.Length > 0)
            {
                foreach (var error in graphQLResponse.Errors)
                {
                    Debug.LogError(error.Message);
                }

                return null;
            }

            return graphQLResponse.Data;
        }
    }
}
