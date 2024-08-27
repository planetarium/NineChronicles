using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using Libplanet.Crypto;
using Nekoyume.Arena;
using Nekoyume.Model.Arena;
using Nekoyume.UI.Model;
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

        public static async Task<List<ArenaParticipantModel>> GetArenaParticipantsAsync(
            GraphQLHttpClient mimirClient,
            Address avatarAddress)
        {
            if (mimirClient is null)
            {
                return null;
            }

            var query = @$"query {{
  arena {{
    leaderboardByAvatarAddress(avatarAddress: ""{avatarAddress}"") {{
      address
      simpleAvatar {{
        name
        level
      }}
      rank
      arenaScore {{
        score
      }}
    }}
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
                var children = data["arena"]["leaderboardByAvatarAddress"].Children<JObject>();
                var participants = children
                    .Select(e =>
                    {
                        var address = new Address(e["address"].Value<string>());
                        var name = e["simpleAvatar"]["name"].Value<string>();
                        var nameWithHash = $"{name} <size=80%><color=#A68F7E>#{address.ToHex()[..4]}</color></size>";
                        return new ArenaParticipantModel
                        {
                            AvatarAddr = address,
                            NameWithHash = nameWithHash,
                            PortraitId = GameConfig.DefaultAvatarArmorId,
                            Level = e["simpleAvatar"]["level"].Value<int>(),
                            Cp = 999_999_999,
                            GuildName = "Guild Name Here",
                            Score = e["arenaScore"]["score"].Value<int>(),
                            Rank = e["rank"].Value<int>(),
                            WinScore = 0,
                            LoseScore = 0,
                        };
                    })
                    .ToList();
                var me = participants.FirstOrDefault(p => p.AvatarAddr.Equals(avatarAddress));
                var myScore = me?.Score ?? ArenaScore.ArenaScoreDefault;
                foreach (var participant in participants)
                {
                    var (myWinScore, myDefeatScore, _) =
                        ArenaHelper.GetScores(myScore, participant.Score);
                    participant.WinScore = myWinScore;
                    participant.LoseScore = myDefeatScore;
                }

                return participants;
            }
            catch (Exception e)
            {
                NcDebug.LogError($"Failed to parse arena participants.\n{e}");
                return null;
            }
        }
    }
}
