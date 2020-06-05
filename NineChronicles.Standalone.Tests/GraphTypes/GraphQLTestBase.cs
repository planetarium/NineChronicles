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

            var tempKeyStorePath = Path.GetTempPath();
            var keyStore = new Web3KeyStore(tempKeyStorePath);
            // Path.GetTempPath()가 실행할 때 마다 새로운 경로를 리턴하지 않을 경우를 위해 생성자에서 초기화해줍니다.
            foreach (var keyId in keyStore.ListIds().ToImmutableList())
            {
                keyStore.Remove(keyId);
            }

            StandaloneContextFx = new StandaloneContext
            {
                BlockChain = blockChain,
                PrivateKey = new PrivateKey(),
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
