using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using Libplanet.Crypto;

namespace Nekoyume.GraphQL
{
    public static class TxResultQuery
    {
        [Serializable]
        public class TxResultResponse
        {
            public Transaction transaction;
        }

        [Serializable]
        public class Transaction
        {
            public TransactionResult transactionResult;
        }

        [Serializable]
        public class TransactionResult
        {
            public TxStatus txStatus;
        }

        public class GoldBalanceResponse
        {
            public string balance;
        }

        [Serializable]
        public enum TxStatus {
            INVALID,
            STAGING,
            SUCCESS,
            FAILURE,
        }

        public class ArenaInfoResponse
        {
            public StateQuery StateQuery;
        }

        public class StateQuery
        {
            public ArenaInfo ArenaInfo;
        }

        public class ArenaInfo
        {
            public List<ArenaInformation> ArenaParticipants;
            public int PurchasedCountDuringInterval;
            public long LastBattleBlockIndex;
        }
        public class ArenaInformation
        {
            public Address AvatarAddr;
            public int Rank;
            public int Score;
            public int Win;
            public int Lose;
            public int Level;
            public string NameWithHash;
            public int Cp;
            public int ArmorId;
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

        public static async Task<ArenaInfoResponse> QueryArenaInfoAsync(NineChroniclesAPIClient client, Address avatarAddress)
        {
            if (!client.IsInitialized)
            {
                return null;
            }

            var query = @"query($avatarAddress: Address!) {
                stateQuery {
                    arenaInfo(avatarAddress: $avatarAddress) {
                        arenaParticipants {
                            score
                            rank
                            avatarAddr
                            win
                            lose
                            level
                            cp
                            nameWithHash
                            armorId
                        }
                        lastBattleBlockIndex
                        purchasedCountDuringInterval
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
    }
}
