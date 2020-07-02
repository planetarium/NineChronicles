using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BTAI;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Subscription;
using GraphQL.Types;
using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Net;
using Log = Serilog.Log;

namespace NineChronicles.Standalone.GraphTypes
{
    public class StandaloneSubscription : ObjectGraphType
    {
        class TipChanged : ObjectGraphType<TipChanged>
        {
            public long Index { get; set; }

            public HashDigest<SHA256> Hash { get; set; }

            public TipChanged()
            {
                Field<NonNullGraphType<LongGraphType>>(nameof(Index));
                Field<ByteStringType>("hash", resolve: context => context.Source.Hash.ToByteArray());
            }
        }

        class PreloadStateType : ObjectGraphType<PreloadState>
        {
            private class PreloadStateExtra
            {
                public string Type { get; set; }
                public long CurrentCount { get; set; }
                public long TotalCount { get; set; }
            }

            private class PreloadStateExtraType : ObjectGraphType<PreloadStateExtra>
            {
                public PreloadStateExtraType()
                {
                    Field<NonNullGraphType<StringGraphType>>(nameof(PreloadStateExtra.Type));
                    Field<NonNullGraphType<LongGraphType>>(nameof(PreloadStateExtra.CurrentCount));
                    Field<NonNullGraphType<LongGraphType>>(nameof(PreloadStateExtra.TotalCount));
                }
            }

            public PreloadStateType()
            {
                Field<NonNullGraphType<LongGraphType>>(name: "currentPhase", resolve: context => context.Source.CurrentPhase);
                Field<NonNullGraphType<LongGraphType>>(name: "totalPhase", resolve: context => PreloadState.TotalPhase);
                Field<NonNullGraphType<PreloadStateExtraType>>(name: "extra", resolve: context =>
                {
                    var preloadState = context.Source;
                    return preloadState switch
                    {
                        ActionExecutionState actionExecutionState => new PreloadStateExtra
                        {
                            Type = nameof(ActionExecutionState),
                            CurrentCount = actionExecutionState.ExecutedBlockCount,
                            TotalCount = actionExecutionState.TotalBlockCount,
                        },
                        BlockDownloadState blockDownloadState => new PreloadStateExtra
                        {
                            Type = nameof(BlockDownloadState),
                            CurrentCount = blockDownloadState.ReceivedBlockCount,
                            TotalCount = blockDownloadState.TotalBlockCount,
                        },
                        BlockHashDownloadState blockHashDownloadState => new PreloadStateExtra
                        {
                            Type = nameof(BlockHashDownloadState),
                            CurrentCount = blockHashDownloadState.ReceivedBlockHashCount,
                            TotalCount = blockHashDownloadState.EstimatedTotalBlockHashCount,
                        },
                        BlockVerificationState blockVerificationState => new PreloadStateExtra
                        {
                            Type = nameof(BlockVerificationState),
                            CurrentCount = blockVerificationState.VerifiedBlockCount,
                            TotalCount = blockVerificationState.TotalBlockCount,
                        },
                        StateDownloadState stateDownloadState => new PreloadStateExtra
                        {
                            Type = nameof(StateDownloadState),
                            CurrentCount = stateDownloadState.ReceivedIterationCount,
                            TotalCount = stateDownloadState.TotalIterationCount,
                        },
                        _ => throw new ExecutionError($"Not supported preload state. {preloadState.GetType()}"),
                    };
                });
            }
        }

        class DifferentAppProtocolVersionEncounterType : ObjectGraphType<DifferentAppProtocolVersionEncounter>
        {
            public DifferentAppProtocolVersionEncounterType()
            {
                Field<StringGraphType>(
                    name: "peer",
                    resolve: context => context.Source.Peer.ToString());
                Field<StringGraphType>(
                    name: "peerVersion",
                    resolve: context => context.Source.PeerVersion.Token);   
                Field<StringGraphType>(
                    name: "localVersion",
                    resolve: context => context.Source.LocalVersion.Token);
            }
        }

        private ISubject<TipChanged> _subject = new ReplaySubject<TipChanged>();

        private StandaloneContext StandaloneContext { get; }

        public StandaloneSubscription(StandaloneContext standaloneContext)
        {
            StandaloneContext = standaloneContext;
            AddField(new EventStreamFieldType {
                Name = "tipChanged",
                Type = typeof(TipChanged),
                Resolver = new FuncFieldResolver<TipChanged>(ResolveTipChanged),
                Subscriber = new EventStreamResolver<TipChanged>(SubscribeTipChanged),
            });
            AddField(new EventStreamFieldType {
                Name = "preloadProgress",
                Type = typeof(PreloadStateType),
                Resolver = new FuncFieldResolver<PreloadState>(context => context.Source as PreloadState),
                Subscriber = new EventStreamResolver<PreloadState>(context => StandaloneContext.PreloadStateSubject.AsObservable()),
            });
            AddField(new EventStreamFieldType
            {
                Name = "nodeStatus",
                Type = typeof(NodeStatusType),
                Resolver = new FuncFieldResolver<NodeStatusType>(context => context.Source as NodeStatusType),
                Subscriber = new EventStreamResolver<NodeStatusType>(context => StandaloneContext.NodeStatusSubject.AsObservable()),
            });
            AddField(new EventStreamFieldType
            {
                Name = "differentAppProtocolVersionEncounter",
                Type = typeof(DifferentAppProtocolVersionEncounterType),
                Resolver = new FuncFieldResolver<DifferentAppProtocolVersionEncounter>(context =>
                    (DifferentAppProtocolVersionEncounter)context.Source),
                Subscriber = new EventStreamResolver<DifferentAppProtocolVersionEncounter>(context =>
                    StandaloneContext.DifferentAppProtocolVersionEncounterSubject.AsObservable()),
            });
        }

        public void RegisterTipChangedSubscription()
        {
            if (StandaloneContext.BlockChain is null)
            {
                throw new ArgumentNullException(
                    nameof(StandaloneContext.BlockChain),
                    $"{nameof(StandaloneContext)}.{nameof(StandaloneContext.BlockChain)}" +
                    $" should be set before calling `{nameof(RegisterTipChangedSubscription)}()`.");
            }

            StandaloneContext.BlockChain.TipChanged += (sender, args) =>
            {
                _subject.OnNext(new TipChanged
                {
                    Index = args.Index,
                    Hash = args.Hash,
                });
            };
        }

        private TipChanged ResolveTipChanged(IResolveFieldContext context)
        {
            return context.Source as TipChanged;
        }

        private IObservable<TipChanged> SubscribeTipChanged(IResolveEventStreamContext context)
        {
            return _subject.AsObservable();
        }
    }
}
