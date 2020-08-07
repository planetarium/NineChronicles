using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using Log = Serilog.Log;

namespace NineChronicles.Standalone.GraphTypes
{
    public class PeerChainStateQuery : ObjectGraphType
    {
        public PeerChainStateQuery(StandaloneContext standaloneContext)
        {
            Field<NonNullGraphType<ListGraphType<StringGraphType>>>(
                name: "state",
                resolve: context =>
                {
                    var service = standaloneContext.NineChroniclesNodeService;

                    if (service is null)
                    {
                        Log.Error($"{nameof(NineChroniclesNodeService)} is null.");
                        return null;
                    }

                    var swarm = service.Swarm;
                    var chain = swarm.BlockChain;
                    var chainStates = new List<string>
                    {
                        $"{swarm.AsPeer.Address}, {chain.Tip.Index}, {chain.Tip.TotalDifficulty}"
                    };

                    var peerChainState = swarm.GetPeerChainStateAsync(
                        TimeSpan.FromSeconds(5), default)
                        .Result
                        .Select(
                            state => $"{state.Peer.Address}, {state.TipIndex}, {state.TotalDifficulty}");

                    chainStates.AddRange(peerChainState);

                    return chainStates;
                }
            );
        }
    }
}
