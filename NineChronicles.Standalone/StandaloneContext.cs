using System.Reactive.Subjects;
using Libplanet.Blockchain;
using Libplanet.KeyStore;
using Libplanet.Net;
using NineChronicles.Standalone.GraphTypes;
using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Standalone
{
    public class StandaloneContext
    {
        public BlockChain<NineChroniclesActionType> BlockChain { get; set; }
        public IKeyStore KeyStore { get; set; }
        public bool BootstrapEnded { get; set; }
        public bool PreloadEnded { get; set; }
        public ReplaySubject<NodeStatusType> NodeStatusSubject { get; } = new ReplaySubject<NodeStatusType>();
        public ReplaySubject<PreloadState> PreloadStateSubject { get; } = new ReplaySubject<PreloadState>();
        public ReplaySubject<DifferentAppProtocolVersionEncounter> DifferentAppProtocolVersionEncounterSubject { get; }
            = new ReplaySubject<DifferentAppProtocolVersionEncounter>();
        public ReplaySubject<Notification> NotificationSubject { get; } = new ReplaySubject<Notification>(1);
        public NineChroniclesNodeService NineChroniclesNodeService { get; set; }
        public NodeStatusType NodeStatus => new NodeStatusType()
        {
            BootstrapEnded = BootstrapEnded,
            PreloadEnded = PreloadEnded,
        };
    }
}
