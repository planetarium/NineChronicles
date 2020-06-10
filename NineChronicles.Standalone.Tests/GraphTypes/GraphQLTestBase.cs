using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using Libplanet.Store;
using Nekoyume.Action;
using NineChronicles.Standalone.GraphTypes;
using Xunit.Abstractions;
using RewardGold = NineChronicles.Standalone.Tests.Common.Actions.RewardGold;

namespace NineChronicles.Standalone.Tests.GraphTypes
{
    public class GraphQLTestBase
    {
        protected ITestOutputHelper _output;

        public GraphQLTestBase(ITestOutputHelper output)
        {
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

            Schema = new StandaloneSchema(StandaloneContextFx);
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
    }
}
