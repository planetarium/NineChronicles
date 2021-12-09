using System;
using System.Threading.Tasks;
using MagicOnion.Client;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    public class ClientFilter : IClientFilter
    {
        public async ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
        {
            try
            {
                return await next(context);
            }
            catch (Exception e)
            {
                Debug.Log($"Filter Catch Exception: {e}");
            }

            return null;
        }
    }
}
