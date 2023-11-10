#nullable enable

using System;
using System.Text;
using Cysharp.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using Libplanet.Crypto;
using Nekoyume.GraphQL.GraphTypes;
using UnityEngine;

namespace Nekoyume
{
    public static class GraphQlHttpClientExtensions
    {
        public static async UniTask<T> StateQueryAsync<T>(
            this GraphQLHttpClient client,
            string query)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            var message = $" Endpoint: {client.Options.EndPoint?.AbsoluteUri ?? "null"}" +
                          $" Query: {query}";
            Debug.Log($"[GraphQl] StateQueryAsync()... {message}");
            var request = new GraphQLRequest(query);
            var response = await client.SendQueryAsync<StateQueryGraphType<T>>(request);
            if (response.Errors != null)
            {
                Debug.LogError("[GraphQl] StateQueryAsync()... request has errors." +
                               $" request: {message}");
                foreach (var error in response.Errors)
                {
                    Debug.LogError(error.Message);
                }
            }

            return response.Data.StateQuery;
        }

        public static async UniTask<AgentGraphType> QueryAgentAsync(
            this GraphQLHttpClient client,
            Address agentAddress)
        {
            var sb = new StringBuilder("query { stateQuery { agent(address: ");
            sb.Append($"\"{agentAddress.ToString()}\"");
            sb.Append(") { address avatarStates { address name level } } } }");
            var query = sb.ToString();
            return await client.StateQueryAsync<AgentGraphType>(query);
        }

        public static async UniTask<AvatarsGraphType> QueryAvatarsAsync(
            this GraphQLHttpClient client,
            params string[] avatarAddresses)
        {
            if (avatarAddresses.Length == 0)
            {
                return new AvatarsGraphType
                {
                    Avatars = Array.Empty<AvatarGraphType>(),
                };
            }

            var sb = new StringBuilder("query { stateQuery { avatars(addresses: [");
            foreach (var avatarAddress in avatarAddresses)
            {
                sb.Append($"\"{avatarAddress}\", ");
            }

            sb.Remove(sb.Length - 2, 2);
            sb.Append("]) { address name level } } }");
            var query = sb.ToString();
            return await client.StateQueryAsync<AvatarsGraphType>(query);
        }

        public static async UniTask<AvatarsGraphType> QueryAvatarsByAgentAddressAsync(
            this GraphQLHttpClient client,
            string agentAddress)
        {
            var addr = new Address(agentAddress);
            var avatarAddresses = new[]
            {
                Addresses.GetAvatarAddress(addr, 0).ToString(),
                Addresses.GetAvatarAddress(addr, 1).ToString(),
                Addresses.GetAvatarAddress(addr, 2).ToString(),
            };
            return await client.QueryAvatarsAsync(avatarAddresses);
        }
    }
}
