using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using Libplanet.Net;
using Libplanet.Standalone.Hosting;
using Libplanet.Store;
using Nekoyume.Action;
using NineChronicles.Standalone.GraphTypes;
using Serilog;
using Xunit.Abstractions;
using RewardGold = NineChronicles.Standalone.Tests.Common.Actions.RewardGold;

namespace NineChronicles.Standalone.Tests.GraphTypes
{
    public class GraphQLTestBase
    {
        protected ITestOutputHelper _output;

        public GraphQLTestBase(ITestOutputHelper output)
        {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();

            _output = output;

            var store = new DefaultStore(null);
            var genesisBlock = BlockChain<PolymorphicAction<ActionBase>>.MakeGenesisBlock();

            var blockPolicy = new BlockPolicy<PolymorphicAction<ActionBase>>(blockAction: new RewardGold());
            var blockChain =
                new BlockChain<PolymorphicAction<ActionBase>>(blockPolicy, store, genesisBlock);

            var tempKeyStorePath = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
            var keyStore = new Web3KeyStore(tempKeyStorePath);

            StandaloneContextFx = new StandaloneContext
            {
                BlockChain = blockChain,
                KeyStore = keyStore,
            };

            Schema = new StandaloneSchema(new TestServiceProvider(StandaloneContextFx));
            Schema.Subscription.As<StandaloneSubscription>().RegisterTipChangedSubscription();

            DocumentExecutor = new DocumentExecuter();
        }

        protected StandaloneSchema Schema { get; }

        protected StandaloneContext StandaloneContextFx { get; }

        protected BlockChain<PolymorphicAction<ActionBase>> BlockChain =>
            StandaloneContextFx.BlockChain;

        protected IKeyStore KeyStore =>
            StandaloneContextFx.KeyStore;

        protected IDocumentExecuter DocumentExecutor { get; }

        protected Task<ExecutionResult> ExecuteQueryAsync(string query)
        {
            return DocumentExecutor.ExecuteAsync(new ExecutionOptions
            {
                Query = query,
                Schema = Schema,
            });
        }

        protected async Task<Task> StartAsync<T>(
            Swarm<T> swarm,
            CancellationToken cancellationToken = default
        )
            where T : IAction, new()
        {
            Task task = swarm.StartAsync(200, 200, cancellationToken);
            await swarm.WaitForRunningAsync();
            return task;
        }

        protected LibplanetNodeService<T> CreateLibplanetNodeService<T>(
            Block<T> genesisBlock,
            AppProtocolVersion appProtocolVersion,
            PublicKey appProtocolVersionSigner,
            Progress<PreloadState> preloadProgress = null,
            IEnumerable<Peer> peers = null)
            where T : IAction, new()
        {
            var properties = new LibplanetNodeServiceProperties<T>
            {
                Host = System.Net.IPAddress.Loopback.ToString(),
                AppProtocolVersion = appProtocolVersion,
                GenesisBlock = genesisBlock,
                StoreStatesCacheSize = 2,
                PrivateKey = new PrivateKey(),
                Port = null,
                MinimumDifficulty = 1024,
                NoMiner = true,
                Render = false,
                Peers = peers ?? ImmutableHashSet<Peer>.Empty,
                TrustedAppProtocolVersionSigners = ImmutableHashSet<PublicKey>.Empty.Add(appProtocolVersionSigner),
            };

            return new LibplanetNodeService<T>(
                properties,
                new BlockPolicy<T>(),
                async (chain, swarm, privateKey, cancellationToken) => { },
                preloadProgress);
        }

        private class TestServiceProvider : IServiceProvider
        {
            private StandaloneQuery Query;

            private StandaloneMutation Mutation;

            private StandaloneSubscription Subscription;

            private StandaloneContext StandaloneContext;

            public TestServiceProvider(StandaloneContext standaloneContext)
            {
                Query = new StandaloneQuery(standaloneContext);
                Mutation = new StandaloneMutation(standaloneContext);
                Subscription = new StandaloneSubscription(standaloneContext);
                StandaloneContext = standaloneContext;
            }

            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(StandaloneQuery))
                {
                    return Query;
                }

                if (serviceType == typeof(StandaloneMutation))
                {
                    return Mutation;
                }

                if (serviceType == typeof(StandaloneSubscription))
                {
                    return Subscription;
                }

                if (serviceType == typeof(ValidationQuery))
                {
                    return new ValidationQuery(StandaloneContext);
                }

                if (serviceType == typeof(ActivationStatusQuery))
                {
                    return new ActivationStatusQuery(StandaloneContext);
                }

                if (serviceType == typeof(PeerChainStateQuery))
                {
                    return new PeerChainStateQuery(StandaloneContext);
                }

                return Activator.CreateInstance(serviceType);
            }
        }
    }
}
