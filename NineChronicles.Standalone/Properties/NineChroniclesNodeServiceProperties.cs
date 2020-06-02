using Libplanet.Standalone.Hosting;
using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Standalone.Properties
{
    public class NineChroniclesNodeServiceProperties
    {
        public RpcNodeServiceProperties Rpc { get; set; }

        public LibplanetNodeServiceProperties<NineChroniclesActionType> Libplanet { get; set; }
    }
}
