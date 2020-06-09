using System;
using System.Net;
using System.Threading.Tasks;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Standalone.Hosting;
using Libplanet.Tx;
using Xunit;

namespace Libplanet.Standalone.Tests.Hosting
{
    public class LibplanetNodeServiceTest
    {
        [Fact]
        public void Constructor()
        {
            var genesisBlock = BlockChain<DummyAction>.MakeGenesisBlock();
            var service = new LibplanetNodeService<DummyAction>(
                new LibplanetNodeServiceProperties<DummyAction>()
                {
                    AppProtocolVersion = new AppProtocolVersion(),
                    GenesisBlock = genesisBlock,
                    PrivateKey = new PrivateKey(),
                    StoreStatesCacheSize = 2,
                    Host = IPAddress.Loopback.ToString(),
                },
                new BlockPolicy(),
                (chain, swarm, pk, ct) => Task.CompletedTask,
                null
            );

            Assert.NotNull(service);
        }

        [Fact]
        public void PropertiesMustContainGenesisBlockOrPath()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var service = new LibplanetNodeService<DummyAction>(
                    new LibplanetNodeServiceProperties<DummyAction>()
                    {
                        AppProtocolVersion = new AppProtocolVersion(),
                        PrivateKey = new PrivateKey(),
                        StoreStatesCacheSize = 2,
                        Host = IPAddress.Loopback.ToString(),
                    },
                    new BlockPolicy(),
                    (chain, swarm, pk, ct) => Task.CompletedTask,
                    null
                );
            });
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

            public void RenderError(IActionContext context, Exception exception)
            {
            }

            public void UnrenderError(IActionContext context, Exception exception)
            {
            }

            IAccountStateDelta IAction.Execute(IActionContext context)
            {
                return context.PreviousStates;
            }

            void IAction.LoadPlainValue(IValue plainValue)
            {
            }

            void IAction.Render(IActionContext context, IAccountStateDelta nextStates)
            {
            }

            void IAction.Unrender(IActionContext context, IAccountStateDelta nextStates)
            {
            }
        }
    }
}
