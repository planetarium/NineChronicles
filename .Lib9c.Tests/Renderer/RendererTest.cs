namespace Lib9c.Tests.Renderer
{
    using System;
    using System.Linq;
    using Lib9c.Renderer;
    using Libplanet;
    using Libplanet.Blocks;
    using Libplanet.Tx;
    using Xunit;
    using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;
    using NCBlock = Libplanet.Blocks.Block<Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>>;

    public class RendererTest
    {
        [Fact]
        public void BlockRendererTest()
        {
            var blockRenderer = new BlockRenderer();
            var branchPointBlock = new Block<NCAction>(
                index: 9,
                difficulty: 10000,
                totalDifficulty: 90000,
                nonce: new Nonce(new byte[] { 0x00, 0x00, 0x00, 0x01 }),
                miner: default,
                previousHash: null,
                timestamp: DateTimeOffset.MinValue,
                transactions: Enumerable.Empty<Transaction<NCAction>>());
            var oldBlock = new Block<NCAction>(
                index: 10,
                difficulty: 10000,
                totalDifficulty: 100000,
                nonce: new Nonce(new byte[] { 0x00, 0x00, 0x00, 0x02 }),
                miner: default,
                previousHash: branchPointBlock.Hash,
                timestamp: DateTimeOffset.MinValue.AddSeconds(1),
                transactions: Enumerable.Empty<Transaction<NCAction>>());
            var newBlock = new Block<NCAction>(
                index: 10,
                difficulty: 10000,
                totalDifficulty: 100000,
                nonce: new Nonce(new byte[] { 0x00, 0x00, 0x00, 0x03 }),
                miner: default,
                previousHash: oldBlock.Hash,
                timestamp: DateTimeOffset.MinValue.AddSeconds(2),
                transactions: Enumerable.Empty<Transaction<NCAction>>());

            (NCBlock OldTip, NCBlock NewTip) everyBlockResult = (null, null);
            (NCBlock OldTip, NCBlock NewTip, NCBlock BranchPoint) everyReorgResult = (null, null, null);
            (NCBlock OldTip, NCBlock NewTip, NCBlock BranchPoint) everyReorgEndResult = (null, null, null);

            blockRenderer.EveryBlock().Subscribe(pair => everyBlockResult = pair);
            blockRenderer.EveryReorg().Subscribe(ev => everyReorgResult = ev);
            blockRenderer.EveryReorgEnd().Subscribe(ev => everyReorgEndResult = ev);

            blockRenderer.RenderBlock(branchPointBlock, oldBlock);
            blockRenderer.RenderReorg(oldBlock, newBlock, branchPointBlock);
            blockRenderer.RenderReorgEnd(oldBlock, newBlock, branchPointBlock);

            Assert.Equal(branchPointBlock, everyBlockResult.OldTip);
            Assert.Equal(oldBlock, everyBlockResult.NewTip);
            Assert.Equal(oldBlock, everyReorgResult.OldTip);
            Assert.Equal(newBlock, everyReorgResult.NewTip);
            Assert.Equal(branchPointBlock, everyReorgResult.BranchPoint);
            Assert.Equal(oldBlock, everyReorgEndResult.OldTip);
            Assert.Equal(newBlock, everyReorgEndResult.NewTip);
            Assert.Equal(branchPointBlock, everyReorgEndResult.BranchPoint);
        }
    }
}
