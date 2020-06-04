using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex;
using Bencodex.Types;
using GraphQL;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Crypto;
using Libplanet.Store;
using Nekoyume.Action;
using NineChronicles.Standalone.GraphTypes;
using Xunit;

namespace NineChronicles.Standalone.Tests.GraphTypes
{
    public class QueryTest
    {
        private StandaloneContext StandaloneContextFx { get; }

        // FIXME: GraphQL 태스트에서 공통으로 사용하는 코드 중복을 줄여야 합니다.
        public QueryTest()
        {
            var store = new DefaultStore(null);
            var genesisBlock = BlockChain<PolymorphicAction<ActionBase>>.MakeGenesisBlock();

            var blockPolicy = new BlockPolicy<PolymorphicAction<ActionBase>>(blockAction: new TestRewardGold());
            var blockChain =
                new BlockChain<PolymorphicAction<ActionBase>>(blockPolicy, store, genesisBlock);

            StandaloneContextFx = new StandaloneContext
            {
                BlockChain = blockChain,
                PrivateKey = new PrivateKey(),
            };
        }

        [Fact]
        public async Task GetState()
        {
            var schema = new StandaloneSchema(StandaloneContextFx);
            Assert.IsType<StandaloneSubscription>(schema.Subscription);
            schema.Subscription.As<StandaloneSubscription>().RegisterTipChangedSubscription();
            var blockChain = StandaloneContextFx.BlockChain;

            var codec = new Codec();
            var miner = new Address();

            var executor = new DocumentExecuter();

            const int repeat = 10;
            foreach (long index in Enumerable.Range(1, repeat))
            {
                await blockChain.MineBlock(miner);

                var result = await executor.ExecuteAsync(new ExecutionOptions
                {
                    Query = $"query {{ state(address: \"{miner.ToHex()}\") }}",
                    Schema = schema,
                });

                var data = (Dictionary<string, object>) result.Data;
                var state = (Integer)codec.Decode(ByteUtil.ParseHex((string) data["state"]));

                // TestRewardGold에서 miner에게 1 gold 씩 주므로 block index와 같을 것입니다.
                Assert.Equal((Integer)index, state);
            }
        }

        // 테스트를 위해 만든 RewardGold 액션입니다.
        class TestRewardGold : IAction
        {
            public void LoadPlainValue(IValue plainValue)
            {
            }

            public IAccountStateDelta Execute(IActionContext context)
            {
                var states = context.PreviousStates;
                if (context.Rehearsal)
                {
                    return states.SetState(context.Signer, default(Null));
                }

                var gold = states.TryGetState(context.Signer, out Integer integer) ? integer : (Integer)0;
                gold += 1;

                return states.SetState(context.Signer, gold);
            }

            public void Render(IActionContext context, IAccountStateDelta nextStates)
            {
            }

            public void RenderError(IActionContext context, Exception exception)
            {
            }

            public void Unrender(IActionContext context, IAccountStateDelta nextStates)
            {
            }

            public void UnrenderError(IActionContext context, Exception exception)
            {
            }

            public IValue PlainValue => new Null();
        }
    }
}
