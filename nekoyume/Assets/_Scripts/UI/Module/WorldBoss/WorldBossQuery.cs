using System;
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
        private static string _url;
        public static void SetUrl(string host)
        {
            _url = $"{host}/raid";
        }

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

        public static async Task<ShopResponse> QueryShopEquipments()
        {
            var apiClient = Game.Game.instance.ApiClient;
            if (!apiClient.IsInitialized)
            {
                return null;
            }

            var query = @$"query {{
                shopQuery {{
                shopEquipments(sellerAvatarAddress: ""0xA0cC54AF585Eb8E1A694da90A9385FC1EB33d854"") {{
                    id
                    sellerAgentAddress
                    sellerAvatarAddress
                    orderId
                    tradableId
                    sellStartedBlockIndex
                    sellExpiredBlockIndex
                    price
                    combatPoint
                    level
                    itemCount
                    itemSubType
                }}
            }}
            }}";

            var response = await apiClient.GetObjectAsync<ShopResponse>(query);
            return response;
        }

        private static async Task<TxResultResponse> QueryTxResultAsync(string txId)
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

        public static IEnumerator CoGetSeasonRewards(
            int raidId,
            Address avatarAddress,
            Action<string> onSuccess,
            Action<int, Address> onFailed)
        {
            var url = $"{_url}/{raidId}/{avatarAddress}/rewards";
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
