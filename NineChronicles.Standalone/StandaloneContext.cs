using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using Libplanet.Net;
using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Standalone
{
    public class StandaloneContext
    {
        public BlockChain<NineChroniclesActionType> BlockChain { get; set; }
        public IKeyStore KeyStore { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public bool BootstrapEnded { get; set; }
        public bool PreloadEnded { get; set; }
        public ReplaySubject<PreloadState> PreloadStateSubject { get; } = new ReplaySubject<PreloadState>();
        public NineChroniclesNodeService NineChroniclesNodeService { get; set; }
    }
}
