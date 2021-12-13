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
            var retryCount = 0;
            Exception exception = null;
            while (retryCount < 3)
            {
                try
                {
                    return await next(context);
                }
                catch (Exception e)
                {
                    await Task.Delay((3 - retryCount) * 1000);
                    retryCount++;
                    exception = e;
                }
            }
            Debug.Log($"Filter Catch Exception: {exception}");
            return null;
        }
    }
}
