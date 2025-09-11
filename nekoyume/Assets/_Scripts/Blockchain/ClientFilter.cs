using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Grpc.Core;
using MagicOnion.Client;

namespace Nekoyume.Blockchain
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
                    NcDebug.Log("Request Begin:" + context.MethodPath);
                    var sw = Stopwatch.StartNew();
                    var resp = await next(context);
                    sw.Stop();
                    NcDebug.Log("Request Completed:" + context.MethodPath + ", Elapsed:" +
                        sw.Elapsed.TotalMilliseconds + "ms");
                    return resp;
                }
                catch (Exception e)
                {
                    if (e is RpcException {StatusCode: StatusCode.FailedPrecondition,})
                    {
                        NcDebug.Log("Request Failed:" + context.MethodPath);
                        throw;
                    }
                    await Task.Delay((3 - retryCount) * 1000);
                    retryCount++;
                    exception = e;
                }
            }

            NcDebug.Log($"Filter Catch Exception: {exception}");
            return null;
        }
    }
}
