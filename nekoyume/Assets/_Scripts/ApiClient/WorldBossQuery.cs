using System;
using System.Collections;
using System.Threading.Tasks;
using Libplanet.Crypto;
using Nekoyume.UI.Model;
using UnityEngine.Networking;

namespace Nekoyume.ApiClient
{
    public static class WorldBossQuery
    {
        public static string Url { get; private set; }

        public static void SetUrl(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                Url = string.Empty;
                NcDebug.Log($"[{nameof(WorldBossQuery)}] initialized with empty host url because of no OnBoardingHost. url: {Url}");
                return;
            }

            Url = $"{host}/raid";
            NcDebug.Log($"[{nameof(WorldBossQuery)}] initialized. host: {host} url: {Url}");
        }

        public static async Task<WorldBossRankingResponse> QueryRankingAsync(
            int raidId,
            Address address)
        {
            var apiClient = ApiClients.Instance.WorldBossClient;
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

        public static IEnumerator CoGetSeasonRewards(
            int raidId,
            Address avatarAddress,
            Action<string> onSuccess,
            Action<int, Address> onFailed)
        {
            var url = $"{Url}/{raidId}/{avatarAddress}/rewards";
            using var request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
            else
            {
                onFailed?.Invoke(raidId, avatarAddress);
            }
        }
    }
}
