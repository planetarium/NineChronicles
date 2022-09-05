using System.Threading.Tasks;
using Libplanet;
using Nekoyume.UI.Model;

namespace Nekoyume.UI.Module.WorldBoss
{
    public static class WorldBossQuery
    {
        public static async Task<WorldBossRankingResponse> QueryRankingAsync(
            int raidId,
            Address address)
        {
            var apiClient = Game.Game.instance.ApiClient;
            if (!apiClient.IsInitialized)
            {
                return null;
            }

            var query = @$"query {{
                worldBossTotalUsers(raidId: {raidId})
                worldBossRanking(raidId: {raidId}, avatarAddress: ""{address}"") {{
                    blockIndex
                    rankingInfo {{
                        highScore
                        address
                        ranking
                        level
                        cp
                        iconId
                        avatarName
                        totalScore
                    }}
                }}
            }}";

            var response = await apiClient.GetObjectAsync<WorldBossRankingResponse>(query);
            return response;
        }
    }
}
