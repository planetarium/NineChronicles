using System.Threading.Tasks;
using GraphQL;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Crypto;
using Libplanet.Store;
using Nekoyume.Action;
using NineChronicles.Standalone.GraphTypes;

using RewardGold = NineChronicles.Standalone.Tests.Common.Actions.RewardGold;

namespace NineChronicles.Standalone.Tests.GraphTypes
{
    public class GraphQLTestBase
    {
        protected GraphQLTestBase()
        {
            var store = new DefaultStore(null);
            var genesisBlock = BlockChain<PolymorphicAction<ActionBase>>.MakeGenesisBlock();

            var blockPolicy = new BlockPolicy<PolymorphicAction<ActionBase>>(blockAction: new RewardGold());
            var blockChain =
                new BlockChain<PolymorphicAction<ActionBase>>(blockPolicy, store, genesisBlock);

            StandaloneContextFx = new StandaloneContext
            {
                BlockChain = blockChain,
                PrivateKey = new PrivateKey(),
            };

            Schema = new StandaloneSchema(StandaloneContextFx);
            Schema.Subscription.As<StandaloneSubscription>().RegisterTipChangedSubscription();

            DocumentExecutor = new DocumentExecuter();
        }

        protected StandaloneSchema Schema { get; }

        protected StandaloneContext StandaloneContextFx { get; }

        protected BlockChain<PolymorphicAction<ActionBase>> BlockChain =>
            StandaloneContextFx.BlockChain;

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
