using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Libplanet;
using Nekoyume.UI.Model;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.UI.Module.WorldBoss
{
    public static class WorldBossQuery
    {
        private const string URL =
            "http://a93dd1d705f7a43149125438c63d092e-1911438231.us-east-2.elb.amazonaws.com:8080/raid";

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

        public static async Task<TxResultResponse> QueryTxResultAsync(string txId)
        {
            var rpcClient = Game.Game.instance.RpcClient;
            if (!rpcClient.IsInitialized)
            {
                return null;
            }

            var query = @$"query {{
                transaction {{
                    transactionResult(txId: ""{txId}"") {{
                            txStatus
                        }}
                }}
            }}";

            var response = await rpcClient.GetObjectAsync<TxResultResponse>(query);
            return response;
        }

        public static async Task<List<SeasonRewards>> CheckTxStatus(List<SeasonRewards> rewards)
        {
            var successList = new List<SeasonRewards>();
            await foreach (var reward in rewards)
            {
                var res = await QueryTxResultAsync(reward.tx_id);
                if (res.transaction.transactionResult.txStatus == TxStatus.SUCCESS)
                {
                    successList.Add(reward);
                }
            }

            return successList;
        }

        public static IEnumerator CoClaimSeasonReward(
            int raidId,
            Address agentAddress,
            Address avatarAddress,
            System.Action<string> onSuccess,
            System.Action onFailed)
        {
            var form = new WWWForm();
            form.AddField("raid_id", raidId);
            form.AddField("avatar_address", avatarAddress.ToHex());
            form.AddField("agent_address", agentAddress.ToHex());

            using var request = UnityWebRequest.Post($"{URL}/reward", form);
            yield return request.SendWebRequest();

            Debug.Log(request.result != UnityWebRequest.Result.Success
                ? request.error
                : "Form upload complete");

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
            else
            {
                onFailed?.Invoke();
            }
        }

        public static IEnumerator CoIsExistSeasonReward(
            int raidId,
            Address avatarAddress,
            System.Action<string> onSuccess)
        {
            using var request = UnityWebRequest.Get($"{URL}/{raidId}/{avatarAddress}");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
        }
    }
}
