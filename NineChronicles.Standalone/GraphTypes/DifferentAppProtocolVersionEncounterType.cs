using GraphQL.Types;

namespace NineChronicles.Standalone.GraphTypes
{
    public sealed class DifferentAppProtocolVersionEncounterType
        : ObjectGraphType<DifferentAppProtocolVersionEncounter>
    {
        public DifferentAppProtocolVersionEncounterType()
        {
            Field<NonNullGraphType<StringGraphType>>(
                name: "peer",
                resolve: context => context.Source.Peer.ToString());
            Field<NonNullGraphType<AppProtocolVersionType>>(
                name: "peerVersion",
                resolve: context => context.Source.PeerVersion);   
            Field<NonNullGraphType<AppProtocolVersionType>>(
                name: "localVersion",
                resolve: context => context.Source.LocalVersion);
        }
    }   
}
