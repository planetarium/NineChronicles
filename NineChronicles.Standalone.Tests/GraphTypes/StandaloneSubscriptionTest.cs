using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Subscription;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Store;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using NineChronicles.Standalone.GraphTypes;
using Xunit;

namespace NineChronicles.Standalone.Tests.GraphTypes
{
    public class StandaloneSubscriptionTest
    {
        private IStandaloneContext StandaloneContextFx { get; }

        public StandaloneSubscriptionTest()
        {
            const int minimumDifficulty = 4096;
            var store = new DefaultStore(null);
            var genesisBlock = BlockChain<PolymorphicAction<ActionBase>>.MakeGenesisBlock();
            var blockChain =
                new BlockChain<PolymorphicAction<ActionBase>>(BlockPolicy.GetPolicy(minimumDifficulty), store, genesisBlock);

            StandaloneContextFx = new StandaloneContext
            {
                BlockChain = blockChain,
                PrivateKey = new PrivateKey(),
            };
        }

        [Fact]
        public async Task SubscribeTipChangedEvent()
        {
            var schema = new StandaloneSchema(StandaloneContextFx);
            Assert.IsType<StandaloneSubscription>(schema.Subscription);
            schema.Subscription.As<StandaloneSubscription>().RegisterTipChangedSubscription();

            var miner = new Address();

            var executor = new DocumentExecuter();

            const int repeat = 10;
            foreach (long index in Enumerable.Range(1, repeat))
            {
                await StandaloneContextFx.BlockChain.MineBlock(miner);

                var result = await executor.ExecuteAsync(new ExecutionOptions
                {
                    Query = "subscription { tipChanged { index } }",
                    Schema = schema,

                });

                Assert.IsType<SubscriptionExecutionResult>(result);
                var subscribeResult = (SubscriptionExecutionResult) result;
                Assert.Equal(index, StandaloneContextFx.BlockChain.Tip.Index);
                var stream = subscribeResult.Streams.Values.FirstOrDefault();
                var rawEvents = await stream.Take((int)index);
                Assert.NotNull(rawEvents);

                var events = (Dictionary<string, object>) rawEvents.Data;
                var tipChangedEvent = (Dictionary<string, object>) events["tipChanged"];
                Assert.Equal(index, tipChangedEvent["index"]);
            }
        }
    }
}
