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
        public static async UniTask<(GraphQLError[]? errors, T? result)> StateQueryAsync<T>(
            this GraphQLHttpClient client,
            string query)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            var message = $" Endpoint: {client.Options.EndPoint?.AbsoluteUri ?? "null"}" +
                          $" Query: {query}";
            NcDebug.Log($"[GraphQl] StateQueryAsync()... {message}");
            var request = new GraphQLRequest(query);
            var response = await client.SendQueryAsync<StateQueryGraphType<T>>(request);
            if (response.Errors != null)
            {
                NcDebug.LogError("[GraphQl] StateQueryAsync()... request has errors." +
                               $" request: {message}");
                foreach (var error in response.Errors)
                {
                    NcDebug.LogError(error.Message);
                }
            }

            return (response.Errors, response.Data.StateQuery);
        }

        public static async UniTask<(GraphQLError[]? errors, AgentGraphType? result)> QueryAgentAsync(
            this GraphQLHttpClient client,
            Address agentAddress)
        {
            var sb = new StringBuilder("query { stateQuery { agent(address: ");
            sb.Append($"\"{agentAddress.ToString()}\"");
            sb.Append(") { address avatarStates { address name level } } } }");
            var query = sb.ToString();
            return await client.StateQueryAsync<AgentGraphType>(query);
        }

        public static async UniTask<(
            GraphQLError[]? errors,
            AgentAndPledgeGraphType? result)> QueryAgentAndPledgeAsync(
            this GraphQLHttpClient client,
            Address agentAddress)
        {
            var sb = new StringBuilder("query { stateQuery { agent(address: ");
            sb.Append($"\"{agentAddress.ToString()}\"");
            sb.Append(") { address avatarStates { address name level } }");
            sb.Append(" pledge(agentAddress: ");
            sb.Append($"\"{agentAddress.ToString()}\"");
            sb.Append(") { patronAddress approved mead } } }");
            var query = sb.ToString();
            return await client.StateQueryAsync<AgentAndPledgeGraphType>(query);
        }

        public static async UniTask<(GraphQLError[]? errors, AvatarsGraphType? result)> QueryAvatarsAsync(
            this GraphQLHttpClient client,
            params string[] avatarAddresses)
        {
            if (avatarAddresses.Length == 0)
            {
                return (
                    null,
                    new AvatarsGraphType
                    {
                        Avatars = Array.Empty<AvatarGraphType>(),
                    });
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

        public static async UniTask<(GraphQLError[]? errors, AvatarsGraphType? result)> QueryAvatarsByAgentAddressAsync(
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
