using System.Threading.Tasks;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Net;
using Libplanet.Standalone.Hosting;
using Libplanet.Tx;
using Xunit;

namespace Libplanet.Standalone.Tests.Hosting
{
    public class LibplanetNodeServiceTest
    {
        // FIXME 깨지고 있는 테스트입니다.
        // https://github.com/planetarium/nekoyume-unity/issues/2152
        // [Fact]
        public void Constructor()
        {
            var service = new LibplanetNodeService<DummyAction>(
                new LibplanetNodeServiceProperties()
                {
                    AppProtocolVersion = new AppProtocolVersion(),
                },
                new BlockPolicy(),
                (chain, swarm, pk, ct) => Task.CompletedTask,
                null
            );

            Assert.NotNull(service);
        }

        private class BlockPolicy : IBlockPolicy<DummyAction>
        {
            public IAction BlockAction => null;

            public bool DoesTransactionFollowsPolicy(Transaction<DummyAction> transaction)
            {
                return true;
            }

            public long GetNextBlockDifficulty(BlockChain<DummyAction> blocks)
            {
                return 0;
            }

            public InvalidBlockException ValidateNextBlock(BlockChain<DummyAction> blocks, Block<DummyAction> nextBlock)
            {
                return null;
            }
        }

        private class DummyAction : IAction
        {
            IValue IAction.PlainValue => Dictionary.Empty;

            IAccountStateDelta IAction.Execute(IActionContext context)
            {
                return context.PreviousStates;
            }

            void IAction.LoadPlainValue(IValue plainValue)
            {
            }

            void IAction.Render(IActionContext context, IAccountStateDelta nextStates)
            {
                throw new System.NotImplementedException();
            }

            void IAction.Unrender(IActionContext context, IAccountStateDelta nextStates)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
