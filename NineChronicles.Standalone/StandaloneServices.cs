using System;
using Libplanet.Net;
using NineChronicles.Standalone.Properties;

namespace NineChronicles.Standalone
{
    public static class StandaloneServices
    {
        public static NineChroniclesNodeService CreateHeadless(
            NineChroniclesNodeServiceProperties properties,
            StandaloneContext standaloneContext = null,
            bool ignoreBootstrapFailure = true
        )
        {
            Progress<PreloadState> progress = null;
            if (!(standaloneContext is null))
            {
                progress = new Progress<PreloadState>(state =>
                {
                    standaloneContext.PreloadStateSubject.OnNext(state);
                });
            }

            properties.Libplanet.DifferentAppProtocolVersionEncountered =
                (Peer peer, AppProtocolVersion peerVersion, AppProtocolVersion localVersion) =>
                {
                    standaloneContext.DifferentAppProtocolVersionEncounterSubject.OnNext(
                        new DifferentAppProtocolVersionEncounter
                        {
                            Peer = peer,
                            PeerVersion = peerVersion,
                            LocalVersion = localVersion,
                        }
                    );

                    // FIXME: 일단은 버전이 다른 피어는 마주쳐도 쌩깐다.
                    return false;
                };

            var service = new NineChroniclesNodeService(
                properties.Libplanet,
                properties.Rpc,
                preloadProgress: progress,
                ignoreBootstrapFailure: ignoreBootstrapFailure);
            service.ConfigureStandaloneContext(standaloneContext);

            return service;
        }

        internal static void ConfigureStandaloneContext(this NineChroniclesNodeService service, StandaloneContext standaloneContext)
        {
            if (!(standaloneContext is null))
            {
                standaloneContext.BlockChain = service.Swarm.BlockChain;
                service.BootstrapEnded.WaitAsync().ContinueWith((task) =>
                {
                    standaloneContext.BootstrapEnded = true;
                    standaloneContext.NodeStatusSubject.OnNext(standaloneContext.NodeStatus);
                });
                service.PreloadEnded.WaitAsync().ContinueWith((task) =>
                {
                    standaloneContext.PreloadEnded = true;
                    standaloneContext.NodeStatusSubject.OnNext(standaloneContext.NodeStatus);
                });
            }
        }
    }
}
