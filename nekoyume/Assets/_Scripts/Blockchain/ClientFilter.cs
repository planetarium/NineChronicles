using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MagicOnion.Client;
using Debug = UnityEngine.Debug;

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
                    NcDebugger.Log("Request Begin:" + context.MethodPath);
                    var sw = Stopwatch.StartNew();
                    var resp = await next(context);
                    sw.Stop();
                    NcDebugger.Log("Request Completed:" + context.MethodPath + ", Elapsed:" + sw.Elapsed.TotalMilliseconds + "ms");
                    return resp;
                }
                catch (Exception e)
                {
                    await Task.Delay((3 - retryCount) * 1000);
                    retryCount++;
                    exception = e;
                }
            }
            NcDebugger.Log($"Filter Catch Exception: {exception}");
            return null;
        }
    }
}
