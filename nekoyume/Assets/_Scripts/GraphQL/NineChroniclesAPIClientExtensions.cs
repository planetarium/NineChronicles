using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using Libplanet.Crypto;
using Libplanet.Types.Blocks;
using Nekoyume.UI.Model;
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
                NcDebug.LogError($"Failed getting response : {nameof(JObject)}");
                return null;
            }

            try
            {
                var hash = response["chainQuery"]["blockQuery"]["blocks"][0]["hash"].ToString();
                return BlockHash.FromString(hash);
            }
            catch
            {
                NcDebug.LogError(response.ToString());
                return null;
            }
        }

        public static async Task<ArenaInfoResponse> QueryArenaInfoAsync(this NineChroniclesAPIClient client, Address avatarAddress)
        {
            if (!client.IsInitialized)
            {
                return null;
            }

            var query = @"query($avatarAddress: Address!) {
                stateQuery {
                    arenaParticipants(avatarAddress: $avatarAddress) {
                        score
                        rank
                        avatarAddr
                        winScore
                        loseScore
                        level
                        cp
                        nameWithHash
                        portraitId
                    }
                }
            }";

            var request = new GraphQLRequest
            {
                Query = query,
                Variables = new
                {
                    avatarAddress = avatarAddress.ToString()
                }
            };
            var response = await client.GetObjectAsync<ArenaInfoResponse>(request);
            return response;
        }

        public class ArenaInfoResponse
        {
            public StateQuery StateQuery;
        }

        public class StateQuery
        {
            public List<ArenaParticipantModel> ArenaParticipants;
        }
    }
}
