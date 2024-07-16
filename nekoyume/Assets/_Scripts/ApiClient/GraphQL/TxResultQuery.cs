using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using Libplanet.Crypto;
using Nekoyume.ApiClient;

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
        public enum TxStatus
        {
            INVALID,
            STAGING,
            SUCCESS,
            FAILURE
        }

        public static async Task<TxResultResponse> QueryTxResultAsync(string txId)
        {
            var rpcClient = ApiClients.Instance.RpcGraphQlClient;
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
    }
}
