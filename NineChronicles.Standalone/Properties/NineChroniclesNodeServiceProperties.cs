using Libplanet.Standalone.Hosting;

namespace NineChronicles.Standalone.Properties
{
    public class NineChroniclesNodeServiceProperties
    {
        public RpcNodeServiceProperties Rpc { get; set; }

        public LibplanetNodeServiceProperties Libplanet { get; set; }
    }
}
